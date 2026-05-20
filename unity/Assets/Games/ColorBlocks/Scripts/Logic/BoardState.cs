using System;

namespace MiniGames.Games.ColorBlocks.Logic
{
    /// <summary>
    /// 10x10 grid of color ids (0 = empty). Mutable; the engine operates in
    /// place. Use Clone() for speculative moves or replay.
    /// </summary>
    public sealed class BoardState
    {
        public const int Size = 10;

        private readonly byte[] _cells;

        public int Width => Size;
        public int Height => Size;

        public BoardState() { _cells = new byte[Size * Size]; }
        private BoardState(byte[] cells) { _cells = cells; }

        public byte Get(int x, int y)
        {
            CheckBounds(x, y);
            return _cells[Index(x, y)];
        }

        public void Set(int x, int y, byte v)
        {
            CheckBounds(x, y);
            _cells[Index(x, y)] = v;
        }

        public bool IsEmpty(int x, int y) => Get(x, y) == 0;

        public bool InBounds(int x, int y)
            => x >= 0 && y >= 0 && x < Size && y < Size;

        public BoardState Clone()
        {
            var copy = new byte[_cells.Length];
            Buffer.BlockCopy(_cells, 0, copy, 0, _cells.Length);
            return new BoardState(copy);
        }

        public bool IsRowFull(int y)
        {
            for (int x = 0; x < Size; x++)
                if (_cells[Index(x, y)] == 0) return false;
            return true;
        }

        public bool IsColFull(int x)
        {
            for (int y = 0; y < Size; y++)
                if (_cells[Index(x, y)] == 0) return false;
            return true;
        }

        public void ClearRow(int y)
        {
            for (int x = 0; x < Size; x++) _cells[Index(x, y)] = 0;
        }

        public void ClearCol(int x)
        {
            for (int y = 0; y < Size; y++) _cells[Index(x, y)] = 0;
        }

        /// <summary>
        /// Used by multiplayer "attack" mechanic: push junk rows in at the
        /// bottom, shifting everything up. Cells that would fall off the top
        /// are discarded. Each junk row has a single gap whose column comes
        /// from the deterministic RNG so the move is reproducible.
        /// Returns true if any existing cell was pushed out of bounds.
        /// </summary>
        public bool PushJunkRows(int count, int junkColor, System.Random rng)
        {
            if (count <= 0) return false;

            // Check overflow BEFORE shifting (those rows are about to be lost).
            bool overflow = false;
            for (int y = Size - count; y < Size && !overflow; y++)
                for (int x = 0; x < Size; x++)
                    if (_cells[Index(x, y)] != 0) { overflow = true; break; }

            // Shift up.
            for (int y = Size - 1; y >= count; y--)
                for (int x = 0; x < Size; x++)
                    _cells[Index(x, y)] = _cells[Index(x, y - count)];

            // Fill the new bottom rows with junk + one gap per row.
            for (int row = 0; row < count; row++)
            {
                int gap = rng.Next(Size);
                for (int x = 0; x < Size; x++)
                    _cells[Index(x, row)] = (byte)(x == gap ? 0 : junkColor);
            }

            return overflow;
        }

        private static int Index(int x, int y) => y * Size + x;

        private static void CheckBounds(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Size || y >= Size)
                throw new ArgumentOutOfRangeException($"({x},{y}) out of {Size}x{Size}");
        }
    }
}
