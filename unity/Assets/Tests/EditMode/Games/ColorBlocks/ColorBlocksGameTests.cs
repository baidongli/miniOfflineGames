using MiniGames.Games.ColorBlocks.Logic;
using NUnit.Framework;

namespace MiniGames.Tests.Games.ColorBlocks
{
    public class ColorBlocksGameTests
    {
        [Test]
        public void Fresh_game_has_three_pieces_in_hand_and_zero_score()
        {
            var g = new ColorBlocksGame(seed: 1);
            Assert.AreEqual(3, g.Hand.Length);
            Assert.AreEqual(0, g.Score);
            Assert.IsFalse(g.IsGameOver);
            foreach (var p in g.Hand) Assert.IsNotNull(p);
        }

        [Test]
        public void Playing_a_piece_removes_it_from_hand_and_adds_score()
        {
            var g = new ColorBlocksGame(seed: 1);
            var shape = g.Hand[0];
            Assert.IsTrue(g.TryPlay(0, 0, 0, out var result));
            Assert.IsNull(g.Hand[0]);
            Assert.AreEqual(shape.CellCount, result.CellsPlaced);
            Assert.AreEqual(shape.CellCount, g.Score);
        }

        [Test]
        public void Hand_refills_after_all_three_played()
        {
            var g = new ColorBlocksGame(seed: 1);
            // Replace with small known shapes so the test is deterministic
            // regardless of the bag's first hand.
            for (int i = 0; i < 3; i++)
                g.Hand[i] = new PieceShape($"dot{i}", (byte)(i + 1), new Cell(0, 0));

            Assert.IsTrue(g.TryPlay(0, 0, 0, out _));
            Assert.IsTrue(g.TryPlay(1, 1, 0, out _));
            Assert.IsTrue(g.TryPlay(2, 2, 0, out _));

            int notNull = 0;
            foreach (var p in g.Hand) if (p != null) notNull++;
            Assert.AreEqual(3, notNull, "hand was not refilled");
        }

        [Test]
        public void Cannot_play_same_slot_twice()
        {
            var g = new ColorBlocksGame(seed: 1);
            Assert.IsTrue(g.TryPlay(0, 0, 0, out _));
            Assert.IsFalse(g.TryPlay(0, 5, 5, out _));
        }

        [Test]
        public void Clearing_a_line_awards_bonus_points()
        {
            var g = new ColorBlocksGame(seed: 1);
            // Pre-fill row 0 except last cell directly on the board.
            for (int x = 0; x < 9; x++) g.Board.Set(x, 0, 1);
            // Find any single-cell piece in the hand, or just place hand[0]
            // such that placement completes the row (only works if shape
            // includes a cell at (0,0) of size 1). Easiest: directly mutate
            // the hand to a known shape for this assertion.
            var single = new PieceShape("dot", 9, new Cell(0,0));
            g.Hand[0] = single;

            Assert.IsTrue(g.TryPlay(0, 9, 0, out var result));
            Assert.AreEqual(1, result.TotalLinesCleared);
            // 1 cell placed + 10 line bonus = 11, no combo for single line.
            Assert.AreEqual(1 + ScoringRules.PointsPerLineCleared, g.Score);
        }
    }
}
