using MiniGames.Games.FruitMerge.Logic;
using NUnit.Framework;

namespace MiniGames.Tests.Games.FruitMerge
{
    public class FruitMergeEngineTests
    {
        [Test]
        public void Drop_into_empty_column_lands_at_bottom()
        {
            var g = new FruitGrid(7, 12);
            var r = FruitMergeEngine.Drop(g, column: 3, tier: 1);
            Assert.IsTrue(r.Placed);
            Assert.AreEqual(0, r.PlacedY);
            Assert.AreEqual(1, g.Get(3, 0));
        }

        [Test]
        public void Drop_stacks_on_existing()
        {
            var g = new FruitGrid(7, 12);
            FruitMergeEngine.Drop(g, 3, 1);
            FruitMergeEngine.Drop(g, 3, 2);   // tier 2, no merge with tier 1
            Assert.AreEqual(1, g.Get(3, 0));
            Assert.AreEqual(2, g.Get(3, 1));
        }

        [Test]
        public void Two_same_tier_adjacent_merge_into_next_tier()
        {
            var g = new FruitGrid(7, 12);
            // Manually set up: (3,0)=1, drop 1 at column 4 (lands at (4,0)).
            g.Set(3, 0, 1);
            var r = FruitMergeEngine.Drop(g, 4, 1);
            Assert.IsTrue(r.Placed);
            Assert.AreEqual(1, r.MergesPerformed);
            // After merge: one cell with tier 2 at the merged anchor.
            int count1 = 0, count2 = 0;
            for (int y = 0; y < g.Height; y++)
                for (int x = 0; x < g.Width; x++)
                {
                    if (g.Get(x, y) == 1) count1++;
                    if (g.Get(x, y) == 2) count2++;
                }
            Assert.AreEqual(0, count1, "tier 1 cells should be consumed");
            Assert.AreEqual(1, count2, "one tier 2 cell should remain");
            Assert.AreEqual(2, r.HighestTierReached);
        }

        [Test]
        public void Three_in_a_row_collapse_into_one_higher_tier()
        {
            var g = new FruitGrid(7, 12);
            g.Set(0, 0, 1); g.Set(1, 0, 1);
            var r = FruitMergeEngine.Drop(g, 2, 1);
            Assert.IsTrue(r.Placed);
            // Three 1s -> one 2. Score = 1 * 3 = 3.
            Assert.AreEqual(3, r.Score);
            Assert.AreEqual(1, r.MergesPerformed);
        }

        [Test]
        public void Chain_reaction_merges_keep_going()
        {
            // (0,0)=1, (1,0)=2. Drop tier 1 into column 0 -> lands at (0,1).
            // Round 1: (0,0)+(0,1) tier 1 -> tier 2 at (0,0).
            // Round 2: (0,0)+(1,0) tier 2 -> tier 3 at (0,0).
            // Round 3: nothing.
            var g = new FruitGrid(7, 12);
            g.Set(0, 0, 1); g.Set(1, 0, 2);
            var r = FruitMergeEngine.Drop(g, 0, 1);
            Assert.AreEqual(2, r.MergesPerformed, "should chain twice");
            Assert.AreEqual(3, g.Get(0, 0));
            Assert.AreEqual(0, g.Get(1, 0));
            Assert.AreEqual(3, r.HighestTierReached);
        }

        [Test]
        public void Drop_into_full_column_reports_game_over()
        {
            var g = new FruitGrid(7, 12);
            for (int y = 0; y < g.Height; y++) g.Set(0, y, 9);
            var r = FruitMergeEngine.Drop(g, 0, 1);
            Assert.IsFalse(r.Placed);
            Assert.IsTrue(r.GameOver);
        }

        [Test]
        public void Drop_out_of_column_range_is_game_over()
        {
            var g = new FruitGrid(7, 12);
            Assert.IsTrue(FruitMergeEngine.Drop(g, -1, 1).GameOver);
            Assert.IsTrue(FruitMergeEngine.Drop(g, 7, 1).GameOver);
        }

        [Test]
        public void Score_for_simple_merge_is_tier_times_count()
        {
            var g = new FruitGrid(7, 12);
            g.Set(3, 0, 2);
            var r = FruitMergeEngine.Drop(g, 4, 2);
            // Two tier-2s merge: score = 2 * 2 = 4.
            Assert.AreEqual(4, r.Score);
        }
    }

    public class FruitBagTests
    {
        [Test]
        public void Same_seed_produces_same_sequence()
        {
            var a = new FruitBag(seed: 99);
            var b = new FruitBag(seed: 99);
            for (int i = 0; i < 50; i++) Assert.AreEqual(a.Next(), b.Next());
        }

        [Test]
        public void Only_emits_tiers_within_MaxSpawn()
        {
            var bag = new FruitBag(seed: 0, maxSpawnTier: 4);
            for (int i = 0; i < 200; i++)
            {
                var v = bag.Next();
                Assert.GreaterOrEqual(v, 1);
                Assert.LessOrEqual(v, 4);
            }
        }
    }

    public class FruitMergeGameTests
    {
        [Test]
        public void Drop_emits_event_and_score()
        {
            var g = new FruitMergeGame(seed: 1);
            DropResult observed = default;
            g.Dropped += r => observed = r;
            Assert.IsTrue(g.TryDrop(0));
            Assert.IsTrue(observed.Placed);
            // Score is at least 0; not checking exact value because the spawned
            // fruit tier is RNG-dependent and an empty column won't merge anyway.
            Assert.GreaterOrEqual(g.Score, 0);
        }

        [Test]
        public void SwapHold_stashes_then_swaps()
        {
            var g = new FruitMergeGame(seed: 1);
            var first = g.NextFruit;
            g.SwapHold();
            Assert.AreEqual(first, g.HoldFruit, "first SwapHold stashes the current next-fruit");
            var second = g.NextFruit;
            g.SwapHold();
            Assert.AreEqual(first, g.NextFruit, "subsequent SwapHold swaps next and hold");
            Assert.AreEqual(second, g.HoldFruit);
        }
    }
}
