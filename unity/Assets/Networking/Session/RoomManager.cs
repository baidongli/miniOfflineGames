using System;
using System.Collections.Generic;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Transport;
using UnityEngine;

namespace MiniGames.Networking.Session
{
    /// <summary>
    /// Game-agnostic room lifecycle: who is host, who is connected, ready states,
    /// selected game, start signal. Sits on top of IGameTransport. Game code
    /// subscribes to events here, not to the transport directly.
    /// </summary>
    public sealed class RoomManager
    {
        private readonly IGameTransport _transport;
        private readonly IMessageSerializer _serializer;
        private readonly Func<DateTimeOffset> _now;

        private readonly Dictionary<PeerId, PlayerSlot> _players = new Dictionary<PeerId, PlayerSlot>();

        /// <summary>Disconnected players still holding their slot during the grace window.</summary>
        private readonly Dictionary<string, DisconnectedSlot> _grace = new Dictionary<string, DisconnectedSlot>();

        /// <summary>How long after a disconnect we keep the slot open for reconnection. Configurable.</summary>
        public TimeSpan ReconnectGrace { get; set; } = TimeSpan.FromSeconds(10);

        public bool IsHost { get; private set; }
        public string LocalPlayerId { get; }
        public string LocalDisplayName { get; }
        public string SelectedGameId { get; private set; }

        /// <summary>On clients, the peer id of the host. Null on host itself or before connection.</summary>
        public PeerId? HostPeer { get; private set; }

        public IReadOnlyDictionary<PeerId, PlayerSlot> ConnectedPlayers => _players;

        public event Action<RoomSnapshot> SnapshotChanged;
        public event Action<StartGame> GameStarting;
        public event Action<PeerId, MessageType, ArraySegment<byte>> MessageReceived;

        /// <summary>Fires for any 0x80+ message; the body slice excludes the leading type byte.</summary>
        public event Action<PeerId, MessageType, ArraySegment<byte>> GameMessageReceived;

        /// <summary>Fires when a previously-disconnected player reconnects within the grace window.</summary>
        public event Action<PeerId, PlayerSlot> PlayerRestored;

        public RoomManager(IGameTransport transport, IMessageSerializer serializer,
            string localPlayerId, string localDisplayName,
            Func<DateTimeOffset> now = null)
        {
            _transport = transport;
            _serializer = serializer;
            _now = now ?? (() => DateTimeOffset.UtcNow);
            LocalPlayerId = localPlayerId;
            LocalDisplayName = localDisplayName;

            _transport.ConnectionResult += OnConnectionResult;
            _transport.Disconnected += OnDisconnected;
            _transport.PayloadReceived += OnPayloadReceived;
        }

        private struct DisconnectedSlot
        {
            public PlayerSlot Slot;
            public DateTimeOffset At;
        }

        public void HostRoom(string serviceId)
        {
            IsHost = true;
            _transport.StartAdvertising(serviceId, LocalDisplayName);
        }

        public void JoinDiscovery(string serviceId)
        {
            IsHost = false;
            _transport.StartDiscovery(serviceId);
        }

        public void SelectGame(string gameId)
        {
            if (!IsHost) return;
            SelectedGameId = gameId;
            BroadcastSnapshot();
        }

        public void StartGame(int countdownMs, int seed)
        {
            if (!IsHost || string.IsNullOrEmpty(SelectedGameId)) return;
            var msg = new StartGame { GameId = SelectedGameId, CountdownMs = countdownMs, Seed = seed };
            var bytes = _serializer.Encode(MessageType.StartGame, msg);
            _transport.Broadcast(bytes, reliable: true);
            GameStarting?.Invoke(msg);
        }

        // --- Game message send API (game-specific 0x80+ payloads). The game
        // serializes its own body bytes; we just frame with a type byte and ship. ---

        public void SendGameMessageToHost(MessageType type, byte[] body, bool reliable)
        {
            if (IsHost) return; // host has no host to send to
            if (HostPeer == null) return;
            _transport.Send(HostPeer.Value, FrameRaw(type, body), reliable);
        }

        public void BroadcastGameMessage(MessageType type, byte[] body, bool reliable)
        {
            _transport.Broadcast(FrameRaw(type, body), reliable);
        }

        public void SendGameMessageTo(PeerId peer, MessageType type, byte[] body, bool reliable)
        {
            _transport.Send(peer, FrameRaw(type, body), reliable);
        }

        private static ArraySegment<byte> FrameRaw(MessageType type, byte[] body)
        {
            int len = body?.Length ?? 0;
            var buf = new byte[1 + len];
            buf[0] = (byte)type;
            if (len > 0) Buffer.BlockCopy(body, 0, buf, 1, len);
            return new ArraySegment<byte>(buf);
        }

        // --- internals ---

        private void OnConnectionResult(PeerId peer, ConnectionStatus status)
        {
            if (status != ConnectionStatus.Ok) return;
            if (IsHost)
            {
                // Wait for client Hello, then add to roster.
            }
            else
            {
                HostPeer = peer;
                // Send Hello to host.
                var hello = new Hello
                {
                    PlayerId = LocalPlayerId,
                    DisplayName = LocalDisplayName,
                    AppVersionMajor = 1,
                    AppVersionMinor = 0,
                    Platform = Application.platform.ToString()
                };
                _transport.Send(peer, _serializer.Encode(MessageType.Hello, hello), reliable: true);
            }
        }

        private void OnDisconnected(PeerId peer)
        {
            if (_players.TryGetValue(peer, out var slot))
            {
                _players.Remove(peer);
                if (IsHost && !string.IsNullOrEmpty(slot.PlayerId))
                {
                    // Keep the slot warm for a short window so a brief WiFi
                    // blip doesn't end the game; OnClientHello restores it.
                    slot.IsConnected = false;
                    _grace[slot.PlayerId] = new DisconnectedSlot { Slot = slot, At = _now() };
                }
                BroadcastSnapshot();
            }
        }

        /// <summary>
        /// Drop slots whose grace window expired. Call periodically (e.g. once
        /// a second) on the host. No-op on clients.
        /// </summary>
        public void PruneStaleDisconnects()
        {
            if (!IsHost || _grace.Count == 0) return;
            var now = _now();
            List<string> expired = null;
            foreach (var kv in _grace)
                if (now - kv.Value.At >= ReconnectGrace)
                {
                    expired ??= new List<string>();
                    expired.Add(kv.Key);
                }
            if (expired == null) return;
            foreach (var id in expired) _grace.Remove(id);
            // Snapshot already updated when the disconnect first happened;
            // no need to broadcast again unless callers want to clean state.
        }

        private void OnPayloadReceived(PeerId peer, ArraySegment<byte> payload)
        {
            var type = _serializer.PeekType(payload);
            MessageReceived?.Invoke(peer, type, payload);

            if ((byte)type >= (byte)MessageType.GameSpecificBase)
            {
                // Strip the leading type byte; hand the body to game code.
                var body = new ArraySegment<byte>(payload.Array, payload.Offset + 1, payload.Count - 1);
                GameMessageReceived?.Invoke(peer, type, body);
                return;
            }

            switch (type)
            {
                case MessageType.Hello when IsHost:
                    if (_serializer.TryDecode<Hello>(payload, out _, out var hello))
                        OnClientHello(peer, hello);
                    break;
                case MessageType.PlayerReady when IsHost:
                    if (_serializer.TryDecode<PlayerReady>(payload, out _, out var ready) &&
                        _players.TryGetValue(peer, out var slot))
                    {
                        slot.IsReady = ready.Ready;
                        BroadcastSnapshot();
                    }
                    break;
                case MessageType.RoomSnapshot when !IsHost:
                    if (_serializer.TryDecode<RoomSnapshot>(payload, out _, out var snap))
                        SnapshotChanged?.Invoke(snap);
                    break;
                case MessageType.StartGame when !IsHost:
                    if (_serializer.TryDecode<StartGame>(payload, out _, out var start))
                        GameStarting?.Invoke(start);
                    break;
            }
        }

        private void OnClientHello(PeerId peer, Hello hello)
        {
            // Reconnect path: if we still hold the slot from a recent disconnect,
            // restore it (preserves ColorIndex, Ready, etc) instead of treating
            // this as a fresh join.
            if (!string.IsNullOrEmpty(hello.PlayerId) && _grace.TryGetValue(hello.PlayerId, out var held))
            {
                var restored = held.Slot;
                restored.IsConnected = true;
                restored.DisplayName = hello.DisplayName;
                _players[peer] = restored;
                _grace.Remove(hello.PlayerId);
                PlayerRestored?.Invoke(peer, restored);
                BroadcastSnapshot();
                return;
            }

            _players[peer] = new PlayerSlot
            {
                PlayerId = hello.PlayerId,
                DisplayName = hello.DisplayName,
                ColorIndex = _players.Count + _grace.Count,
                IsHost = false,
                IsReady = false,
                IsConnected = true
            };
            BroadcastSnapshot();
        }

        private void BroadcastSnapshot()
        {
            if (!IsHost) return;
            var snap = BuildSnapshot();
            var bytes = _serializer.Encode(MessageType.RoomSnapshot, snap);
            _transport.Broadcast(bytes, reliable: true);
            SnapshotChanged?.Invoke(snap);
        }

        private RoomSnapshot BuildSnapshot()
        {
            var snap = new RoomSnapshot
            {
                HostPlayerId = LocalPlayerId,
                SelectedGameId = SelectedGameId
            };
            snap.Players.Add(new PlayerSlot
            {
                PlayerId = LocalPlayerId,
                DisplayName = LocalDisplayName,
                ColorIndex = 0,
                IsHost = true,
                IsReady = true,
                IsConnected = true
            });
            foreach (var kv in _players)
                snap.Players.Add(kv.Value);
            // Include grace-period slots so the UI shows them as "reconnecting...".
            foreach (var kv in _grace)
                snap.Players.Add(kv.Value.Slot);
            return snap;
        }
    }
}
