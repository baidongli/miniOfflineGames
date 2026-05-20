namespace MiniGames.Games.NumberMerge.Logic
{
    public enum SwipeDir : byte
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3
    }

    /// <summary>
    /// 4x4 grid storing tile *exponents*: 0 = empty, 1 = "2", 2 = "4",
    /// 3 = "8", ..., 11 = "2048", 12 = "4096", etc. Storing exponents
    /// keeps everything to a single byte even at very high values.
    /// </summary>
    public sealed class NumberMergeBoard
    {
        public const int Size = 4;
        public const int WinExponent = 11; // 2^11 = 2048

        private readonly byte[] _cells = new byte[Size * Size];

        public byte Get(int x, int y) => _cells[y * Size + x];
        public void Set(int x, int y, byte v) => _cells[y * Size + x] = v;

        public int Value(int x, int y) => Get(x, y) == 0 ? 0 : 1 << Get(x, y);

        public bool IsEmpty(int x, int y) => Get(x, y) == 0;

        public bool IsFull()
        {
            for (int i = 0; i < _cells.Length; i++) if (_cells[i] == 0) return false;
            return true;
        }

        public byte MaxExponent()
        {
            byte m = 0;
            for (int i = 0; i < _cells.Length; i++) if (_cells[i] > m) m = _cells[i];
            return m;
        }

        public int EmptyCount()
        {
            int n = 0;
            for (int i = 0; i < _cells.Length; i++) if (_cells[i] == 0) n++;
            return n;
        }
    }
}
