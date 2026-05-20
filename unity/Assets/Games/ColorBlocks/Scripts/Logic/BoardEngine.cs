using System.Collections.Generic;

namespace MiniGames.Games.ColorBlocks.Logic
{
    /// <summary>
    /// Pure placement + line-clear logic. No Unity dependency.
    /// </summary>
    public static class BoardEngine
    {
        public static bool CanPlace(BoardState board, PieceShape shape, int originX, int originY)
        {
            for (int i = 0; i < shape.Cells.Count; i++)
            {
                var c = shape.Cells[i];
                int x = originX + c.X;
                int y = originY + c.Y;
                if (!board.InBounds(x, y)) return false;
                if (!board.IsEmpty(x, y)) return false;
            }
            return true;
        }

        /// <summary>
        /// Attempts to place a shape and immediately clear any full lines.
        /// Returns false (and leaves board untouched) when placement is illegal.
        /// </summary>
        public static bool TryPlace(BoardState board, PieceShape shape, int originX, int originY,
            out PlaceResult result)
        {
            if (!CanPlace(board, shape, originX, originY))
            {
                result = default;
                return false;
            }

            // Apply placement.
            for (int i = 0; i < shape.Cells.Count; i++)
            {
                var c = shape.Cells[i];
                board.Set(originX + c.X, originY + c.Y, shape.ColorId);
            }

            // Detect lines (rows/cols) to clear. Detect BEFORE clearing so
            // intersecting clears don't false-negative each other.
            var rows = new List<int>();
            var cols = new List<int>();
            for (int y = 0; y < BoardState.Size; y++)
                if (board.IsRowFull(y)) rows.Add(y);
            for (int x = 0; x < BoardState.Size; x++)
                if (board.IsColFull(x)) cols.Add(x);

            foreach (var y in rows) board.ClearRow(y);
            foreach (var x in cols) board.ClearCol(x);

            result = new PlaceResult
            {
                CellsPlaced = shape.CellCount,
                ClearedRows = rows,
                ClearedCols = cols
            };
            return true;
        }

        /// <summary>
        /// True iff any cell on the board admits the shape. Used to detect
        /// game over: if none of the pieces in hand fit, the player has lost.
        /// </summary>
        public static bool HasAnyValidPlacement(BoardState board, PieceShape shape)
        {
            for (int y = 0; y + shape.Height <= BoardState.Size; y++)
                for (int x = 0; x + shape.Width <= BoardState.Size; x++)
                    if (CanPlace(board, shape, x, y)) return true;
            return false;
        }
    }

    public struct PlaceResult
    {
        public int CellsPlaced;
        public List<int> ClearedRows;
        public List<int> ClearedCols;
        public int TotalLinesCleared => ClearedRows.Count + ClearedCols.Count;
    }
}
