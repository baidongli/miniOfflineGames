using MiniGames.Games.Reversi.AI;
using MiniGames.Games.Reversi.Logic;
using MiniGames.Games.Reversi.Multiplayer;
using NUnit.Framework;

namespace MiniGames.Tests.Games.Reversi
{
    public class ReversiBoardTests
    {
        [Test]
        public void Initial_board_has_four_center_pieces()
        {
            var b = new ReversiBoard();
            Assert.AreEqual(ReversiBoard.White, b.Get(3, 3));
            Assert.AreEqual(ReversiBoard.White, b.Get(4, 4));
            Assert.AreEqual(ReversiBoard.Black, b.Get(3, 4));
            Assert.AreEqual(ReversiBoard.Black, b.Get(4, 3));
            Assert.AreEqual(2, b.Count(ReversiBoard.Black));
            Assert.AreEqual(2, b.Count(ReversiBoard.White));
        }

        [Test]
        public void Black_opens_with_four_legal_moves()
        {
            var b = new ReversiBoard();
            var moves = b.LegalMoves(ReversiBoard.Black);
            Assert.AreEqual(4, moves.Count, "standard opening has 4 legal Black moves");
        }

        [Test]
        public void FlipsFor_returns_the_flanked_pieces()
        {
            var b = new ReversiBoard();
            // Black plays (3, 5): flanks the White at (3, 4)? No - that's same column,
            // but there's no Black piece above to sandwich. Try (2, 4): Black at (4, 4)?
            // Standard opening: Black at (4, 3) and (3, 4); White at (3, 3) and (4, 4).
            // Black plays (5, 4): direction (-1, 0) walks (4, 4)=White, then (3, 4)=Black.
            // So it sandwiches the white at (4, 4) -> flip.
            var flips = b.FlipsFor(5, 4, ReversiBoard.Black);
            CollectionAssert.Contains(flips, (4, 4));
        }

        [Test]
        public void Illegal_move_returns_empty_flip_list()
        {
            var b = new ReversiBoard();
            // (0, 0) has no adjacent opponent piece; can't flip anything.
            Assert.IsEmpty(b.FlipsFor(0, 0, ReversiBoard.Black));
        }
    }

    public class ReversiGameTests
    {
        [Test]
        public void Black_moves_first()
        {
            var g = new ReversiGame();
            Assert.AreEqual(ReversiBoard.Black, g.CurrentPlayer);
        }

        [Test]
        public void Successful_move_advances_turn_and_flips_pieces()
        {
            var g = new ReversiGame();
            Assert.IsTrue(g.TryPlay(5, 4, out var r));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(ReversiBoard.Black, g.Board.Get(5, 4));
            Assert.AreEqual(ReversiBoard.Black, g.Board.Get(4, 4), "the flanked white piece should be Black now");
            Assert.AreEqual(ReversiBoard.White, g.CurrentPlayer);
        }

        [Test]
        public void Illegal_move_is_rejected_without_advancing_turn()
        {
            var g = new ReversiGame();
            Assert.IsFalse(g.TryPlay(0, 0, out _));
            Assert.AreEqual(ReversiBoard.Black, g.CurrentPlayer);
        }

        [Test]
        public void ConcedeFrom_black_makes_white_win()
        {
            var g = new ReversiGame();
            g.ConcedeFrom(ReversiBoard.Black);
            Assert.AreEqual(ReversiResult.WhiteWins, g.Result);
        }
    }

    public class MultiplayerReversiTests
    {
        [Test]
        public void Seat_assignment_by_player_id_is_deterministic()
        {
            var a = new MultiplayerReversi("alice", "bob");
            var b = new MultiplayerReversi("bob", "alice");
            Assert.AreEqual(ReversiBoard.Black, a.LocalSeat);
            Assert.AreEqual(ReversiBoard.White, b.LocalSeat);
        }

        [Test]
        public void Move_broadcast_and_apply_keeps_two_peers_in_sync()
        {
            var alice = new MultiplayerReversi("alice", "bob");
            var bob   = new MultiplayerReversi("bob", "alice");
            alice.MoveOutgoing += m => bob.OnMoveReceived(m);
            bob.MoveOutgoing   += m => alice.OnMoveReceived(m);

            Assert.IsTrue(alice.TryLocalPlay(5, 4));   // Alice = Black, opens.
            // Bob is White; needs a legal move.
            var bobMoves = bob.Game.Board.LegalMoves(ReversiBoard.White);
            Assert.IsNotEmpty(bobMoves);
            Assert.IsTrue(bob.TryLocalPlay(bobMoves[0].x, bobMoves[0].y));

            // Boards equal.
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    Assert.AreEqual(alice.Game.Board.Get(x, y), bob.Game.Board.Get(x, y));
        }
    }

    public class MinimaxReversiAITests
    {
        [Test]
        public void AI_returns_a_legal_move_on_opening_board()
        {
            var g = new ReversiGame();
            var move = new MinimaxReversiAI(depth: 2).Choose(g);
            Assert.IsNotNull(move);
            Assert.IsNotEmpty(g.Board.FlipsFor(move.Value.x, move.Value.y, ReversiBoard.Black));
        }

        [Test]
        public void Self_play_terminates()
        {
            var g = new ReversiGame();
            var ai = new MinimaxReversiAI(depth: 2);
            var cpu = new CpuReversiController(g, ai);
            int safety = 200;
            while (!g.IsGameOver && safety-- > 0) cpu.TakeTurn();
            Assert.IsTrue(g.IsGameOver, "AI self-play should terminate");
            Assert.AreNotEqual(ReversiResult.InProgress, g.Result);
        }
    }
}
