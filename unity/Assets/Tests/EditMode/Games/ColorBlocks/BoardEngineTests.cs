using MiniGames.Games.ColorBlocks.Logic;
using NUnit.Framework;

namespace MiniGames.Tests.Games.ColorBlocks
{
    public class BoardEngineTests
    {
        [Test]
        public void CanPlace_on_empty_board_for_shape_within_bounds()
        {
            var b = new BoardState();
            var shape = new PieceShape("h3", 1, new Cell(0,0), new Cell(1,0), new Cell(2,0));
            Assert.IsTrue(BoardEngine.CanPlace(b, shape, 0, 0));
            Assert.IsTrue(BoardEngine.CanPlace(b, shape, 7, 9));
        }

        [Test]
        public void CannotPlace_out_of_bounds()
        {
            var b = new BoardState();
            var shape = new PieceShape("h3", 1, new Cell(0,0), new Cell(1,0), new Cell(2,0));
            Assert.IsFalse(BoardEngine.CanPlace(b, shape, 8, 0)); // would put cell at x=10
        }

        [Test]
        public void CannotPlace_on_occupied_cell()
        {
            var b = new BoardState();
            b.Set(2, 2, 9);
            var shape = new PieceShape("sq2", 1, new Cell(0,0), new Cell(1,0), new Cell(0,1), new Cell(1,1));
            Assert.IsFalse(BoardEngine.CanPlace(b, shape, 1, 1)); // covers (2,2)
            Assert.IsTrue(BoardEngine.CanPlace(b, shape, 3, 3));
        }

        [Test]
        public void Place_marks_cells_and_returns_correct_cell_count()
        {
            var b = new BoardState();
            var shape = new PieceShape("v3", 5, new Cell(0,0), new Cell(0,1), new Cell(0,2));
            Assert.IsTrue(BoardEngine.TryPlace(b, shape, 4, 5, out var r));
            Assert.AreEqual(3, r.CellsPlaced);
            Assert.AreEqual(5, b.Get(4, 5));
            Assert.AreEqual(5, b.Get(4, 6));
            Assert.AreEqual(5, b.Get(4, 7));
            Assert.AreEqual(0, r.TotalLinesCleared);
        }

        [Test]
        public void Filling_a_row_clears_it()
        {
            var b = new BoardState();
            // Fill row 3 except the last cell.
            for (int x = 0; x < 9; x++) b.Set(x, 3, 7);
            var shape = new PieceShape("dot", 4, new Cell(0,0));
            Assert.IsTrue(BoardEngine.TryPlace(b, shape, 9, 3, out var r));
            Assert.Contains(3, r.ClearedRows);
            Assert.AreEqual(1, r.TotalLinesCleared);
            for (int x = 0; x < 10; x++) Assert.AreEqual(0, b.Get(x, 3));
        }

        [Test]
        public void Filling_row_and_column_at_intersection_clears_both()
        {
            var b = new BoardState();
            // Row 5 missing (5,5); column 5 missing (5,5). Placing a dot at (5,5)
            // completes both at once.
            for (int x = 0; x < 10; x++) if (x != 5) b.Set(x, 5, 1);
            for (int y = 0; y < 10; y++) if (y != 5) b.Set(5, y, 1);
            var dot = new PieceShape("dot", 2, new Cell(0,0));
            Assert.IsTrue(BoardEngine.TryPlace(b, dot, 5, 5, out var r));
            Assert.AreEqual(2, r.TotalLinesCleared);
            Assert.Contains(5, r.ClearedRows);
            Assert.Contains(5, r.ClearedCols);
        }

        [Test]
        public void HasAnyValidPlacement_true_when_empty()
        {
            var b = new BoardState();
            var shape = new PieceShape("sq3", 1,
                new Cell(0,0), new Cell(1,0), new Cell(2,0),
                new Cell(0,1), new Cell(1,1), new Cell(2,1),
                new Cell(0,2), new Cell(1,2), new Cell(2,2));
            Assert.IsTrue(BoardEngine.HasAnyValidPlacement(b, shape));
        }

        [Test]
        public void HasAnyValidPlacement_false_when_board_completely_full()
        {
            var b = new BoardState();
            for (int x = 0; x < 10; x++)
                for (int y = 0; y < 10; y++)
                    b.Set(x, y, 1);
            var dot = new PieceShape("dot", 1, new Cell(0,0));
            Assert.IsFalse(BoardEngine.HasAnyValidPlacement(b, dot));
        }
    }
}
