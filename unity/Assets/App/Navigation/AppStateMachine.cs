using System;
using System.Collections.Generic;
using MiniGames.App.Bootstrap;
using MiniGames.GameModule;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Session;
using MiniGames.Networking.Transport;

namespace MiniGames.App.Navigation
{
    /// <summary>
    /// Pure-C# screen flow controller. Owns the current game module +
    /// session, drives view transitions, and validates state changes.
    /// All transitions log to AppServices.Analytics for funnel tracking.
    ///
    /// Boot -&gt; Hub -&gt; GameSelect -&gt; (InGame | Lobby -&gt; InGame) -&gt; Results -&gt; Hub
    /// </summary>
    public sealed class AppStateMachine
    {
        private readonly AppServices _services;
        private readonly IAppView _view;
        private readonly IReadOnlyList<IGameModule> _games;

        public AppState State { get; private set; } = AppState.Boot;
        public IGameModule SelectedGame { get; private set; }
        public PlayMode? CurrentMode { get; private set; }
        public GameSession ActiveSession { get; private set; }
        public RoomManager Room { get; private set; }

        public event Action<AppState, AppState> StateChanged;

        public AppStateMachine(AppServices services, IAppView view, IReadOnlyList<IGameModule> games)
        {
            _services = services;
            _view = view;
            _games = games;
            _view.ShowBoot();
        }

        public void GoToHub()
        {
            ClearSession();
            SelectedGame = null;
            CurrentMode = null;
            Transition(AppState.Hub, () => _view.ShowHub(_games));
        }

        public void SelectGame(IGameModule game)
        {
            if (game == null) return;
            if (State != AppState.Hub) return;
            SelectedGame = game;
            Transition(AppState.GameSelect, () => _view.ShowGameSelect(game));
        }

        public void CancelGameSelect()
        {
            if (State != AppState.GameSelect) return;
            GoToHub();
        }

        public void StartSolo()
        {
            if (State != AppState.GameSelect || SelectedGame == null) return;
            if ((SelectedGame.Capabilities & GameCapabilities.Solo) == 0) return;
            CurrentMode = PlayMode.Solo;
            ActiveSession = BuildSession(net: NullSendChannel.Instance, room: null);
            ActiveSession.Module.StartSolo(ActiveSession.Context);
            Transition(AppState.InGame, () => _view.ShowInGame(SelectedGame, PlayMode.Solo));
        }

        public void StartSameDevice()
        {
            if (State != AppState.GameSelect || SelectedGame == null) return;
            if ((SelectedGame.Capabilities & GameCapabilities.SameDevice) == 0) return;
            CurrentMode = PlayMode.SameDevice;
            // Same-device runs the multiplayer code path with a Null channel;
            // local players are routed via the SameDeviceInputDispatcher.
            ActiveSession = BuildSession(net: NullSendChannel.Instance, room: null);
            var pseudoSnap = MakeLocalPseudoSnapshot(SelectedGame.MinPlayers);
            ActiveSession.Module.StartMultiplayer(ActiveSession.Context, pseudoSnap, NewSeed(), isHost: true);
            Transition(AppState.InGame, () => _view.ShowInGame(SelectedGame, PlayMode.SameDevice));
        }

        public void HostNearby()
        {
            if (State != AppState.GameSelect || SelectedGame == null) return;
            if ((SelectedGame.Capabilities & GameCapabilities.Multiplayer) == 0) return;
            CurrentMode = PlayMode.NearbyHost;
            Room = new RoomManager(_services.Transport, _services.Serializer,
                _services.LocalPlayerId, _services.LocalDisplayName);
            Room.HostRoom(MakeServiceId());
            Room.SelectGame(SelectedGame.Id);
            Room.GameStarting += OnGameStarting;
            Transition(AppState.Lobby, () => _view.ShowLobby(SelectedGame, isHost: true));
        }

        public void JoinNearby()
        {
            if (State != AppState.GameSelect || SelectedGame == null) return;
            CurrentMode = PlayMode.NearbyJoin;
            Room = new RoomManager(_services.Transport, _services.Serializer,
                _services.LocalPlayerId, _services.LocalDisplayName);
            Room.JoinDiscovery(MakeServiceId());
            Room.GameStarting += OnGameStarting;
            Transition(AppState.Lobby, () => _view.ShowLobby(SelectedGame, isHost: false));
        }

        public void HostStartGame()
        {
            if (State != AppState.Lobby) return;
            if (Room == null || !Room.IsHost) return;
            Room.StartGame(countdownMs: 3000, seed: NewSeed());
        }

        public void LeaveLobby()
        {
            if (State != AppState.Lobby) return;
            _services.Transport.Stop();
            Room = null;
            GoToHub();
        }

        public void EndGame(GameResults results)
        {
            if (State != AppState.InGame) return;
            results ??= new GameResults
            {
                LocalPlayerId = _services.LocalPlayerId,
                Players = new List<PlayerResult>()
            };

            // Auto-submit the local player's score to the per-game leaderboard.
            // Skips when no score (turn-based games) or no leaderboard service.
            if (_services?.HighScores != null && SelectedGame != null && results.Players != null)
            {
                foreach (var p in results.Players)
                {
                    if (p == null || p.PlayerId != _services.LocalPlayerId) continue;
                    if (p.Score <= 0) continue;   // turn-based games typically use Score=0
                    _services.HighScores.Submit(
                        SelectedGame.Id,
                        p.PlayerId,
                        _services.LocalDisplayName,
                        p.Score);
                    break;
                }
            }

            ClearSession();
            Transition(AppState.Results, () => _view.ShowResults(SelectedGame, results));
        }

        public void DismissResults() => GoToHub();

        // --- internals ---

        private void OnGameStarting(StartGame start)
        {
            if (State != AppState.Lobby) return;
            if (SelectedGame == null || SelectedGame.Id != start.GameId) return;

            ActiveSession = BuildSession(net: new RoomSendChannel(Room), room: Room);
            var snap = Room.IsHost ? BuildHostSnapshot() : new RoomSnapshot
            {
                HostPlayerId = Room.HostPeer?.Value,
                SelectedGameId = SelectedGame.Id
            };
            ActiveSession.Module.StartMultiplayer(ActiveSession.Context, snap, start.Seed, Room.IsHost);
            Transition(AppState.InGame, () => _view.ShowInGame(SelectedGame, CurrentMode.GetValueOrDefault()));
        }

        private RoomSnapshot BuildHostSnapshot()
        {
            var snap = new RoomSnapshot
            {
                HostPlayerId = _services.LocalPlayerId,
                SelectedGameId = SelectedGame.Id
            };
            snap.Players.Add(new PlayerSlot
            {
                PlayerId = _services.LocalPlayerId,
                DisplayName = _services.LocalDisplayName,
                IsHost = true,
                IsReady = true,
                IsConnected = true
            });
            foreach (var kv in Room.ConnectedPlayers) snap.Players.Add(kv.Value);
            return snap;
        }

        private RoomSnapshot MakeLocalPseudoSnapshot(int playerCount)
        {
            var snap = new RoomSnapshot
            {
                HostPlayerId = _services.LocalPlayerId,
                SelectedGameId = SelectedGame.Id
            };
            for (int i = 0; i < playerCount; i++)
            {
                snap.Players.Add(new PlayerSlot
                {
                    PlayerId = i == 0 ? _services.LocalPlayerId : $"local_{i}",
                    DisplayName = i == 0 ? _services.LocalDisplayName : $"P{i + 1}",
                    ColorIndex = i,
                    IsHost = i == 0,
                    IsReady = true,
                    IsConnected = true
                });
            }
            return snap;
        }

        private GameSession BuildSession(IGameSendChannel net, RoomManager room)
        {
            var ctx = new GameContext(
                _services.Audio, _services.Save, _services.Analytics, _services.Haptics,
                net, _services.LocalPlayerId);
            return new GameSession(SelectedGame, ctx, room);
        }

        private void ClearSession()
        {
            if (Room != null) Room.GameStarting -= OnGameStarting;
            ActiveSession?.Dispose();
            ActiveSession = null;
            Room = null;
        }

        private int NewSeed() => Guid.NewGuid().GetHashCode();

        private string MakeServiceId() => "app.minigames.v1";

        private void Transition(AppState next, Action present)
        {
            var prev = State;
            State = next;
            present?.Invoke();
            _services?.Analytics?.Track("nav_transition",
                ("from", prev.ToString()), ("to", next.ToString()));
            StateChanged?.Invoke(prev, next);
        }
    }
}
