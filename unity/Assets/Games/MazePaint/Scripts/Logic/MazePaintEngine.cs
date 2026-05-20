using System.Collections.Generic;

namespace MiniGames.Games.MazePaint.Logic
{
    public struct MazeStepResult
    {
        public List<int> DiedThisTick;
        public List<int> CapturedThisTick;   // players that closed a loop this tick
        public bool MatchOver;
        public int? WinnerIndex;
    }

    /// <summary>
    /// One simulation tick for Maze Paint. Order:
    ///  1. Commit headings.
    ///  2. Compute new heads.
    ///  3. Resolve deaths (out-of-bounds, head-on-trail). All deaths decided
    ///     against the pre-resolution alive/trail state so simultaneous
    ///     events resolve symmetrically.
    ///  4. For survivors, update head + trail. Closing a loop runs the
    ///     flood-fill capture algorithm.
    /// </summary>
    public static class MazePaintEngine
    {
        public static MazeStepResult Step(MazePaintGameState s)
        {
            s.Tick++;
            var died = new List<int>();
            var captured = new List<int>();

            // 1. Commit headings.
            for (int i = 0; i < s.Players.Count; i++)
            {
                var p = s.Players[i];
                if (!p.IsAlive) continue;
                if (p.PendingHeading != p.Heading.Opposite()) p.Heading = p.PendingHeading;
            }

            // 2. Compute new heads.
            var newHeads = new MazePos[s.Players.Count];
            for (int i = 0; i < s.Players.Count; i++)
            {
                var p = s.Players[i];
                if (!p.IsAlive) continue;
                newHeads[i] = p.Head.Step(p.Heading);
            }

            // 3. Death resolution (pre-move).
            //    - Out of bounds: die.
            //    - On own active trail (incl. its own previous cells): die.
            //    - On another player's active trail cell: THAT player dies
            //      (their trail vanishes), the head-mover survives.
            var toKill = new HashSet<int>();
            for (int i = 0; i < s.Players.Count; i++)
            {
                var p = s.Players[i];
                if (!p.IsAlive) continue;
                var head = newHeads[i];
                if (!s.Board.InBounds(head)) { toKill.Add(i); continue; }

                int trailOwner = s.Board.TrailAt(head);
                if (trailOwner == i)
                {
                    // Stepped on own trail -> suicide.
                    toKill.Add(i);
                }
                else if (trailOwner >= 0)
                {
                    // Crashed into someone's trail -> they die.
                    toKill.Add(trailOwner);
                }
            }
            foreach (var idx in toKill)
            {
                var p = s.Players[idx];
                if (!p.IsAlive) continue;
                p.IsAlive = false;
                ClearTrail(s.Board, p);
                died.Add(idx);
            }

            // 4. Surviving players: move + update trail or capture.
            for (int i = 0; i < s.Players.Count; i++)
            {
                var p = s.Players[i];
                if (!p.IsAlive) continue;
                var head = newHeads[i];
                p.Head = head;

                int owner = s.Board.OwnerAt(head);
                if (owner == i)
                {
                    // Back on home soil. If we have a trail, capture.
                    if (p.ActiveTrail.Count > 0)
                    {
                        CaptureLoop(s.Board, p);
                        captured.Add(i);
                        p.OwnedCells = s.Board.CountOwned(i);
                    }
                }
                else
                {
                    // Off home: extend trail.
                    p.ActiveTrail.Add(head);
                    s.Board.SetTrail(head, i);
                }
            }

            // 5. Match-over: zero or one alive in MP.
            int aliveCount = 0, winner = -1;
            for (int i = 0; i < s.Players.Count; i++)
                if (s.Players[i].IsAlive) { aliveCount++; winner = i; }

            bool over = s.Players.Count == 1 ? aliveCount == 0 : aliveCount <= 1;

            return new MazeStepResult
            {
                DiedThisTick = died,
                CapturedThisTick = captured,
                MatchOver = over,
                WinnerIndex = over && aliveCount == 1 ? winner : (int?)null
            };
        }

        private static void ClearTrail(MazeBoard board, MazePlayer p)
        {
            foreach (var c in p.ActiveTrail) board.ClearTrail(c);
            p.ActiveTrail.Clear();
        }

        /// <summary>
        /// Convert trail cells to owned, then flood-fill from board edges
        /// treating (player territory + trail) as walls. Any cell not
        /// reachable from the edges becomes owned by this player.
        /// </summary>
        private static void CaptureLoop(MazeBoard board, MazePlayer p)
        {
            int size = board.Size;
            int n = size * size;

            // Paint trail as owned.
            foreach (var c in p.ActiveTrail) board.SetOwner(c, p.Index);

            // Build a wall mask = own territory.
            var wall = new bool[n];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (board.OwnerAt(x, y) == p.Index) wall[y * size + x] = true;

            // Flood from all boundary cells.
            var reachable = new bool[n];
            var queue = new Queue<int>();
            for (int i = 0; i < size; i++)
            {
                TryEnqueue(0, i);
                TryEnqueue(size - 1, i);
                TryEnqueue(i, 0);
                TryEnqueue(i, size - 1);
            }

            void TryEnqueue(int x, int y)
            {
                int k = y * size + x;
                if (!wall[k] && !reachable[k])
                {
                    reachable[k] = true;
                    queue.Enqueue(k);
                }
            }

            while (queue.Count > 0)
            {
                int k = queue.Dequeue();
                int x = k % size, y = k / size;
                if (x > 0)        TryEnqueue(x - 1, y);
                if (x < size - 1) TryEnqueue(x + 1, y);
                if (y > 0)        TryEnqueue(x, y - 1);
                if (y < size - 1) TryEnqueue(x, y + 1);
            }

            // Any non-wall, non-reachable cell is enclosed -> capture it.
            for (int k = 0; k < n; k++)
                if (!wall[k] && !reachable[k])
                {
                    int x = k % size, y = k / size;
                    board.SetOwner(x, y, p.Index);
                }

            // Trail no longer "in progress".
            foreach (var c in p.ActiveTrail) board.ClearTrail(c);
            p.ActiveTrail.Clear();
        }
    }
}
