using MiniGames.Games.FruitMerge.Logic;

namespace MiniGames.Games.FruitMerge.AI
{
    public interface IFruitMergeAI
    {
        int ChooseColumn(FruitMergeGame game);
    }

    /// <summary>
    /// Greedy by adjacency: scores each column by how many same-tier
    /// neighbors the dropped fruit would have at its landing spot. Prefers
    /// lower landings to keep the stack short. Returns column 0 if every
    /// column is full (shouldn't happen during legal play).
    /// </summary>
    public sealed class GreedyFruitMergeAI : IFruitMergeAI
    {
        public int ChooseColumn(FruitMergeGame g)
        {
            byte tier = g.NextFruit;
            int bestScore = int.MinValue;
            int best = 0;

            for (int c = 0; c < g.Grid.Width; c++)
            {
                int y = g.Grid.LowestEmptyY(c);
                if (y >= g.Grid.Height) continue;

                int score = 0;
                // Same-tier neighbors at the landing spot make immediate merge.
                if (y > 0 && g.Grid.Get(c, y - 1) == tier) score += 10;
                if (c > 0 && g.Grid.Get(c - 1, y) == tier) score += 10;
                if (c < g.Grid.Width - 1 && g.Grid.Get(c + 1, y) == tier) score += 10;
                // Prefer landing low (avoid tall stacks).
                score -= y * 2;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }
            return best;
        }
    }
}
