using MiniGames.Games.ConnectFour.Logic;
using NUnit.Framework;

namespace MiniGames.Tests.Games.ConnectFour
{
    public class ConnectFourBoardTests
    {
        [Test]
        public void Drop_lands_at_lowest_empty_row()
        {
            var b = new ConnectFourBoard();
            Assert.AreEqual(0, b.Drop(3, ConnectFourBoard.PlayerA));
            Assert.AreEqual(1, b.Drop(3, ConnectFourBoard.PlayerB));
            Assert.AreEqual(ConnectFourBoard.PlayerA, b.Get(3, 0));
            Assert.AreEqual(ConnectFourBoard.PlayerB, b.Get(3, 1));
        }

        [Test]
        public void Drop_returns_minus_one_when_column_full()
        {
            var b = new ConnectFourBoard(width: 1, height: 3, winLength: 3);
            Assert.AreEqual(0, b.Drop(0, 1));
            Assert.AreEqual(1, b.Drop(0, 1));
            Assert.AreEqual(2, b.Drop(0, 1));
            Assert.AreEqual(-1, b.Drop(0, 1));
        }

        [Test]
        public void Horizontal_four_in_a_row_wins()
        {
            var b = new ConnectFourBoard();
            for (int x = 0; x < 4; x++) b.Drop(x, ConnectFourBoard.PlayerA);
            Assert.IsTrue(b.IsWinAt(0, 0));
            Assert.IsTrue(b.IsWinAt(3, 0));
        }

        [Test]
        public void Vertical_four_in_a_row_wins()
        {
            var b = new ConnectFourBoard();
            for (int y = 0; y < 4; y++) b.Drop(0, ConnectFourBoard.PlayerB);
            Assert.IsTrue(b.IsWinAt(0, 3));
        }

        [Test]
        public void Diagonal_four_in_a_row_wins()
        {
            // Build  P at (0,0) (1,1) (2,2) (3,3) - need padding to lift them
            var b = new ConnectFourBoard();
            // Pad column 1 with one opponent at bottom.
            b.Drop(1, ConnectFourBoard.PlayerB);
            // Pad column 2 with two opponents.
            b.Drop(2, ConnectFourBoard.PlayerB);
            b.Drop(2, ConnectFourBoard.PlayerB);
            // Pad column 3 with three opponents.
            b.Drop(3, ConnectFourBoard.PlayerB);
            b.Drop(3, ConnectFourBoard.PlayerB);
            b.Drop(3, ConnectFourBoard.PlayerB);
            // Now drop our pieces.
            b.Drop(0, ConnectFourBoard.PlayerA); // (0,0)
            b.Drop(1, ConnectFourBoard.PlayerA); // (1,1)
            b.Drop(2, ConnectFourBoard.PlayerA); // (2,2)
            b.Drop(3, ConnectFourBoard.PlayerA); // (3,3)
            Assert.IsTrue(b.IsWinAt(3, 3));
        }

        [Test]
        public void IsFull_reports_when_all_columns_full()
        {
            var b = new ConnectFourBoard(width: 2, height: 2, winLength: 3);
            Assert.IsFalse(b.IsFull());
            b.Drop(0, 1); b.Drop(0, 1);
            b.Drop(1, 1); b.Drop(1, 1);
            Assert.IsTrue(b.IsFull());
        }
    }

    public class ConnectFourGameTests
    {
        [Test]
        public void Players_alternate_turns()
        {
            var g = new ConnectFourGame();
            Assert.AreEqual(ConnectFourBoard.PlayerA, g.CurrentPlayer);
            g.TryPlay(0, out _);
            Assert.AreEqual(ConnectFourBoard.PlayerB, g.CurrentPlayer);
            g.TryPlay(1, out _);
            Assert.AreEqual(ConnectFourBoard.PlayerA, g.CurrentPlayer);
        }

        [Test]
        public void TryPlay_into_full_column_returns_false_without_advancing_turn()
        {
            var g = new ConnectFourGame(width: 1, height: 2, winLength: 2);
            g.TryPlay(0, out _);  // A
            g.TryPlay(0, out _);  // B - column now full
            byte before = g.CurrentPlayer;
            Assert.IsFalse(g.TryPlay(0, out _));
            Assert.AreEqual(before, g.CurrentPlayer, "rejected move should not advance the turn");
        }

        [Test]
        public void Winning_move_sets_result()
        {
            var g = new ConnectFourGame();
            // A plays (0,0); B plays (1,0); A (1,1); B (2,0); A (2,1); B (3,0); A (2,2); B (4,0); A (3,1)... too tangled.
            // Easier: drive a horizontal win directly.
            // Sequence: A at col 0,1,2,3 and B at col 0,1,2 between.
            // A:0, B:0, A:1, B:1, A:2, B:2, A:3 -> A wins horizontally at row 1.
            int[] seq = { 0, 0, 1, 1, 2, 2, 3 };
            foreach (var c in seq) g.TryPlay(c, out _);
            Assert.AreEqual(GameResult.PlayerAWins, g.Result);
            Assert.IsTrue(g.IsGameOver);
        }

        [Test]
        public void TryPlay_returns_false_after_game_over()
        {
            var g = new ConnectFourGame();
            int[] seq = { 0, 0, 1, 1, 2, 2, 3 };  // A wins
            foreach (var c in seq) g.TryPlay(c, out _);
            Assert.IsTrue(g.IsGameOver);
            Assert.IsFalse(g.TryPlay(4, out _));
        }

        [Test]
        public void ConcedeFrom_A_makes_B_win()
        {
            var g = new ConnectFourGame();
            g.ConcedeFrom(ConnectFourBoard.PlayerA);
            Assert.AreEqual(GameResult.PlayerBWins, g.Result);
            Assert.IsTrue(g.IsGameOver);
        }

        [Test]
        public void Draw_when_board_fills_with_no_winner()
        {
            // Tiny 2x2 board, winLength 3 (impossible). Fill it.
            var g = new ConnectFourGame(width: 2, height: 2, winLength: 3);
            g.TryPlay(0, out _);  // A (0,0)
            g.TryPlay(1, out _);  // B (1,0)
            g.TryPlay(1, out _);  // A (1,1)
            g.TryPlay(0, out _);  // B (0,1) - board full
            Assert.AreEqual(GameResult.Draw, g.Result);
        }
    }
}
