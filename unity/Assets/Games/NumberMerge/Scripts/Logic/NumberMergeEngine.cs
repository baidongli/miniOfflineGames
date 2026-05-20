using System;
using System.Collections.Generic;

namespace MiniGames.Games.NumberMerge.Logic
{
    public struct SwipeResult
    {
        public bool AnyMoved;
        public int ScoreGained;
        public byte MaxExponentReached;
    }

    /// <summary>
    /// 2048-style sliding logic. For each swipe direction, slide tiles
    /// toward that edge, merging adjacent equal-value tiles (each tile
    /// may participate in at most one merge per swipe).
    ///
    /// Implementation trick: rotate the board so that "swipe direction"
    /// becomes "slide left", then run the same 1D collapse on each row.
    /// </summary>
    public static class NumberMergeEngine
    {
        public static SwipeResult Swipe(NumberMergeBoard board, SwipeDir dir)
        {
            int n = NumberMergeBoard.Size;
            // Snapshot grid into a 2D array oriented so we always collapse "left".
            var grid = new byte[n, n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    grid[x, y] = ReadOriented(board, dir, x, y);

            int totalScore = 0;
            for (int y = 0; y < n; y++)
            {
                var row = new byte[n];
                for (int x = 0; x < n; x++) row[x] = grid[x, y];

                int score = CollapseLeft(row);
                totalScore += score;
                for (int x = 0; x < n; x++) grid[x, y] = row[x];
            }

            // Write oriented grid back.
            bool anyMoved = false;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    byte newVal = grid[x, y];
                    byte oldVal = ReadOriented(board, dir, x, y);
                    if (newVal != oldVal)
                    {
                        WriteOriented(board, dir, x, y, newVal);
                        anyMoved = true;
                    }
                }

            return new SwipeResult
            {
                AnyMoved = anyMoved,
                ScoreGained = totalScore,
                MaxExponentReached = board.MaxExponent()
            };
        }

        /// <summary>Collapses a single row to the left in place. Returns the score gained.</summary>
        public static int CollapseLeft(byte[] row)
        {
            int n = row.Length;
            // 1. Compact non-zero values to the left.
            int write = 0;
            for (int i = 0; i < n; i++)
                if (row[i] != 0) { row[write++] = row[i]; if (write - 1 != i) row[i] = 0; }
            for (int i = write; i < n; i++) row[i] = 0;

            // 2. Merge equal neighbors (each only once).
            int score = 0;
            for (int i = 0; i < n - 1; i++)
            {
                if (row[i] != 0 && row[i] == row[i + 1])
                {
                    row[i]++;                          // exponent goes up by one
                    score += 1 << row[i];              // 2^new exponent = the value created
                    row[i + 1] = 0;
                    i++;                                // skip the just-merged cell
                }
            }

            // 3. Compact again after merges.
            write = 0;
            var tmp = new byte[n];
            for (int i = 0; i < n; i++) if (row[i] != 0) tmp[write++] = row[i];
            for (int i = 0; i < n; i++) row[i] = tmp[i];
            return score;
        }

        /// <summary>True if at least one swipe direction would move/merge anything.</summary>
        public static bool HasAnyValidSwipe(NumberMergeBoard board)
        {
            int n = NumberMergeBoard.Size;
            // Empty cell anywhere -> at least one direction collapses something.
            if (!board.IsFull()) return true;
            // Otherwise look for adjacent equal pairs (would merge).
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    byte v = board.Get(x, y);
                    if (x + 1 < n && board.Get(x + 1, y) == v) return true;
                    if (y + 1 < n && board.Get(x, y + 1) == v) return true;
                }
            return false;
        }

        /// <summary>Spawn a tile at a random empty cell. 90% '2', 10% '4'. Returns false if no empty cell.</summary>
        public static bool SpawnTile(NumberMergeBoard board, Random rng)
        {
            int n = NumberMergeBoard.Size;
            var empties = new List<(int x, int y)>();
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    if (board.IsEmpty(x, y)) empties.Add((x, y));
            if (empties.Count == 0) return false;
            var (sx, sy) = empties[rng.Next(empties.Count)];
            byte exp = (byte)(rng.Next(10) == 0 ? 2 : 1);   // 10% chance of '4'
            board.Set(sx, sy, exp);
            return true;
        }

        // --- orientation helpers (treat every direction as "slide left") ---

        private static byte ReadOriented(NumberMergeBoard board, SwipeDir dir, int x, int y)
        {
            int n = NumberMergeBoard.Size;
            switch (dir)
            {
                case SwipeDir.Left:  return board.Get(x, y);
                case SwipeDir.Right: return board.Get(n - 1 - x, y);
                case SwipeDir.Up:    return board.Get(y, n - 1 - x);
                case SwipeDir.Down:  return board.Get(y, x);
                default: return 0;
            }
        }

        private static void WriteOriented(NumberMergeBoard board, SwipeDir dir, int x, int y, byte v)
        {
            int n = NumberMergeBoard.Size;
            switch (dir)
            {
                case SwipeDir.Left:  board.Set(x, y, v); break;
                case SwipeDir.Right: board.Set(n - 1 - x, y, v); break;
                case SwipeDir.Up:    board.Set(y, n - 1 - x, v); break;
                case SwipeDir.Down:  board.Set(y, x, v); break;
            }
        }
    }
}
