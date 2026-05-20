using MiniGames.Games.ConnectFour.Logic;

namespace MiniGames.Games.ConnectFour.AI
{
    public interface IConnectFourAI
    {
        int ChooseColumn(ConnectFourGame game);
    }

    /// <summary>
    /// Minimax with shallow alpha-beta. Branching factor is the board
    /// width (7); depth 4-5 is fast and a reasonable opponent.
    ///
    /// Eval: terminal positions get +/- 100000. Non-terminal positions
    /// score by counting open lines of length WinLength-1 through the
    /// candidate cell (one move from a win).
    /// </summary>
    public sealed class MinimaxConnectFourAI : IConnectFourAI
    {
        public int Depth { get; }

        public MinimaxConnectFourAI(int depth = 4) { Depth = depth; }

        public int ChooseColumn(ConnectFourGame game)
        {
            int bestCol = -1;
            int bestScore = int.MinValue;
            // Try center columns first; gives better alpha-beta cuts AND
            // disambiguates ties toward more strategic placement.
            foreach (var col in CenterOrderedColumns(game.Board.Width))
            {
                if (game.Board.IsColumnFull(col)) continue;
                var snap = Snapshot(game.Board);
                ApplyDrop(snap, col, game.CurrentPlayer);
                int score = -Negamax(snap, OtherPlayer(game.CurrentPlayer),
                    game.Board.WinLength, Depth - 1, int.MinValue + 1, -bestScore);
                if (score > bestScore || bestCol < 0)
                {
                    bestScore = score;
                    bestCol = col;
                }
            }
            return bestCol < 0 ? 0 : bestCol;
        }

        // --- internals ---

        private static int Negamax(byte[,] board, byte player, int winLength,
            int depth, int alpha, int beta)
        {
            // Win check on the last move's contribution would need to know
            // where the last drop happened. Cheaper: scan every cell whose
            // value matches `OtherPlayer(player)` (whose turn just ended)
            // and look for a winning run from any of them. Skip when we
            // know structurally that no move has been made yet.
            int w = board.GetLength(0), h = board.GetLength(1);
            byte just = OtherPlayer(player);
            if (HasWin(board, just, winLength)) return -100000 - depth;
            if (IsFull(board)) return 0;
            if (depth == 0) return Heuristic(board, player, winLength);

            int best = int.MinValue + 1;
            foreach (var col in CenterOrderedColumns(w))
            {
                if (!CanDrop(board, col)) continue;
                int row = DropRow(board, col);
                board[col, row] = player;
                int score = -Negamax(board, OtherPlayer(player), winLength, depth - 1, -beta, -alpha);
                board[col, row] = 0;
                if (score > best) best = score;
                if (best > alpha) alpha = best;
                if (alpha >= beta) break;
            }
            return best;
        }

        private static int Heuristic(byte[,] board, byte sideToMove, int winLength)
        {
            // Score from `sideToMove`'s perspective.
            // Count "open" windows of length winLength: a window of N cells
            // where all of sideToMove's cells in it could still complete.
            // Cheap approximation: count cells in the central column owned
            // by sideToMove (controls the center column = big advantage).
            int w = board.GetLength(0), h = board.GetLength(1);
            int cx = w / 2;
            int score = 0;
            for (int y = 0; y < h; y++)
            {
                if (board[cx, y] == sideToMove) score += 3;
                else if (board[cx, y] == OtherPlayer(sideToMove)) score -= 3;
            }
            // Plus rough count of length-(winLength-1) runs.
            score += CountOpenRuns(board, sideToMove, winLength - 1) * 5;
            score -= CountOpenRuns(board, OtherPlayer(sideToMove), winLength - 1) * 5;
            return score;
        }

        private static int CountOpenRuns(byte[,] board, byte player, int length)
        {
            int w = board.GetLength(0), h = board.GetLength(1);
            int n = 0;
            int[,] dirs = { { 1, 0 }, { 0, 1 }, { 1, 1 }, { 1, -1 } };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    for (int d = 0; d < 4; d++)
                    {
                        int dx = dirs[d, 0], dy = dirs[d, 1];
                        int cx = x, cy = y;
                        bool runOk = true;
                        for (int i = 0; i < length; i++)
                        {
                            if (cx < 0 || cy < 0 || cx >= w || cy >= h) { runOk = false; break; }
                            if (board[cx, cy] != player) { runOk = false; break; }
                            cx += dx; cy += dy;
                        }
                        if (runOk) n++;
                    }
                }
            return n;
        }

        private static bool HasWin(byte[,] board, byte player, int winLength)
        {
            int w = board.GetLength(0), h = board.GetLength(1);
            int[,] dirs = { { 1, 0 }, { 0, 1 }, { 1, 1 }, { 1, -1 } };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (board[x, y] != player) continue;
                    for (int d = 0; d < 4; d++)
                    {
                        int dx = dirs[d, 0], dy = dirs[d, 1];
                        int cx = x, cy = y;
                        int k = 0;
                        while (cx >= 0 && cy >= 0 && cx < w && cy < h && board[cx, cy] == player)
                        { k++; if (k >= winLength) return true; cx += dx; cy += dy; }
                    }
                }
            return false;
        }

        private static bool IsFull(byte[,] board)
        {
            int w = board.GetLength(0), h = board.GetLength(1);
            for (int x = 0; x < w; x++)
                if (board[x, h - 1] == 0) return false;
            return true;
        }

        private static bool CanDrop(byte[,] board, int column) => board[column, board.GetLength(1) - 1] == 0;

        private static int DropRow(byte[,] board, int column)
        {
            int h = board.GetLength(1);
            for (int y = 0; y < h; y++) if (board[column, y] == 0) return y;
            return -1;
        }

        private static byte[,] Snapshot(ConnectFourBoard b)
        {
            var arr = new byte[b.Width, b.Height];
            for (int y = 0; y < b.Height; y++)
                for (int x = 0; x < b.Width; x++) arr[x, y] = b.Get(x, y);
            return arr;
        }

        private static void ApplyDrop(byte[,] board, int column, byte player)
        {
            int y = DropRow(board, column);
            if (y >= 0) board[column, y] = player;
        }

        private static byte OtherPlayer(byte p) => p == ConnectFourBoard.PlayerA ? ConnectFourBoard.PlayerB : ConnectFourBoard.PlayerA;

        private static System.Collections.Generic.IEnumerable<int> CenterOrderedColumns(int width)
        {
            // 0 1 2 3 4 5 6  ->  3 2 4 1 5 0 6
            int center = width / 2;
            yield return center;
            for (int offset = 1; offset <= width / 2; offset++)
            {
                if (center - offset >= 0) yield return center - offset;
                if (center + offset < width) yield return center + offset;
            }
        }
    }

    public sealed class CpuConnectFourController
    {
        public readonly ConnectFourGame Game;
        public readonly IConnectFourAI Ai;
        public CpuConnectFourController(ConnectFourGame game, IConnectFourAI ai)
        { Game = game; Ai = ai; }
        public bool TakeTurn()
        {
            if (Game.IsGameOver) return false;
            return Game.TryPlay(Ai.ChooseColumn(Game), out _);
        }
    }
}
