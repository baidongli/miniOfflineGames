using System;

namespace MiniGames.Games.FruitMerge.Logic
{
    /// <summary>
    /// Deterministic source of "next fruit" values. Tiers 1..MaxSpawn appear
    /// with declining probability; the highest tier is only created by
    /// merging. Same seed -&gt; same sequence (multiplayer parity).
    /// </summary>
    public sealed class FruitBag
    {
        private readonly Random _rng;
        public int MaxSpawnTier { get; }

        public FruitBag(int seed, int maxSpawnTier = 4)
        {
            _rng = new Random(seed);
            MaxSpawnTier = maxSpawnTier;
        }

        public byte Next()
        {
            // Weight schedule: tier 1 weight 5, tier 2 weight 3, tier 3 weight 2,
            // tier 4+ weight 1 each. Keeps the lowest tiers common.
            int totalWeight = 0;
            for (int t = 1; t <= MaxSpawnTier; t++) totalWeight += WeightFor(t);
            int r = _rng.Next(totalWeight);
            for (int t = 1; t <= MaxSpawnTier; t++)
            {
                int w = WeightFor(t);
                if (r < w) return (byte)t;
                r -= w;
            }
            return 1;
        }

        private static int WeightFor(int tier) => tier switch
        {
            1 => 5,
            2 => 3,
            3 => 2,
            _ => 1
        };
    }
}
