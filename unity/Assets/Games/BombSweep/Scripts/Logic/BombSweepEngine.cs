using System.Collections.Generic;

namespace MiniGames.Games.BombSweep.Logic
{
    public struct BombSweepStepResult
    {
        public List<int> DiedThisTick;
        public List<BombPos> ExplodedThisTick;
        public bool MatchOver;
        public int? WinnerIndex;
    }

    /// <summary>
    /// One simulation tick (target ~8 Hz). Order matters:
    ///   1. Apply pending bomb-placement requests.
    ///   2. Decrement bomb fuses; collect bombs to explode.
    ///   3. Expand explosions (chain reactions: a bomb in an explosion's
    ///      path detonates immediately).
    ///   4. Destroy soft blocks and drop powerups.
    ///   5. Fade old explosions.
    ///   6. Move players one cell if their per-cell tick counter ticked.
    ///   7. Resolve deaths: any alive player standing on a lit cell dies.
    ///   8. Pick up powerups.
    /// </summary>
    public static class BombSweepEngine
    {
        public static BombSweepStepResult Step(BombSweepGameState s)
        {
            s.Tick++;
            var died = new List<int>();
            var exploded = new List<BombPos>();

            // 1. Place bombs.
            foreach (var p in s.Players)
            {
                if (!p.IsAlive) continue;
                if (!p.BombRequested) continue;
                p.BombRequested = false;
                if (p.CurrentBombs >= p.MaxBombs) continue;
                if (s.BombAt(p.Pos) != null) continue;       // already a bomb here
                s.Bombs.Add(new Bomb
                {
                    Pos = p.Pos,
                    OwnerIndex = p.Index,
                    Range = p.Range,
                    TicksUntilExplode = BombSweepGameState.BombFuseTicks
                });
                p.CurrentBombs++;
            }

            // 2. Decrement fuses.
            var detonateQueue = new Queue<Bomb>();
            foreach (var b in s.Bombs)
            {
                b.TicksUntilExplode--;
                if (b.TicksUntilExplode <= 0) detonateQueue.Enqueue(b);
            }

            // 3-4. Explode, with chain reactions.
            while (detonateQueue.Count > 0)
            {
                var bomb = detonateQueue.Dequeue();
                if (!s.Bombs.Contains(bomb)) continue;   // already removed by a chained explosion
                s.Bombs.Remove(bomb);
                s.Players[bomb.OwnerIndex].CurrentBombs--;
                exploded.Add(bomb.Pos);

                var cells = new List<BombPos>();
                cells.Add(bomb.Pos);
                ExpandRay(s, bomb.Pos, BombDir.Up,    bomb.Range, cells, detonateQueue);
                ExpandRay(s, bomb.Pos, BombDir.Down,  bomb.Range, cells, detonateQueue);
                ExpandRay(s, bomb.Pos, BombDir.Left,  bomb.Range, cells, detonateQueue);
                ExpandRay(s, bomb.Pos, BombDir.Right, bomb.Range, cells, detonateQueue);

                s.Explosions.Add(new Explosion
                {
                    Cells = cells,
                    TicksUntilFade = BombSweepGameState.ExplosionFadeTicks
                });
            }

            // 5. Fade old explosions.
            for (int i = s.Explosions.Count - 1; i >= 0; i--)
            {
                s.Explosions[i].TicksUntilFade--;
                if (s.Explosions[i].TicksUntilFade <= 0) s.Explosions.RemoveAt(i);
            }

            // 6. Move players.
            foreach (var p in s.Players)
            {
                if (!p.IsAlive) continue;
                p.Heading = p.PendingHeading;
                if (p.Heading == BombDir.None) { p.MoveAccumulator = 0; continue; }
                p.MoveAccumulator++;
                if (p.MoveAccumulator < p.SpeedTicksPerCell) continue;
                p.MoveAccumulator = 0;

                var next = p.Pos.Step(p.Heading);
                if (!s.Board.InBounds(next)) continue;
                if (!s.Board.IsWalkable(next)) continue;
                if (s.BombAt(next) != null && !next.Equals(p.Pos)) continue;
                p.Pos = next;
            }

            // 7. Kill anyone on a lit cell.
            foreach (var p in s.Players)
            {
                if (!p.IsAlive) continue;
                if (IsLit(s, p.Pos))
                {
                    p.IsAlive = false;
                    died.Add(p.Index);
                }
            }

            // 8. Pickups.
            foreach (var p in s.Players)
            {
                if (!p.IsAlive) continue;
                var c = s.Board.Get(p.Pos);
                switch (c)
                {
                    case CellType.PowerBombs:
                        p.MaxBombs++;
                        s.Board.Set(p.Pos, CellType.Empty);
                        break;
                    case CellType.PowerRange:
                        p.Range++;
                        s.Board.Set(p.Pos, CellType.Empty);
                        break;
                    case CellType.PowerSpeed:
                        p.SpeedTicksPerCell = System.Math.Max(1, p.SpeedTicksPerCell - 1);
                        s.Board.Set(p.Pos, CellType.Empty);
                        break;
                }
            }

            // 9. Match-over check.
            int aliveCount = 0, winner = -1;
            for (int i = 0; i < s.Players.Count; i++)
                if (s.Players[i].IsAlive) { aliveCount++; winner = i; }
            bool over = s.Players.Count == 1 ? aliveCount == 0 : aliveCount <= 1;

            return new BombSweepStepResult
            {
                DiedThisTick = died,
                ExplodedThisTick = exploded,
                MatchOver = over,
                WinnerIndex = over && aliveCount == 1 ? winner : (int?)null
            };
        }

        private static void ExpandRay(BombSweepGameState s, BombPos origin, BombDir dir,
            int range, List<BombPos> cells, Queue<Bomb> detonate)
        {
            var cur = origin;
            for (int i = 1; i <= range; i++)
            {
                cur = cur.Step(dir);
                if (!s.Board.InBounds(cur)) return;
                var c = s.Board.Get(cur);
                if (c == CellType.HardWall) return;

                // Chain: a bomb in the path detonates this tick too.
                var b = s.BombAt(cur);
                if (b != null && b.TicksUntilExplode > 0)
                {
                    b.TicksUntilExplode = 0;
                    detonate.Enqueue(b);
                }

                cells.Add(cur);

                if (c == CellType.SoftBlock)
                {
                    // Destroy and maybe drop a powerup.
                    if (s.Rng.Next(100) < BombSweepGameState.PowerupDropChance)
                    {
                        var roll = s.Rng.Next(3);
                        var drop = roll == 0 ? CellType.PowerBombs
                                 : roll == 1 ? CellType.PowerRange
                                             : CellType.PowerSpeed;
                        s.Board.Set(cur, drop);
                    }
                    else
                    {
                        s.Board.Set(cur, CellType.Empty);
                    }
                    return;  // explosion stops at the destroyed block
                }
            }
        }

        private static bool IsLit(BombSweepGameState s, BombPos pos)
        {
            foreach (var e in s.Explosions)
                foreach (var c in e.Cells)
                    if (c.Equals(pos)) return true;
            return false;
        }
    }
}
