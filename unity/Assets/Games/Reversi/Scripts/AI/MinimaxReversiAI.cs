using System.Collections.Generic;
using MiniGames.Games.Reversi.Logic;

namespace MiniGames.Games.Reversi.AI
{
    public interface IReversiAI
    {
        /// <summary>Returns the best (x, y) move, or null to pass.</summary>
        (int x, int y)? Choose(ReversiGame game);
    }

    /// <summary>
    /// Minimax with alpha-beta and a classic positional weight matrix:
    /// corners are gold, X-squares (diagonally adjacent to corners) are
    /// terrible, edges are good. Combined with mobility (legal-move count
    /// differential) gives a reasonable mid-strength opponent.
    /// </summary>
    public sealed class MinimaxReversiAI : IReversiAI
    {
        public int Depth { get; }

        public MinimaxReversiAI(int depth = 3) { Depth = depth; }

        // Classic positional weights (mirror symmetric).
        private static readonly int[,] W =
        {
            { 100, -20,  10,   5,   5,  10, -20, 100 },
            { -20, -50,  -2,  -2,  -2,  -2, -50, -20 },
            {  10,  -2,  -1,  -1,  -1,  -1,  -2,  10 },
            {   5,  -2,  -1,  -1,  -1,  -1,  -2,   5 },
            {   5,  -2,  -1,  -1,  -1,  -1,  -2,   5 },
            {  10,  -2,  -1,  -1,  -1,  -1,  -2,  10 },
            { -20, -50,  -2,  -2,  -2,  -2, -50, -20 },
            { 100, -20,  10,   5,   5,  10, -20, 100 },
        };

        public (int x, int y)? Choose(ReversiGame game)
        {
            var moves = game.Board.LegalMoves(game.CurrentPlayer);
            if (moves.Count == 0) return null;
            byte me = game.CurrentPlayer;
            int bestScore = int.MinValue;
            (int x, int y) best = moves[0];

            var snapshot = Snapshot(game.Board);
            foreach (var (mx, my) in moves)
            {
                var after = ApplyMove(snapshot, mx, my, me);
                int score = -Negamax(after, ReversiBoard.Opposite(me), Depth - 1, int.MinValue + 1, -bestScore);
                if (score > bestScore) { bestScore = score; best = (mx, my); }
            }
            return best;
        }

        private static int Negamax(byte[,] board, byte side, int depth, int alpha, int beta)
        {
            if (depth == 0) return Eval(board, side);
            var moves = LegalMoves(board, side);
            if (moves.Count == 0)
            {
                // Pass; if opponent also has no moves, terminal.
                if (LegalMoves(board, ReversiBoard.Opposite(side)).Count == 0)
                    return TerminalScore(board, side);
                return -Negamax(board, ReversiBoard.Opposite(side), depth - 1, -beta, -alpha);
            }

            int best = int.MinValue + 1;
            foreach (var (mx, my) in moves)
            {
                var after = ApplyMove(board, mx, my, side);
                int score = -Negamax(after, ReversiBoard.Opposite(side), depth - 1, -beta, -alpha);
                if (score > best) best = score;
                if (best > alpha) alpha = best;
                if (alpha >= beta) break;
            }
            return best;
        }

        private static int Eval(byte[,] board, byte side)
        {
            int positional = 0;
            int myCount = 0, oppCount = 0;
            byte opp = ReversiBoard.Opposite(side);
            for (int y = 0; y < ReversiBoard.Size; y++)
                for (int x = 0; x < ReversiBoard.Size; x++)
                {
                    var c = board[x, y];
                    if (c == side) { positional += W[x, y]; myCount++; }
                    else if (c == opp) { positional -= W[x, y]; oppCount++; }
                }
            // Mobility differential.
            int myMoves = LegalMoves(board, side).Count;
            int oppMoves = LegalMoves(board, opp).Count;
            int mobility = (myMoves - oppMoves) * 5;
            return positional + mobility;
        }

        private static int TerminalScore(byte[,] board, byte side)
        {
            int my = 0, opp = 0;
            byte oth = ReversiBoard.Opposite(side);
            for (int y = 0; y < ReversiBoard.Size; y++)
                for (int x = 0; x < ReversiBoard.Size; x++)
                {
                    if (board[x, y] == side) my++;
                    else if (board[x, y] == oth) opp++;
                }
            return (my - opp) * 1000;  // overwhelm positional
        }

        // --- standalone board ops mirroring ReversiBoard but on a flat 2D array ---

        private static byte[,] Snapshot(ReversiBoard b)
        {
            var arr = new byte[ReversiBoard.Size, ReversiBoard.Size];
            for (int y = 0; y < ReversiBoard.Size; y++)
                for (int x = 0; x < ReversiBoard.Size; x++) arr[x, y] = b.Get(x, y);
            return arr;
        }

        private static byte[,] ApplyMove(byte[,] board, int x, int y, byte side)
        {
            var copy = (byte[,])board.Clone();
            copy[x, y] = side;
            byte opp = ReversiBoard.Opposite(side);
            foreach (var (dx, dy) in ReversiBoard.Dirs)
            {
                int cx = x + dx, cy = y + dy;
                var run = new List<(int, int)>();
                while (InBounds(cx, cy) && copy[cx, cy] == opp)
                { run.Add((cx, cy)); cx += dx; cy += dy; }
                if (run.Count > 0 && InBounds(cx, cy) && copy[cx, cy] == side)
                    foreach (var (rx, ry) in run) copy[rx, ry] = side;
            }
            return copy;
        }

        private static List<(int x, int y)> LegalMoves(byte[,] board, byte side)
        {
            var list = new List<(int, int)>();
            for (int y = 0; y < ReversiBoard.Size; y++)
                for (int x = 0; x < ReversiBoard.Size; x++)
                    if (board[x, y] == ReversiBoard.Empty && WouldFlip(board, x, y, side))
                        list.Add((x, y));
            return list;
        }

        private static bool WouldFlip(byte[,] board, int x, int y, byte side)
        {
            byte opp = ReversiBoard.Opposite(side);
            foreach (var (dx, dy) in ReversiBoard.Dirs)
            {
                int cx = x + dx, cy = y + dy;
                int run = 0;
                while (InBounds(cx, cy) && board[cx, cy] == opp) { run++; cx += dx; cy += dy; }
                if (run > 0 && InBounds(cx, cy) && board[cx, cy] == side) return true;
            }
            return false;
        }

        private static bool InBounds(int x, int y)
            => x >= 0 && y >= 0 && x < ReversiBoard.Size && y < ReversiBoard.Size;
    }

    public sealed class CpuReversiController
    {
        public readonly ReversiGame Game;
        public readonly IReversiAI Ai;
        public CpuReversiController(ReversiGame g, IReversiAI a) { Game = g; Ai = a; }
        public bool TakeTurn()
        {
            if (Game.IsGameOver) return false;
            var move = Ai.Choose(Game);
            if (move == null) return Game.Pass(out _);
            return Game.TryPlay(move.Value.x, move.Value.y, out _);
        }
    }
}
