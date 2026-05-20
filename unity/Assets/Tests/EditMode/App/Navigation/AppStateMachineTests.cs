using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MiniGames.App.Bootstrap;
using MiniGames.App.Navigation;
using MiniGames.App.Shared.Energy;
using MiniGames.GameModule;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Transport;
using NUnit.Framework;

namespace MiniGames.Tests.App.Navigation
{
    public class AppStateMachineTests
    {
        private sealed class FakeView : IAppView
        {
            public List<string> Calls = new List<string>();
            public IGameModule LastGame;
            public PlayMode? LastMode;
            public bool? LastIsHost;
            public GameResults LastResults;

            public void ShowBoot() => Calls.Add("Boot");
            public void ShowHub(IReadOnlyList<IGameModule> games) => Calls.Add("Hub");
            public void ShowGameSelect(IGameModule game) { Calls.Add("GameSelect"); LastGame = game; }
            public void ShowLobby(IGameModule game, bool isHost) { Calls.Add("Lobby"); LastGame = game; LastIsHost = isHost; }
            public void ShowInGame(IGameModule game, PlayMode mode) { Calls.Add("InGame"); LastGame = game; LastMode = mode; }
            public void ShowResults(IGameModule game, GameResults r) { Calls.Add("Results"); LastResults = r; }
        }

        private sealed class FakeGame : IGameModule
        {
            public string Id { get; }
            public string DisplayName => Id;
            public GameCapabilities Capabilities { get; }
            public int MinPlayers => 2;
            public int MaxPlayers => 4;
            public bool StartedSolo, StartedMP;

            public FakeGame(string id, GameCapabilities caps) { Id = id; Capabilities = caps; }

            public Task LoadAsync(GameContext ctx) => Task.CompletedTask;
            public void StartSolo(GameContext ctx) => StartedSolo = true;
            public void StartMultiplayer(GameContext ctx, RoomSnapshot r, int s, bool h) => StartedMP = true;
            public void Pause() { } public void Resume() { }
            public Task UnloadAsync() => Task.CompletedTask;
            public void OnPeerMessage(PeerId from, MessageType type, ArraySegment<byte> payload) { }
            public void OnPeerJoined(PeerId peer) { }
            public void OnPeerLeft(PeerId peer) { }
        }

        private FakeView _view;
        private FakeGame _gameSoloMp;
        private List<IGameModule> _games;
        private AppServices _services;
        private AppStateMachine _sm;

        [SetUp]
        public void Setup()
        {
            _view = new FakeView();
            _gameSoloMp = new FakeGame("test", GameCapabilities.Solo | GameCapabilities.Multiplayer | GameCapabilities.SameDevice);
            _games = new List<IGameModule> { _gameSoloMp };
            _services = new AppServices
            {
                Transport = new MockTransport("local", new MockNetwork()),
                Serializer = new MessagePackMessageSerializer(),
                LocalPlayerId = "me",
                LocalDisplayName = "Me",
                Energy = new EnergyTimer(5, TimeSpan.FromMinutes(15), EnergyState.Fresh(5)),
            };
            _sm = new AppStateMachine(_services, _view, _games);
        }

        [Test]
        public void Boots_to_Boot_then_GoToHub_shows_Hub()
        {
            Assert.AreEqual(AppState.Boot, _sm.State);
            CollectionAssert.AreEqual(new[] { "Boot" }, _view.Calls);

            _sm.GoToHub();
            Assert.AreEqual(AppState.Hub, _sm.State);
            CollectionAssert.AreEqual(new[] { "Boot", "Hub" }, _view.Calls);
        }

        [Test]
        public void SelectGame_only_from_Hub()
        {
            _sm.SelectGame(_gameSoloMp);
            // Still in Boot, transition rejected.
            Assert.AreEqual(AppState.Boot, _sm.State);

            _sm.GoToHub();
            _sm.SelectGame(_gameSoloMp);
            Assert.AreEqual(AppState.GameSelect, _sm.State);
            Assert.AreSame(_gameSoloMp, _sm.SelectedGame);
        }

        [Test]
        public void StartSolo_routes_to_InGame_and_calls_module_StartSolo()
        {
            _sm.GoToHub();
            _sm.SelectGame(_gameSoloMp);
            _sm.StartSolo();
            Assert.AreEqual(AppState.InGame, _sm.State);
            Assert.AreEqual(PlayMode.Solo, _sm.CurrentMode);
            Assert.IsTrue(_gameSoloMp.StartedSolo);
            Assert.IsFalse(_gameSoloMp.StartedMP);
        }

        [Test]
        public void StartSameDevice_calls_StartMultiplayer_with_pseudo_snapshot()
        {
            _sm.GoToHub();
            _sm.SelectGame(_gameSoloMp);
            _sm.StartSameDevice();
            Assert.AreEqual(AppState.InGame, _sm.State);
            Assert.IsTrue(_gameSoloMp.StartedMP);
        }

        [Test]
        public void HostNearby_goes_to_Lobby_as_host()
        {
            _sm.GoToHub();
            _sm.SelectGame(_gameSoloMp);
            _sm.HostNearby();
            Assert.AreEqual(AppState.Lobby, _sm.State);
            Assert.IsNotNull(_sm.Room);
            Assert.IsTrue(_sm.Room.IsHost);
            Assert.AreEqual(true, _view.LastIsHost);
        }

        [Test]
        public void LeaveLobby_returns_to_Hub()
        {
            _sm.GoToHub();
            _sm.SelectGame(_gameSoloMp);
            _sm.HostNearby();
            _sm.LeaveLobby();
            Assert.AreEqual(AppState.Hub, _sm.State);
            Assert.IsNull(_sm.Room);
        }

        [Test]
        public void EndGame_goes_to_Results_then_DismissResults_returns_to_Hub()
        {
            _sm.GoToHub();
            _sm.SelectGame(_gameSoloMp);
            _sm.StartSolo();
            _sm.EndGame(new GameResults
            {
                LocalPlayerId = "me",
                Players = new List<PlayerResult>
                {
                    new PlayerResult { PlayerId = "me", Score = 100, Place = 1 }
                }
            });
            Assert.AreEqual(AppState.Results, _sm.State);
            Assert.AreEqual(100, _view.LastResults.Players[0].Score);

            _sm.DismissResults();
            Assert.AreEqual(AppState.Hub, _sm.State);
        }

        [Test]
        public void Game_with_only_solo_capability_cannot_StartSameDevice()
        {
            var soloOnly = new FakeGame("solo_only", GameCapabilities.Solo);
            var games = new List<IGameModule> { soloOnly };
            var sm = new AppStateMachine(_services, new FakeView(), games);
            sm.GoToHub();
            sm.SelectGame(soloOnly);
            sm.StartSameDevice();
            Assert.AreEqual(AppState.GameSelect, sm.State, "should refuse SameDevice without the capability");
        }
    }
}
