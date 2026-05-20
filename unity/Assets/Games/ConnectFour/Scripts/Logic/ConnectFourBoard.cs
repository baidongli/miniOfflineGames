using System;

namespace MiniGames.Games.ConnectFour.Logic
{
    /// <summary>
    /// Connect Four-style grid. Width x Height cells; pieces drop into a
    /// chosen column and land on top of the lowest existing piece.
    /// Configurable WinLength makes this also usable for Gomoku-style
    /// variants (15x15 with WinLength=5).
    /// </summary>
    public sealed class ConnectFourBoard
    {
        public const byte Empty = 0;
        public const byte PlayerA = 1;
        public const byte PlayerB = 2;

        public readonly int Width;
        public readonly int Height;
        public readonly int WinLength;
        private readonly byte[] _cells;

        public ConnectFourBoard(int width = 7, int height = 6, int winLength = 4)
        {
            if (width <= 0 || height <= 0 || winLength <= 1)
                throw new ArgumentOutOfRangeException();
            Width = width;
            Height = height;
            WinLength = winLength;
            _cells = new byte[width * height];
        }

        public byte Get(int x, int y) => _cells[y * Width + x];
        public void Set(int x, int y, byte v) { _cells[y * Width + x] = v; }
        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        /// <summary>Drops a player's disc into the given column. Returns the row it landed at, or -1 if the column is full.</summary>
        public int Drop(int column, byte player)
        {
            if (column < 0 || column >= Width) return -1;
            for (int y = 0; y < Height; y++)
            {
                if (_cells[y * Width + column] == Empty)
                {
                    _cells[y * Width + column] = player;
                    return y;
                }
            }
            return -1;
        }

        public bool IsColumnFull(int column)
        {
            if (column < 0 || column >= Width) return true;
            return _cells[(Height - 1) * Width + column] != Empty;
        }

        public bool IsFull()
        {
            for (int x = 0; x < Width; x++) if (!IsColumnFull(x)) return false;
            return true;
        }

        /// <summary>
        /// After a piece is dropped at (x, y), check if it completed a run of
        /// WinLength. Faster than scanning the whole board because we only
        /// inspect lines through the new piece.
        /// </summary>
        public bool IsWinAt(int x, int y)
        {
            byte p = Get(x, y);
            if (p == Empty) return false;
            // 4 axes: horizontal, vertical, both diagonals.
            return RunLength(x, y, 1, 0, p) >= WinLength
                || RunLength(x, y, 0, 1, p) >= WinLength
                || RunLength(x, y, 1, 1, p) >= WinLength
                || RunLength(x, y, 1, -1, p) >= WinLength;
        }

        private int RunLength(int x, int y, int dx, int dy, byte p)
        {
            // Walk in (dx, dy) and (-dx, -dy), counting same-player cells including (x, y).
            int n = 1;
            int cx = x + dx, cy = y + dy;
            while (InBounds(cx, cy) && Get(cx, cy) == p) { n++; cx += dx; cy += dy; }
            cx = x - dx; cy = y - dy;
            while (InBounds(cx, cy) && Get(cx, cy) == p) { n++; cx -= dx; cy -= dy; }
            return n;
        }
    }
}
