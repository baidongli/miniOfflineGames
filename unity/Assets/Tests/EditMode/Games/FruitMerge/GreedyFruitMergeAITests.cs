using MiniGames.Games.FruitMerge.AI;
using MiniGames.Games.FruitMerge.Logic;
using NUnit.Framework;

namespace MiniGames.Tests.Games.FruitMerge
{
    public class GreedyFruitMergeAITests
    {
        [Test]
        public void AI_returns_a_legal_column_on_empty_grid()
        {
            var g = new FruitMergeGame(seed: 1);
            int c = new GreedyFruitMergeAI().ChooseColumn(g);
            Assert.IsTrue(c >= 0 && c < g.Grid.Width);
            Assert.IsTrue(g.TryDrop(c), $"AI's column {c} should be droppable");
        }

        [Test]
        public void AI_picks_the_column_with_a_same_tier_neighbor()
        {
            // Same-tier fruit at (3, 0). Columns that produce an immediate
            // merge: 2 (lands at (2,0), side-adjacent), 3 (lands at (3,1),
            // below-adjacent), 4 (lands at (4,0), side-adjacent). The AI's
            // tie-break prefers lower landings, so cols 2 and 4 (y=0) beat
            // col 3 (y=1). Any of {2, 3, 4} is a "saw the neighbor" pick.
            var g = new FruitMergeGame(seed: 1);
            byte tier = g.NextFruit;
            g.Grid.Set(3, 0, tier);

            int c = new GreedyFruitMergeAI().ChooseColumn(g);
            Assert.IsTrue(c == 2 || c == 3 || c == 4,
                $"AI should target a column adjacent to the same-tier fruit, got {c}");
        }

        [Test]
        public void AI_prefers_low_landings_when_no_merge_is_available()
        {
            var g = new FruitMergeGame(seed: 1);
            // Stack column 0 high; column 1 is empty. NextFruit doesn't match anything.
            for (int y = 0; y < 5; y++) g.Grid.Set(0, y, 9);   // tier 9 -> won't match any low tier
            int c = new GreedyFruitMergeAI().ChooseColumn(g);
            Assert.AreNotEqual(0, c, "AI should avoid the tall column when no merge is possible");
        }
    }
}
