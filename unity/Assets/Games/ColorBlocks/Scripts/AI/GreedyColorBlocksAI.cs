using MiniGames.Games.ColorBlocks.Logic;

namespace MiniGames.Games.ColorBlocks.AI
{
    public interface IColorBlocksAI
    {
        /// <summary>Returns (handIndex, originX, originY) for the best move, or null if no piece fits.</summary>
        (int handIndex, int x, int y)? ChooseMove(ColorBlocksGame game);
    }

    /// <summary>
    /// Greedy heuristic: for every (piece, position) that fits, score the
    /// resulting board and pick the highest. Score = 50 * lines cleared
    /// + cells placed - placement Y (prefer keeping the board low-density).
    /// </summary>
    public sealed class GreedyColorBlocksAI : IColorBlocksAI
    {
        public (int handIndex, int x, int y)? ChooseMove(ColorBlocksGame game)
        {
            int bestScore = int.MinValue;
            (int, int, int)? best = null;

            for (int h = 0; h < game.Hand.Length; h++)
            {
                var shape = game.Hand[h];
                if (shape == null) continue;
                for (int y = 0; y + shape.Height <= BoardState.Size; y++)
                {
                    for (int x = 0; x + shape.Width <= BoardState.Size; x++)
                    {
                        if (!BoardEngine.CanPlace(game.Board, shape, x, y)) continue;

                        var preview = game.Board.Clone();
                        BoardEngine.TryPlace(preview, shape, x, y, out var r);
                        int score = r.TotalLinesCleared * 50 + shape.CellCount - y;
                        if (score > bestScore)
                        {
                            bestScore = score;
                            best = (h, x, y);
                        }
                    }
                }
            }
            return best;
        }
    }
}
