using System;

namespace MiniGames.Games.Tetris.Logic
{
    /// <summary>
    /// Standard Tetris board: 10 wide x 20 visible tall + 4 cell hidden
    /// "buffer" at the top where pieces spawn. Each cell stores the
    /// TetrominoType that locked there (0 = empty). Y grows up; gravity
    /// pulls a piece down (decreasing Y).
    /// </summary>
    public sealed class TetrisBoard
    {
        public const int Width = 10;
        public const int VisibleHeight = 20;
        public const int BufferHeight = 4;
        public const int TotalHeight = VisibleHeight + BufferHeight; // 24

        private readonly byte[] _cells;

        public TetrisBoard() { _cells = new byte[Width * TotalHeight]; }

        public byte Get(int x, int y)
        {
            if (!InBounds(x, y)) return 0;
            return _cells[y * Width + x];
        }

        public void Set(int x, int y, byte v)
        {
            if (!InBounds(x, y)) return;
            _cells[y * Width + x] = v;
        }

        public bool IsEmpty(int x, int y) => Get(x, y) == 0;

        public bool InBounds(int x, int y) =>
            x >= 0 && y >= 0 && x < Width && y < TotalHeight;

        public bool IsRowFull(int y)
        {
            for (int x = 0; x < Width; x++)
                if (_cells[y * Width + x] == 0) return false;
            return true;
        }

        public bool IsRowEmpty(int y)
        {
            for (int x = 0; x < Width; x++)
                if (_cells[y * Width + x] != 0) return false;
            return true;
        }

        /// <summary>
        /// Remove the given rows (must be sorted ascending). Rows above
        /// each removed row fall down by one. Cells previously at the top
        /// become empty.
        /// </summary>
        public void RemoveRows(System.Collections.Generic.List<int> rowsAsc)
        {
            // Process top-down so indexes stay correct as we collapse.
            for (int i = rowsAsc.Count - 1; i >= 0; i--)
            {
                int row = rowsAsc[i];
                for (int y = row; y < TotalHeight - 1; y++)
                    for (int x = 0; x < Width; x++)
                        _cells[y * Width + x] = _cells[(y + 1) * Width + x];
                // Top row now empty.
                for (int x = 0; x < Width; x++) _cells[(TotalHeight - 1) * Width + x] = 0;
            }
        }

        /// <summary>
        /// Multiplayer attack: push N junk rows in at the bottom; everything
        /// shifts up by N. Junk row is full except for one column gap per
        /// row (chosen by the rng so the wire payload can replay it).
        /// Returns true if any cell was pushed out of the visible+buffer area
        /// (i.e. instant top-out).
        /// </summary>
        public bool PushJunkRows(int count, byte junkColor, System.Random rng)
        {
            if (count <= 0) return false;

            // Overflow check before shifting.
            bool overflow = false;
            for (int y = TotalHeight - count; y < TotalHeight && !overflow; y++)
                for (int x = 0; x < Width; x++)
                    if (_cells[y * Width + x] != 0) { overflow = true; break; }

            // Shift everything up by count.
            for (int y = TotalHeight - 1; y >= count; y--)
                for (int x = 0; x < Width; x++)
                    _cells[y * Width + x] = _cells[(y - count) * Width + x];

            // Fill bottom `count` rows with junk + one gap each.
            for (int row = 0; row < count; row++)
            {
                int gap = rng.Next(Width);
                for (int x = 0; x < Width; x++)
                    _cells[row * Width + x] = (byte)(x == gap ? 0 : junkColor);
            }

            return overflow;
        }
    }
}
