using MiniGames.Games.Tetris.Logic;

namespace MiniGames.Games.Tetris.AI
{
    public interface ITetrisAI
    {
        /// <summary>Choose a target (rotation, X) for the current piece.</summary>
        (int rotation, int x) ChooseLanding(TetrisGame game);
    }

    /// <summary>
    /// Classic Dellacherie-style heuristic: try every (rotation, X), drop
    /// the piece, score the resulting board. Higher = better.
    ///   + 5 per cleared line
    ///   - 3 per increase in aggregate height
    ///   - 8 per new "hole" (empty cell with a filled cell anywhere above)
    ///   - 2 per "bumpiness" (sum of |height diff| between adjacent columns)
    /// </summary>
    public sealed class SimpleTetrisAI : ITetrisAI
    {
        public (int rotation, int x) ChooseLanding(TetrisGame game)
        {
            int bestScore = int.MinValue;
            (int rot, int x) best = (game.Rotation, game.X);

            for (int rot = 0; rot < 4; rot++)
            {
                for (int x = -1; x <= TetrisBoard.Width; x++)
                {
                    int y = DropTarget(game, rot, x);
                    if (y < 0) continue;

                    var preview = ClonePreviewAndPlace(game, rot, x, y, out int lines);
                    int score = Evaluate(preview, lines);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = (rot, x);
                    }
                }
            }
            return best;
        }

        private static int DropTarget(TetrisGame g, int rot, int x)
        {
            // Find the lowest Y at which the piece fits with these (rot, x).
            int y = TetrisBoard.TotalHeight - 4;
            if (!Fits(g.Board, g.Current, rot, x, y)) return -1;
            while (y > 0 && Fits(g.Board, g.Current, rot, x, y - 1)) y--;
            return y;
        }

        private static bool Fits(TetrisBoard b, TetrominoType type, int rot, int x, int y)
        {
            var cells = TetrominoShapes.Cells(type, rot);
            foreach (var (cx, cy) in cells)
            {
                int bx = x + cx, by = y + cy;
                if (bx < 0 || bx >= TetrisBoard.Width) return false;
                if (by < 0) return false;
                if (by < TetrisBoard.TotalHeight && b.Get(bx, by) != 0) return false;
            }
            return true;
        }

        private static byte[] ClonePreviewAndPlace(TetrisGame g, int rot, int x, int y, out int lines)
        {
            int w = TetrisBoard.Width;
            int h = TetrisBoard.TotalHeight;
            var copy = new byte[w * h];
            for (int yy = 0; yy < h; yy++)
                for (int xx = 0; xx < w; xx++)
                    copy[yy * w + xx] = g.Board.Get(xx, yy);

            var cells = TetrominoShapes.Cells(g.Current, rot);
            foreach (var (cx, cy) in cells)
                copy[(y + cy) * w + (x + cx)] = (byte)g.Current;

            // Clear filled rows; count them.
            lines = 0;
            for (int yy = 0; yy < h; yy++)
            {
                bool full = true;
                for (int xx = 0; xx < w; xx++) if (copy[yy * w + xx] == 0) { full = false; break; }
                if (full)
                {
                    lines++;
                    // Shift everything above down by 1.
                    for (int sy = yy; sy < h - 1; sy++)
                        for (int xx = 0; xx < w; xx++)
                            copy[sy * w + xx] = copy[(sy + 1) * w + xx];
                    for (int xx = 0; xx < w; xx++) copy[(h - 1) * w + xx] = 0;
                    yy--; // re-check this row after shift
                }
            }
            return copy;
        }

        private static int Evaluate(byte[] board, int linesCleared)
        {
            int w = TetrisBoard.Width;
            int h = TetrisBoard.TotalHeight;
            // Column heights.
            var heights = new int[w];
            for (int x = 0; x < w; x++)
            {
                for (int y = h - 1; y >= 0; y--)
                {
                    if (board[y * w + x] != 0) { heights[x] = y + 1; break; }
                }
            }
            int aggHeight = 0;
            for (int x = 0; x < w; x++) aggHeight += heights[x];

            int holes = 0;
            for (int x = 0; x < w; x++)
            {
                bool seenBlock = false;
                for (int y = h - 1; y >= 0; y--)
                {
                    if (board[y * w + x] != 0) seenBlock = true;
                    else if (seenBlock) holes++;
                }
            }

            int bumpiness = 0;
            for (int x = 0; x < w - 1; x++) bumpiness += System.Math.Abs(heights[x] - heights[x + 1]);

            return 5 * linesCleared - 3 * aggHeight - 8 * holes - 2 * bumpiness;
        }
    }

    /// <summary>Drives a TetrisGame with an ITetrisAI. Call TakeTurn() per "AI tick".</summary>
    public sealed class CpuTetrisController
    {
        public readonly TetrisGame Game;
        public readonly ITetrisAI Ai;

        public CpuTetrisController(TetrisGame game, ITetrisAI ai) { Game = game; Ai = ai; }

        public bool TakeTurn()
        {
            if (Game.IsGameOver) return false;
            var (rot, x) = Ai.ChooseLanding(Game);
            // Rotate.
            int safety = 8;
            while (Game.Rotation != rot && safety-- > 0)
                if (!Game.TryRotate(1)) break;
            // Shift horizontally.
            safety = TetrisBoard.Width * 2;
            while (Game.X < x && Game.TryMoveRight() && safety-- > 0) { }
            while (Game.X > x && Game.TryMoveLeft() && safety-- > 0) { }
            // Slam.
            var r = Game.HardDrop();
            return r.Locked && !r.GameOver;
        }
    }
}
