using System.Collections.Generic;

namespace MiniGames.Games.ColorBlocks.Logic
{
    /// <summary>
    /// A piece is a set of cells relative to its top-left bounding-box corner.
    /// Cells are immutable; width/height are precomputed so placement checks
    /// don't allocate.
    /// </summary>
    public sealed class PieceShape
    {
        public readonly string Id;
        public readonly byte ColorId;
        public readonly IReadOnlyList<Cell> Cells;
        public readonly int Width;
        public readonly int Height;
        public readonly int CellCount;

        public PieceShape(string id, byte colorId, params Cell[] cells)
        {
            Id = id;
            ColorId = colorId;
            Cells = cells;
            CellCount = cells.Length;
            int maxX = 0, maxY = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i].X > maxX) maxX = cells[i].X;
                if (cells[i].Y > maxY) maxY = cells[i].Y;
            }
            Width = maxX + 1;
            Height = maxY + 1;
        }
    }
}
