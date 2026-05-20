using System.Collections.Generic;

namespace MiniGames.Games.Reversi.Logic
{
    /// <summary>
    /// Standard 8x8 Reversi/Othello board. Cells store: 0 empty, 1 black, 2 white.
    /// Black moves first by convention.
    /// </summary>
    public sealed class ReversiBoard
    {
        public const byte Empty = 0;
        public const byte Black = 1;
        public const byte White = 2;
        public const int Size = 8;

        private readonly byte[] _cells = new byte[Size * Size];

        public ReversiBoard()
        {
            // Standard opening: B/W/W/B center.
            Set(3, 3, White);
            Set(4, 4, White);
            Set(3, 4, Black);
            Set(4, 3, Black);
        }

        public byte Get(int x, int y) => _cells[y * Size + x];
        public void Set(int x, int y, byte v) => _cells[y * Size + x] = v;
        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Size && y < Size;

        public int Count(byte player)
        {
            int n = 0;
            for (int i = 0; i < _cells.Length; i++) if (_cells[i] == player) n++;
            return n;
        }

        public bool IsFull()
        {
            for (int i = 0; i < _cells.Length; i++) if (_cells[i] == Empty) return false;
            return true;
        }

        // 8 directions for ray flipping.
        public static readonly (int dx, int dy)[] Dirs =
        {
            (-1,-1), (0,-1), (1,-1),
            (-1, 0),         (1, 0),
            (-1, 1), (0, 1), (1, 1),
        };

        /// <summary>
        /// Returns the list of opponent cells that would flip if `player` placed at (x, y).
        /// Empty list = illegal move.
        /// </summary>
        public List<(int x, int y)> FlipsFor(int x, int y, byte player)
        {
            var flips = new List<(int, int)>();
            if (!InBounds(x, y) || Get(x, y) != Empty) return flips;
            byte opponent = Opposite(player);

            foreach (var (dx, dy) in Dirs)
            {
                int cx = x + dx, cy = y + dy;
                var run = new List<(int, int)>();
                while (InBounds(cx, cy) && Get(cx, cy) == opponent)
                {
                    run.Add((cx, cy));
                    cx += dx; cy += dy;
                }
                if (run.Count > 0 && InBounds(cx, cy) && Get(cx, cy) == player)
                    flips.AddRange(run);
            }
            return flips;
        }

        public bool HasAnyLegalMove(byte player)
        {
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    if (Get(x, y) == Empty && FlipsFor(x, y, player).Count > 0) return true;
            return false;
        }

        public List<(int x, int y)> LegalMoves(byte player)
        {
            var list = new List<(int, int)>();
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    if (Get(x, y) == Empty && FlipsFor(x, y, player).Count > 0) list.Add((x, y));
            return list;
        }

        public static byte Opposite(byte p) => p == Black ? White : (p == White ? Black : Empty);
    }
}
