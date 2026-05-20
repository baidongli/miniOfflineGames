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
            // Set up: same-tier fruit at column 3 row 0; everything else
            // empty. NextFruit is what TestSetup() forces.
            var g = new FruitMergeGame(seed: 1);
            byte tier = g.NextFruit;
            g.Grid.Set(3, 0, tier);  // a same-tier match if AI drops into col 3, lands at (3,1)
            // Wait - dropping at col 3 lands at (3,1) since (3,0) is occupied;
            // (3,0) is below (3,1) -> they're adjacent. Score = +10 (below).

            int c = new GreedyFruitMergeAI().ChooseColumn(g);
            Assert.AreEqual(3, c, "AI should target the column with adjacent same-tier fruit");
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
