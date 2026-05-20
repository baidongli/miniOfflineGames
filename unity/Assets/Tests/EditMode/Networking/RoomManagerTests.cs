using MiniGames.Networking.Protocol;
using MiniGames.Networking.Session;
using MiniGames.Networking.Transport;
using NUnit.Framework;

namespace MiniGames.Tests.Networking
{
    public class RoomManagerTests
    {
        private MockNetwork _net;
        private MockTransport _hostT;
        private MockTransport _clientT;
        private RoomManager _hostRoom;
        private RoomManager _clientRoom;
        private MessagePackMessageSerializer _ser;

        [SetUp]
        public void Setup()
        {
            _net = new MockNetwork();
            _hostT = new MockTransport("host", _net);
            _clientT = new MockTransport("c1", _net);
            _ser = new MessagePackMessageSerializer();
            _hostRoom = new RoomManager(_hostT, _ser, localPlayerId: "host", localDisplayName: "Host");
            _clientRoom = new RoomManager(_clientT, _ser, localPlayerId: "c1", localDisplayName: "Bob");
        }

        private void Pair()
        {
            _hostRoom.HostRoom("svc");
            _clientRoom.JoinDiscovery("svc");
            _clientT.RequestConnection(new PeerId("host"));
            // MockTransport.AcceptConnection fires Ok on both sides; one call
            // is enough to complete the handshake.
            _hostT.AcceptConnection(new PeerId("c1"));
        }

        [Test]
        public void Client_hello_results_in_host_broadcasting_snapshot_with_two_players()
        {
            RoomSnapshot lastSeenByClient = null;
            _clientRoom.SnapshotChanged += s => lastSeenByClient = s;

            Pair();

            Assert.IsNotNull(lastSeenByClient, "client should have received at least one snapshot");
            Assert.AreEqual(2, lastSeenByClient.Players.Count);
            // Host listed first by RoomManager.BuildSnapshot.
            Assert.IsTrue(lastSeenByClient.Players[0].IsHost);
            Assert.AreEqual("host", lastSeenByClient.Players[0].PlayerId);
            Assert.AreEqual("c1", lastSeenByClient.Players[1].PlayerId);
            Assert.AreEqual("Bob", lastSeenByClient.Players[1].DisplayName);
        }

        [Test]
        public void Host_select_game_propagates_to_client()
        {
            RoomSnapshot last = null;
            _clientRoom.SnapshotChanged += s => last = s;
            Pair();

            _hostRoom.SelectGame("color_blocks");

            Assert.AreEqual("color_blocks", last.SelectedGameId);
        }

        [Test]
        public void Host_StartGame_raises_GameStarting_on_both_sides()
        {
            Pair();
            _hostRoom.SelectGame("snakes");

            StartGame hostFire = null, clientFire = null;
            _hostRoom.GameStarting += m => hostFire = m;
            _clientRoom.GameStarting += m => clientFire = m;

            _hostRoom.StartGame(countdownMs: 1500, seed: 99);

            Assert.IsNotNull(hostFire);
            Assert.AreEqual("snakes", hostFire.GameId);
            Assert.AreEqual(99, hostFire.Seed);

            Assert.IsNotNull(clientFire);
            Assert.AreEqual("snakes", clientFire.GameId);
            Assert.AreEqual(99, clientFire.Seed);
        }

        [Test]
        public void Non_host_StartGame_is_ignored()
        {
            Pair();
            _hostRoom.SelectGame("snakes");

            bool fired = false;
            _hostRoom.GameStarting += _ => fired = true;
            _clientRoom.StartGame(countdownMs: 1000, seed: 1);

            Assert.IsFalse(fired, "client cannot start game");
        }

        [Test]
        public void Disconnected_client_drops_from_snapshot()
        {
            Pair();

            RoomSnapshot last = null;
            _hostRoom.SnapshotChanged += s => last = s;

            _hostT.Disconnect(new PeerId("c1"));

            Assert.IsNotNull(last);
            Assert.AreEqual(1, last.Players.Count, "client should be removed");
            Assert.AreEqual("host", last.Players[0].PlayerId);
        }
    }
}
