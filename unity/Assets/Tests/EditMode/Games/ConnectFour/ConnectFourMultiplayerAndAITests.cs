using MiniGames.Games.ConnectFour.AI;
using MiniGames.Games.ConnectFour.Logic;
using MiniGames.Games.ConnectFour.Multiplayer;
using NUnit.Framework;

namespace MiniGames.Tests.Games.ConnectFour
{
    public class MultiplayerConnectFourTests
    {
        [Test]
        public void Seat_assignment_is_deterministic_by_player_id()
        {
            // "alice" < "bob" lexicographically -> alice is PlayerA
            var aliceView = new MultiplayerConnectFour("alice", "bob");
            var bobView = new MultiplayerConnectFour("bob", "alice");

            Assert.AreEqual(ConnectFourBoard.PlayerA, aliceView.LocalSeat);
            Assert.AreEqual(ConnectFourBoard.PlayerB, aliceView.RemoteSeat);
            Assert.AreEqual(ConnectFourBoard.PlayerB, bobView.LocalSeat);
            Assert.AreEqual(ConnectFourBoard.PlayerA, bobView.RemoteSeat);
        }

        [Test]
        public void Local_can_only_play_on_own_turn()
        {
            var alice = new MultiplayerConnectFour("alice", "bob");  // PlayerA
            var bob   = new MultiplayerConnectFour("bob", "alice");  // PlayerB

            Assert.IsTrue(alice.IsLocalTurn);
            Assert.IsFalse(bob.IsLocalTurn);

            // Bob tries first - rejected.
            Assert.IsFalse(bob.TryLocalPlay(3));
        }

        [Test]
        public void Move_outgoing_is_broadcast_then_applied_locally()
        {
            var alice = new MultiplayerConnectFour("alice", "bob");
            MoveMessage emitted = null;
            alice.MoveOutgoing += m => emitted = m;

            Assert.IsTrue(alice.TryLocalPlay(3));
            Assert.IsNotNull(emitted);
            Assert.AreEqual("alice", emitted.PlayerId);
            Assert.AreEqual(3, emitted.Column);
            Assert.AreEqual(1, emitted.MoveNumber);
            Assert.AreEqual(ConnectFourBoard.PlayerA, alice.Game.Board.Get(3, 0));
            Assert.IsFalse(alice.IsLocalTurn, "turn should have passed to opponent");
        }

        [Test]
        public void Two_peers_stay_in_sync_through_a_short_game()
        {
            var alice = new MultiplayerConnectFour("alice", "bob");
            var bob = new MultiplayerConnectFour("bob", "alice");

            // Wire the outgoing events to deliver to the other side.
            alice.MoveOutgoing += m => bob.OnMoveReceived(m);
            bob.MoveOutgoing += m => alice.OnMoveReceived(m);

            // Alice = A, plays col 0. Bob = B, plays col 1. Repeat.
            Assert.IsTrue(alice.TryLocalPlay(0));  // A
            Assert.IsTrue(bob.TryLocalPlay(1));    // B
            Assert.IsTrue(alice.TryLocalPlay(0));  // A
            Assert.IsTrue(bob.TryLocalPlay(1));    // B

            // Boards should match.
            for (int y = 0; y < alice.Game.Board.Height; y++)
                for (int x = 0; x < alice.Game.Board.Width; x++)
                    Assert.AreEqual(alice.Game.Board.Get(x, y), bob.Game.Board.Get(x, y));
        }

        [Test]
        public void Out_of_order_move_message_is_dropped()
        {
            var alice = new MultiplayerConnectFour("alice", "bob");
            // Fake a move from bob with MoveNumber 5 (way ahead).
            alice.OnMoveReceived(new MoveMessage { PlayerId = "bob", Column = 3, MoveNumber = 5 });
            // Board should remain empty.
            for (int x = 0; x < alice.Game.Board.Width; x++)
                Assert.AreEqual(ConnectFourBoard.Empty, alice.Game.Board.Get(x, 0));
        }

        [Test]
        public void Resign_ends_the_game_for_both_sides()
        {
            var alice = new MultiplayerConnectFour("alice", "bob");
            var bob = new MultiplayerConnectFour("bob", "alice");
            alice.ResignOutgoing += m => bob.OnResignReceived(m);

            alice.Resign();

            Assert.IsTrue(alice.Game.IsGameOver);
            Assert.IsTrue(bob.Game.IsGameOver);
            Assert.AreEqual(GameResult.PlayerBWins, alice.Game.Result);
            Assert.AreEqual(GameResult.PlayerBWins, bob.Game.Result);
        }
    }

    public class MinimaxConnectFourAITests
    {
        [Test]
        public void AI_plays_a_winning_move_when_one_exists()
        {
            // Set up: A has 3 in a row at (0,0)(1,0)(2,0). B to move - should block at (3,0).
            var g = new ConnectFourGame();
            g.TryPlay(0, out _);  // A
            g.TryPlay(0, out _);  // B
            g.TryPlay(1, out _);  // A
            g.TryPlay(1, out _);  // B
            g.TryPlay(2, out _);  // A - now A has 3 in row 0
            // B's turn now. Without blocking, A wins next.
            var ai = new MinimaxConnectFourAI(depth: 3);
            int col = ai.ChooseColumn(g);
            Assert.AreEqual(3, col, "AI should block A's three-in-a-row");
        }

        [Test]
        public void CpuController_can_play_a_full_game_against_itself()
        {
            var g = new ConnectFourGame();
            var ai = new MinimaxConnectFourAI(depth: 2);
            var cpu = new CpuConnectFourController(g, ai);
            int safety = 200;
            while (!g.IsGameOver && safety-- > 0) cpu.TakeTurn();
            Assert.IsTrue(g.IsGameOver, "self-play should terminate");
        }
    }
}
