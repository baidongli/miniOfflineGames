namespace MiniGames.Games.FruitMerge.Logic
{
    /// <summary>
    /// Fruit Merge plays on a tall narrow grid. Each cell stores a tier
    /// (1..MaxTier), 0 = empty. Players drop a fruit into a column; it
    /// falls to the lowest empty cell. Same-tier adjacent fruits merge to
    /// the next tier; merges chain.
    /// </summary>
    public sealed class FruitGrid
    {
        public const int DefaultWidth = 7;
        public const int DefaultHeight = 12;
        public const int MaxTier = 11;

        public readonly int Width;
        public readonly int Height;
        private readonly byte[] _cells;

        public FruitGrid(int width = DefaultWidth, int height = DefaultHeight)
        {
            Width = width;
            Height = height;
            _cells = new byte[width * height];
        }

        public byte Get(int x, int y) => _cells[y * Width + x];
        public void Set(int x, int y, byte tier) { _cells[y * Width + x] = tier; }
        public bool IsEmpty(int x, int y) => _cells[y * Width + x] == 0;

        /// <summary>y-coordinate of the lowest empty cell in column x, or Height if full.</summary>
        public int LowestEmptyY(int x)
        {
            for (int y = 0; y < Height; y++)
                if (_cells[y * Width + x] == 0) return y;
            return Height;
        }

        public int CountFruits()
        {
            int n = 0;
            for (int i = 0; i < _cells.Length; i++) if (_cells[i] != 0) n++;
            return n;
        }

        /// <summary>Settle cells in each column under gravity (no merging here).</summary>
        public void ApplyGravity()
        {
            for (int x = 0; x < Width; x++)
            {
                int write = 0;
                for (int y = 0; y < Height; y++)
                {
                    var v = _cells[y * Width + x];
                    if (v == 0) continue;
                    if (write != y)
                    {
                        _cells[write * Width + x] = v;
                        _cells[y * Width + x] = 0;
                    }
                    write++;
                }
            }
        }
    }
}
