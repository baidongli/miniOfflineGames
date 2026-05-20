using System.Collections.Generic;

namespace MiniGames.Games.FruitMerge.Logic
{
    public struct DropResult
    {
        public bool Placed;
        public int PlacedX;
        public int PlacedY;
        public int Score;                // total awarded for this drop incl. chains
        public int MergesPerformed;      // count of merge events
        public int HighestTierReached;
        public bool GameOver;            // column was full
    }

    /// <summary>
    /// Place a fruit at the top of a column, then run merge-and-gravity to
    /// fixed point. Two or more orthogonally-adjacent cells of equal tier
    /// collapse into a single cell of (tier+1) at the center, scoring
    /// proportionally. Chains scored cumulatively.
    /// </summary>
    public static class FruitMergeEngine
    {
        public static DropResult Drop(FruitGrid grid, int column, byte tier)
        {
            var result = new DropResult { HighestTierReached = tier };

            if (column < 0 || column >= grid.Width)
            {
                result.GameOver = true;
                return result;
            }
            int y = grid.LowestEmptyY(column);
            if (y >= grid.Height)
            {
                result.GameOver = true;
                return result;
            }

            grid.Set(column, y, tier);
            result.Placed = true;
            result.PlacedX = column;
            result.PlacedY = y;

            // Chain merges until no more.
            while (true)
            {
                int gained = ResolveOneMergeRound(grid, out int highestThisRound);
                if (gained <= 0) break;
                result.Score += gained;
                result.MergesPerformed++;
                if (highestThisRound > result.HighestTierReached)
                    result.HighestTierReached = highestThisRound;
            }
            return result;
        }

        // One pass: find every connected component of equal tier (orthogonal),
        // collapse components of size >= 2 into a single (tier+1) at the
        // component's lowest cell, drop everything else with gravity.
        private static int ResolveOneMergeRound(FruitGrid grid, out int highestTier)
        {
            highestTier = 0;
            int w = grid.Width, h = grid.Height;
            var visited = new bool[w * h];
            int totalScore = 0;
            bool anyMerged = false;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int k = y * w + x;
                    if (visited[k]) continue;
                    byte tier = grid.Get(x, y);
                    if (tier == 0 || tier >= FruitGrid.MaxTier) { visited[k] = true; continue; }

                    var component = new List<(int x, int y)>();
                    FloodComponent(grid, x, y, tier, visited, component);
                    if (component.Count < 2) continue;

                    // Merge: clear all, set lowest cell to tier+1.
                    int lowestIdx = 0;
                    for (int i = 1; i < component.Count; i++)
                        if (component[i].y < component[lowestIdx].y) lowestIdx = i;
                    var anchor = component[lowestIdx];
                    foreach (var c in component) grid.Set(c.x, c.y, 0);
                    byte nextTier = (byte)(tier + 1);
                    grid.Set(anchor.x, anchor.y, nextTier);
                    totalScore += tier * component.Count;
                    if (nextTier > highestTier) highestTier = nextTier;
                    anyMerged = true;
                }
            }

            if (anyMerged) grid.ApplyGravity();
            return totalScore;
        }

        private static void FloodComponent(FruitGrid grid, int sx, int sy, byte tier,
            bool[] visited, List<(int, int)> outCells)
        {
            int w = grid.Width, h = grid.Height;
            var stack = new Stack<(int x, int y)>();
            stack.Push((sx, sy));
            while (stack.Count > 0)
            {
                var (x, y) = stack.Pop();
                int k = y * w + x;
                if (visited[k]) continue;
                if (grid.Get(x, y) != tier) continue;
                visited[k] = true;
                outCells.Add((x, y));
                if (x > 0)     stack.Push((x - 1, y));
                if (x < w - 1) stack.Push((x + 1, y));
                if (y > 0)     stack.Push((x, y - 1));
                if (y < h - 1) stack.Push((x, y + 1));
            }
        }
    }
}
