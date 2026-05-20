using MiniGames.Games.ColorBlocks.AI;
using MiniGames.Games.ColorBlocks.Logic;
using NUnit.Framework;

namespace MiniGames.Tests.Games.ColorBlocks
{
    public class GreedyColorBlocksAITests
    {
        [Test]
        public void AI_returns_a_legal_move_on_empty_board()
        {
            var g = new ColorBlocksGame(seed: 1);
            var ai = new GreedyColorBlocksAI();
            var move = ai.ChooseMove(g);
            Assert.IsNotNull(move);
            var (h, x, y) = move.Value;
            Assert.IsTrue(g.CanPlay(h, x, y),
                $"AI returned ({h},{x},{y}) which is not a legal move");
        }

        [Test]
        public void AI_prefers_a_line_clear_over_a_non_clearing_move()
        {
            var g = new ColorBlocksGame(seed: 1);
            // Pre-fill row 0 except (9,0) so a dot at (9,0) clears the row.
            for (int x = 0; x < 9; x++) g.Board.Set(x, 0, 1);
            // Put a dot in hand slot 0.
            g.Hand[0] = new PieceShape("dot", 5, new Cell(0, 0));
            // Put another piece in slot 1 so AI has options.
            g.Hand[1] = new PieceShape("h2", 5, new Cell(0, 0), new Cell(1, 0));

            var move = new GreedyColorBlocksAI().ChooseMove(g);
            Assert.IsNotNull(move);
            Assert.AreEqual(0, move.Value.handIndex, "AI should prefer the line-clearing piece");
            Assert.AreEqual(9, move.Value.x);
            Assert.AreEqual(0, move.Value.y);
        }

        [Test]
        public void AI_returns_null_when_no_piece_fits()
        {
            var g = new ColorBlocksGame(seed: 1);
            // Fill the board completely.
            for (int x = 0; x < BoardState.Size; x++)
                for (int y = 0; y < BoardState.Size; y++) g.Board.Set(x, y, 1);
            Assert.IsNull(new GreedyColorBlocksAI().ChooseMove(g));
        }
    }
}
