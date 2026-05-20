using MiniGames.GameModule;
using MiniGames.Games.ColorBlocks;
using MiniGames.Games.ColorBlocks.Logic;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Session;
using MiniGames.Networking.Transport;
using NUnit.Framework;

namespace MiniGames.Tests.Networking
{
    /// <summary>
    /// End-to-end: a 2-line clear on player A's local game must result in
    /// 1 junk row appearing on player B's local board, after travelling
    /// through MessagePack -&gt; RoomSendChannel -&gt; RoomManager -&gt;
    /// MockTransport -&gt; peer RoomManager -&gt; GameSession -&gt; ColorBlocksModule.
    /// </summary>
    public class ColorBlocksMultiplayerIntegrationTests
    {
        private static (ColorBlocksModule mod, GameSession session, RoomManager room, MockTransport transport)
            BuildPlayer(string id, string name, MockNetwork net)
        {
            var transport = new MockTransport(id, net);
            var ser = new JsonMessageSerializer();
            var room = new RoomManager(transport, ser, id, name);
            var mod = new ColorBlocksModule();
            var ctx = new GameContext(
                audio: null, save: null, analytics: null, haptics: null,
                net: new RoomSendChannel(room), localPlayerId: id);
            var session = new GameSession(mod, ctx, room);
            return (mod, session, room, transport);
        }

        [Test]
        public void Two_line_clear_on_A_sends_junk_row_to_B()
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
            Assert.IsNotNull(snap);

            a.mod.StartMultiplayer(a.session.Context, snap, seed: 1, isHost: true);
            b.mod.StartMultiplayer(b.session.Context, snap, seed: 1, isHost: false);

            // Mark a cell on B's board so we can prove it shifted up.
            b.mod.MultiplayerGame.Local.Board.Set(5, 0, 4);

            // Engineer a 2-line clear on A: pre-fill row 0 except (0,0) AND
            // column 0 except (0,0), then drop a 1x1 at (0,0) -> both clear.
            var board = a.mod.MultiplayerGame.Local.Board;
            for (int x = 1; x < 10; x++) board.Set(x, 0, 1);
            for (int y = 1; y < 10; y++) board.Set(0, y, 1);
            a.mod.MultiplayerGame.Local.Hand[0] = new PieceShape("dot", 5, new Cell(0, 0));

            Assert.IsTrue(a.mod.MultiplayerGame.Local.TryPlay(0, 0, 0, out var place));
            Assert.AreEqual(2, place.TotalLinesCleared, "the dummied-up move should clear two lines");

            // After the attack message has propagated, B's marker cell (5,0)
            // should have shifted up by 1.
            Assert.AreEqual(4, b.mod.MultiplayerGame.Local.Board.Get(5, 1),
                "B's cell at (5,0) should have shifted to (5,1)");
        }

        [Test]
        public void Single_line_clear_on_A_does_not_attack_B()
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

            a.mod.StartMultiplayer(a.session.Context, snap, seed: 1, isHost: true);
            b.mod.StartMultiplayer(b.session.Context, snap, seed: 1, isHost: false);

            b.mod.MultiplayerGame.Local.Board.Set(5, 0, 4);

            // Single line clear: fill row 0 except last, drop a dot.
            var board = a.mod.MultiplayerGame.Local.Board;
            for (int x = 0; x < 9; x++) board.Set(x, 0, 1);
            a.mod.MultiplayerGame.Local.Hand[0] = new PieceShape("dot", 5, new Cell(0, 0));
            Assert.IsTrue(a.mod.MultiplayerGame.Local.TryPlay(0, 9, 0, out var place));
            Assert.AreEqual(1, place.TotalLinesCleared);

            // B's marker should NOT have moved (single-line clear is not an attack).
            Assert.AreEqual(4, b.mod.MultiplayerGame.Local.Board.Get(5, 0));
        }

        [Test]
        public void Opponent_progress_updates_visible_to_other_peer()
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

            a.mod.StartMultiplayer(a.session.Context, snap, seed: 1, isHost: true);
            b.mod.StartMultiplayer(b.session.Context, snap, seed: 1, isHost: false);

            // A plays any piece; progress should be broadcast to B.
            a.mod.MultiplayerGame.Local.Hand[0] = new PieceShape("dot", 5, new Cell(0, 0));
            Assert.IsTrue(a.mod.MultiplayerGame.Local.TryPlay(0, 0, 0, out _));

            Assert.IsTrue(b.mod.MultiplayerGame.Opponents.ContainsKey("A"),
                "B should track A as an opponent after a progress update");
        }
    }
}
