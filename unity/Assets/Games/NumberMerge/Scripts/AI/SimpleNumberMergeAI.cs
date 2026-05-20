using MiniGames.Games.NumberMerge.Logic;

namespace MiniGames.Games.NumberMerge.AI
{
    public interface INumberMergeAI
    {
        SwipeDir? Choose(NumberMergeGame game);
    }

    /// <summary>
    /// 1-ply lookahead: simulate each of the 4 directions on a board copy,
    /// score the result, pick the highest. Heuristic favors:
    ///   - empty cells (fewer empties = closer to game over)
    ///   - max tile in a corner (the classic 2048 strategy)
    ///   - monotonic top row (tiles ordered descending along an edge)
    /// </summary>
    public sealed class SimpleNumberMergeAI : INumberMergeAI
    {
        public SwipeDir? Choose(NumberMergeGame game)
        {
            SwipeDir? best = null;
            int bestScore = int.MinValue;
            foreach (SwipeDir d in new[] { SwipeDir.Down, SwipeDir.Left, SwipeDir.Right, SwipeDir.Up })
            {
                var copy = Snapshot(game.Board);
                var r = NumberMergeEngine.Swipe(copy, d);
                if (!r.AnyMoved) continue;
                int score = Evaluate(copy) + r.ScoreGained;
                if (score > bestScore) { bestScore = score; best = d; }
            }
            return best;
        }

        private static NumberMergeBoard Snapshot(NumberMergeBoard b)
        {
            var copy = new NumberMergeBoard();
            for (int y = 0; y < NumberMergeBoard.Size; y++)
                for (int x = 0; x < NumberMergeBoard.Size; x++)
                    copy.Set(x, y, b.Get(x, y));
            return copy;
        }

        private static int Evaluate(NumberMergeBoard b)
        {
            int s = b.EmptyCount() * 100;
            // Bonus for max tile in a corner.
            byte max = b.MaxExponent();
            if (b.Get(0, 0) == max || b.Get(3, 0) == max ||
                b.Get(0, 3) == max || b.Get(3, 3) == max) s += 500;
            // Monotonic bottom row (left-to-right descending).
            int monoBottom = 0;
            for (int x = 0; x + 1 < NumberMergeBoard.Size; x++)
                if (b.Get(x, 0) >= b.Get(x + 1, 0)) monoBottom += 20;
            return s + monoBottom;
        }
    }

    public sealed class CpuNumberMergeController
    {
        public readonly NumberMergeGame Game;
        public readonly INumberMergeAI Ai;
        public CpuNumberMergeController(NumberMergeGame g, INumberMergeAI a) { Game = g; Ai = a; }
        public bool TakeTurn()
        {
            if (Game.IsGameOver) return false;
            var d = Ai.Choose(Game);
            if (d == null) return false;
            return Game.TrySwipe(d.Value, out _);
        }
    }
}
