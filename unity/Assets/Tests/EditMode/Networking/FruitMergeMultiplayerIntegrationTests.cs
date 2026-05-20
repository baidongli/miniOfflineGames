using MiniGames.GameModule;
using MiniGames.Games.FruitMerge;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Session;
using MiniGames.Networking.Transport;
using NUnit.Framework;

namespace MiniGames.Tests.Networking
{
    public class FruitMergeMultiplayerIntegrationTests
    {
        private static (FruitMergeModule mod, GameSession session, RoomManager room, MockTransport transport)
            BuildPlayer(string id, string name, MockNetwork net)
        {
            var t = new MockTransport(id, net);
            var ser = new JsonMessageSerializer();
            var room = new RoomManager(t, ser, id, name);
            var mod = new FruitMergeModule();
            var ctx = new GameContext(null, null, null, null,
                new RoomSendChannel(room), id);
            var session = new GameSession(mod, ctx, room);
            return (mod, session, room, t);
        }

        [Test]
        public void Drop_on_A_appears_in_B_Opponents_view()
        {
            var net = new MockNetwork();
            var a = BuildPlayer("A", "Alice", net);
            var b = BuildPlayer("B", "Bob", net);

            RoomSnapshot snap = null;
            b.room.SnapshotChanged += s => snap = s;
            a.room.HostRoom("svc");
            b.room.JoinDiscovery("svc");
            b.transport.RequestConnection(new PeerId("A"));
            a.transport.AcceptConnection(new PeerId("B"));

            a.mod.StartMultiplayer(a.session.Context, snap, seed: 42, isHost: true);
            b.mod.StartMultiplayer(b.session.Context, snap, seed: 42, isHost: false);

            // A drops into column 3. B should see the opponent's drop.
            Assert.IsTrue(a.mod.MultiplayerGame.TryDrop(3));
            Assert.IsTrue(b.mod.MultiplayerGame.Opponents.ContainsKey("A"));
            Assert.AreEqual(3, b.mod.MultiplayerGame.Opponents["A"].LastColumnDropped);
        }

        [Test]
        public void Both_players_see_same_NextFruit_with_same_seed()
        {
            var net = new MockNetwork();
            var a = BuildPlayer("A", "Alice", net);
            var b = BuildPlayer("B", "Bob", net);

            RoomSnapshot snap = null;
            b.room.SnapshotChanged += s => snap = s;
            a.room.HostRoom("svc");
            b.room.JoinDiscovery("svc");
            b.transport.RequestConnection(new PeerId("A"));
            a.transport.AcceptConnection(new PeerId("B"));

            a.mod.StartMultiplayer(a.session.Context, snap, seed: 77, isHost: true);
            b.mod.StartMultiplayer(b.session.Context, snap, seed: 77, isHost: false);

            // Deterministic NextFruit sequence on both sides.
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(a.mod.MultiplayerGame.Local.NextFruit,
                                b.mod.MultiplayerGame.Local.NextFruit,
                                $"diverged on draw {i}");
                Assert.IsTrue(a.mod.MultiplayerGame.TryDrop(i % 7));
                Assert.IsTrue(b.mod.MultiplayerGame.TryDrop(i % 7));
            }
        }
    }
}
