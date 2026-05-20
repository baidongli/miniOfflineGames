using System;
using System.Collections.Generic;
using MiniGames.GameModule;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Session;
using MiniGames.Networking.Transport;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGames.App.Hub
{
    /// <summary>
    /// Lobby for Nearby multiplayer. Hosts call Host(); clients call Discover().
    /// The view renders the current RoomSnapshot; Start button is host-only and
    /// disabled until at least min-players are ready.
    /// </summary>
    public sealed class RoomLobbyController : MonoBehaviour
    {
        [Header("View")]
        [SerializeField] private TMP_Text _statusLabel;
        [SerializeField] private RectTransform _peerList;
        [SerializeField] private PeerRowView _peerRowPrefab;
        [SerializeField] private RectTransform _discoveredList;
        [SerializeField] private PeerRowView _discoveredRowPrefab;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _readyButton;
        [SerializeField] private Button _leaveButton;

        private IGameTransport _transport;
        private RoomManager _room;
        private IGameModule _game;
        private readonly Dictionary<PeerId, PeerRowView> _peerRows = new Dictionary<PeerId, PeerRowView>();
        private readonly Dictionary<PeerId, PeerRowView> _discoveredRows = new Dictionary<PeerId, PeerRowView>();

        public event Action<IGameModule, RoomSnapshot, int> RoomStarting;
        public event Action LobbyLeft;

        private const string ServiceId = "app.minigames.v1";

        public void HostFor(IGameModule game, IGameTransport transport, IMessageSerializer serializer,
            string localPlayerId, string localDisplayName)
        {
            _game = game;
            BindTransport(transport, serializer, localPlayerId, localDisplayName);
            _room.HostRoom(ServiceId);
            _room.SelectGame(game.Id);
            _statusLabel.text = $"Hosting {game.DisplayName} - waiting for players";
            _startButton.gameObject.SetActive(true);
            _startButton.interactable = false;
            _readyButton.gameObject.SetActive(false);
        }

        public void JoinFor(IGameModule game, IGameTransport transport, IMessageSerializer serializer,
            string localPlayerId, string localDisplayName)
        {
            _game = game;
            BindTransport(transport, serializer, localPlayerId, localDisplayName);
            _room.JoinDiscovery(ServiceId);
            _statusLabel.text = "Looking for nearby hosts...";
            _startButton.gameObject.SetActive(false);
            _readyButton.gameObject.SetActive(true);
        }

        private void BindTransport(IGameTransport transport, IMessageSerializer serializer,
            string localPlayerId, string localDisplayName)
        {
            _transport = transport;
            _room = new RoomManager(transport, serializer, localPlayerId, localDisplayName);
            _room.SnapshotChanged += RenderSnapshot;
            _room.GameStarting += OnGameStarting;

            transport.EndpointFound += OnEndpointFound;
            transport.EndpointLost += OnEndpointLost;

            _startButton.onClick.AddListener(OnStartTapped);
            _readyButton.onClick.AddListener(OnReadyTapped);
            _leaveButton.onClick.AddListener(OnLeaveTapped);
        }

        private void OnEndpointFound(PeerId peer, string name)
        {
            if (_discoveredRows.ContainsKey(peer)) return;
            var row = Instantiate(_discoveredRowPrefab, _discoveredList);
            row.Bind(name, isReady: false, onTap: () =>
            {
                _transport.RequestConnection(peer);
                _statusLabel.text = $"Connecting to {name}...";
            });
            _discoveredRows[peer] = row;
        }

        private void OnEndpointLost(PeerId peer)
        {
            if (_discoveredRows.TryGetValue(peer, out var row))
            {
                Destroy(row.gameObject);
                _discoveredRows.Remove(peer);
            }
        }

        private void RenderSnapshot(RoomSnapshot snap)
        {
            foreach (var row in _peerRows.Values) Destroy(row.gameObject);
            _peerRows.Clear();

            foreach (var slot in snap.Players)
            {
                var row = Instantiate(_peerRowPrefab, _peerList);
                row.Bind(slot.DisplayName + (slot.IsHost ? " (host)" : ""), slot.IsReady, null);
                _peerRows[new PeerId(slot.PlayerId)] = row;
            }

            if (_room.IsHost)
            {
                int readyCount = 0;
                foreach (var s in snap.Players) if (s.IsReady) readyCount++;
                _startButton.interactable = readyCount >= _game.MinPlayers;
                _statusLabel.text = $"{snap.Players.Count} players, {readyCount} ready";
            }
        }

        private void OnGameStarting(StartGame start)
        {
            var snap = new RoomSnapshot { SelectedGameId = start.GameId };
            // Real snapshot will arrive separately; for now just forward.
            RoomStarting?.Invoke(_game, snap, start.Seed);
        }

        private void OnStartTapped()
        {
            var seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            _room.StartGame(countdownMs: 3000, seed: seed);
        }

        private void OnReadyTapped()
        {
            // TODO: send PlayerReady via room
        }

        private void OnLeaveTapped()
        {
            _transport?.Stop();
            LobbyLeft?.Invoke();
        }
    }
}
