using System.Collections.Generic;

namespace MiniGames.Games.ColorBlocks.Logic
{
    /// <summary>
    /// All shapes the game can spawn. Block-blast / 1010!-style catalog:
    /// singles, lines (h+v), squares, L's, T's, S/Z. Colors cycle so the
    /// player sees variety; shape choice is what matters for difficulty.
    /// </summary>
    public static class PieceCatalog
    {
        public static readonly IReadOnlyList<PieceShape> All;

        static PieceCatalog()
        {
            var list = new List<PieceShape>();

            // Single
            list.Add(new PieceShape("dot", 1, new Cell(0, 0)));

            // Horizontal lines
            list.Add(new PieceShape("h2", 2, new Cell(0,0), new Cell(1,0)));
            list.Add(new PieceShape("h3", 2, new Cell(0,0), new Cell(1,0), new Cell(2,0)));
            list.Add(new PieceShape("h4", 2, new Cell(0,0), new Cell(1,0), new Cell(2,0), new Cell(3,0)));
            list.Add(new PieceShape("h5", 2, new Cell(0,0), new Cell(1,0), new Cell(2,0), new Cell(3,0), new Cell(4,0)));

            // Vertical lines
            list.Add(new PieceShape("v2", 3, new Cell(0,0), new Cell(0,1)));
            list.Add(new PieceShape("v3", 3, new Cell(0,0), new Cell(0,1), new Cell(0,2)));
            list.Add(new PieceShape("v4", 3, new Cell(0,0), new Cell(0,1), new Cell(0,2), new Cell(0,3)));
            list.Add(new PieceShape("v5", 3, new Cell(0,0), new Cell(0,1), new Cell(0,2), new Cell(0,3), new Cell(0,4)));

            // Squares
            list.Add(new PieceShape("sq2", 4, new Cell(0,0), new Cell(1,0), new Cell(0,1), new Cell(1,1)));
            list.Add(new PieceShape("sq3", 4,
                new Cell(0,0), new Cell(1,0), new Cell(2,0),
                new Cell(0,1), new Cell(1,1), new Cell(2,1),
                new Cell(0,2), new Cell(1,2), new Cell(2,2)));

            // L's (3-cell)
            list.Add(new PieceShape("l_tl", 5, new Cell(0,0), new Cell(0,1), new Cell(1,1))); // ┐
            list.Add(new PieceShape("l_tr", 5, new Cell(1,0), new Cell(0,1), new Cell(1,1))); // ┌
            list.Add(new PieceShape("l_bl", 5, new Cell(0,0), new Cell(1,0), new Cell(0,1))); // ┘
            list.Add(new PieceShape("l_br", 5, new Cell(0,0), new Cell(1,0), new Cell(1,1))); // └

            // Big L (3x3 corners)
            list.Add(new PieceShape("bigL_tl", 6,
                new Cell(0,0),
                new Cell(0,1),
                new Cell(0,2), new Cell(1,2), new Cell(2,2)));
            list.Add(new PieceShape("bigL_tr", 6,
                new Cell(2,0),
                new Cell(2,1),
                new Cell(0,2), new Cell(1,2), new Cell(2,2)));
            list.Add(new PieceShape("bigL_bl", 6,
                new Cell(0,0), new Cell(1,0), new Cell(2,0),
                new Cell(0,1),
                new Cell(0,2)));
            list.Add(new PieceShape("bigL_br", 6,
                new Cell(0,0), new Cell(1,0), new Cell(2,0),
                new Cell(2,1),
                new Cell(2,2)));

            // T tetromino
            list.Add(new PieceShape("t_up",    7, new Cell(0,0), new Cell(1,0), new Cell(2,0), new Cell(1,1)));
            list.Add(new PieceShape("t_down",  7, new Cell(1,0), new Cell(0,1), new Cell(1,1), new Cell(2,1)));
            list.Add(new PieceShape("t_left",  7, new Cell(1,0), new Cell(0,1), new Cell(1,1), new Cell(1,2)));
            list.Add(new PieceShape("t_right", 7, new Cell(0,0), new Cell(0,1), new Cell(1,1), new Cell(0,2)));

            All = list;
        }
    }
}
