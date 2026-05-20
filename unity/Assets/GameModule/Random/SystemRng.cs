using SR = System.Random;

namespace MiniGames.GameModule.Random
{
    public sealed class SystemRng : IRng
    {
        private readonly SR _rng;
        public SystemRng(int seed) { _rng = new SR(seed); }
        public int NextInt() => _rng.Next();
        public int NextInt(int maxExclusive) => _rng.Next(maxExclusive);
        public int NextInt(int minInclusive, int maxExclusive) => _rng.Next(minInclusive, maxExclusive);
        public double NextDouble() => _rng.NextDouble();
    }

    /// <summary>
    /// Returns a predetermined sequence of ints (cycles if exhausted).
    /// For tests that need to control specific outcomes (e.g. force a
    /// specific food spawn, or a specific piece shape).
    /// </summary>
    public sealed class FixedSequenceRng : IRng
    {
        private readonly int[] _values;
        private int _index;

        public FixedSequenceRng(params int[] values)
        {
            _values = values ?? new int[] { 0 };
            if (_values.Length == 0) _values = new int[] { 0 };
        }

        private int Take()
        {
            int v = _values[_index];
            _index = (_index + 1) % _values.Length;
            return v;
        }

        public int NextInt() => Take();

        public int NextInt(int maxExclusive)
        {
            int v = Take();
            // Map negatives into range; never throw.
            int m = ((v % maxExclusive) + maxExclusive) % maxExclusive;
            return m;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            int range = maxExclusive - minInclusive;
            return minInclusive + NextInt(range);
        }

        public double NextDouble()
        {
            int v = Take();
            return ((v & int.MaxValue) / (double)int.MaxValue);
        }
    }
}
