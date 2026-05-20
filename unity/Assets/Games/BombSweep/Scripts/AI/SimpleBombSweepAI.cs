using MiniGames.Games.BombSweep.Logic;

namespace MiniGames.Games.BombSweep.AI
{
    public interface IBombSweepAI
    {
        (BombDir heading, bool placeBomb) Choose(BombSweepGameState s, int playerIndex);
    }

    /// <summary>
    /// Heuristic AI that won't win tournaments but plays the game:
    /// 1. If standing on or adjacent to a soon-to-detonate bomb, flee.
    /// 2. If adjacent to a soft block or enemy and bomb available, place one.
    /// 3. Otherwise wander toward the nearest soft block (to gain powerups).
    /// </summary>
    public sealed class SimpleBombSweepAI : IBombSweepAI
    {
        public (BombDir heading, bool placeBomb) Choose(BombSweepGameState s, int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= s.Players.Count) return (BombDir.None, false);
            var me = s.Players[playerIndex];
            if (!me.IsAlive) return (BombDir.None, false);

            // 1. Danger check: am I on a future-blast cell? Flee.
            var dangerCells = ComputeDangerCells(s);
            if (dangerCells.Contains(PackPos(me.Pos)))
            {
                var safe = FindSafeDirection(s, me, dangerCells);
                if (safe != BombDir.None) return (safe, false);
            }

            // 2. Place a bomb if a soft block or enemy is adjacent and we have a bomb available.
            if (me.CurrentBombs < me.MaxBombs && AdjacentTargetWorthBombing(s, me))
            {
                // Place + start moving toward safety same tick.
                var flee = FindSafeDirection(s, me, ProjectBlastFrom(s, me.Pos, me.Range, dangerCells));
                return (flee, true);
            }

            // 3. Wander toward nearest soft block.
            var stepDir = StepTowardNearestSoftBlock(s, me);
            return (stepDir, false);
        }

        private static System.Collections.Generic.HashSet<int> ComputeDangerCells(BombSweepGameState s)
        {
            var set = new System.Collections.Generic.HashSet<int>();
            // Cells already lit by explosions.
            foreach (var e in s.Explosions)
                foreach (var c in e.Cells) set.Add(PackPos(c));
            // Cells reachable by any bomb's blast ray.
            foreach (var b in s.Bombs)
            {
                set.Add(PackPos(b.Pos));
                AddRay(set, s, b.Pos, BombDir.Up,    b.Range);
                AddRay(set, s, b.Pos, BombDir.Down,  b.Range);
                AddRay(set, s, b.Pos, BombDir.Left,  b.Range);
                AddRay(set, s, b.Pos, BombDir.Right, b.Range);
            }
            return set;
        }

        private static System.Collections.Generic.HashSet<int> ProjectBlastFrom(BombSweepGameState s,
            BombPos origin, int range, System.Collections.Generic.HashSet<int> already)
        {
            var set = new System.Collections.Generic.HashSet<int>(already);
            set.Add(PackPos(origin));
            AddRay(set, s, origin, BombDir.Up,    range);
            AddRay(set, s, origin, BombDir.Down,  range);
            AddRay(set, s, origin, BombDir.Left,  range);
            AddRay(set, s, origin, BombDir.Right, range);
            return set;
        }

        private static void AddRay(System.Collections.Generic.HashSet<int> set, BombSweepGameState s,
            BombPos origin, BombDir dir, int range)
        {
            var cur = origin;
            for (int i = 1; i <= range; i++)
            {
                cur = cur.Step(dir);
                if (!s.Board.InBounds(cur)) return;
                var c = s.Board.Get(cur);
                if (c == CellType.HardWall) return;
                set.Add(PackPos(cur));
                if (c == CellType.SoftBlock) return;
            }
        }

        private static BombDir FindSafeDirection(BombSweepGameState s, BombSweepPlayer me,
            System.Collections.Generic.HashSet<int> danger)
        {
            foreach (BombDir d in new[] { BombDir.Up, BombDir.Right, BombDir.Down, BombDir.Left })
            {
                var n = me.Pos.Step(d);
                if (!s.Board.InBounds(n)) continue;
                if (!s.Board.IsWalkable(n)) continue;
                if (s.BombAt(n) != null) continue;
                if (!danger.Contains(PackPos(n))) return d;
            }
            return BombDir.None;
        }

        private static bool AdjacentTargetWorthBombing(BombSweepGameState s, BombSweepPlayer me)
        {
            foreach (BombDir d in new[] { BombDir.Up, BombDir.Right, BombDir.Down, BombDir.Left })
            {
                var n = me.Pos.Step(d);
                if (!s.Board.InBounds(n)) continue;
                if (s.Board.Get(n) == CellType.SoftBlock) return true;
                foreach (var other in s.Players)
                    if (other.Index != me.Index && other.IsAlive && other.Pos.Equals(n))
                        return true;
            }
            return false;
        }

        private static BombDir StepTowardNearestSoftBlock(BombSweepGameState s, BombSweepPlayer me)
        {
            BombPos? best = null;
            int bestDist = int.MaxValue;
            for (int y = 0; y < s.Board.Height; y++)
                for (int x = 0; x < s.Board.Width; x++)
                    if (s.Board.Get(x, y) == CellType.SoftBlock)
                    {
                        int d = System.Math.Abs(x - me.Pos.X) + System.Math.Abs(y - me.Pos.Y);
                        if (d < bestDist) { bestDist = d; best = new BombPos(x, y); }
                    }
            if (best == null) return BombDir.None;
            int dx = best.Value.X - me.Pos.X;
            int dy = best.Value.Y - me.Pos.Y;
            // Prefer the bigger axis; check walkability before committing.
            BombDir primary = System.Math.Abs(dx) > System.Math.Abs(dy)
                ? (dx > 0 ? BombDir.Right : BombDir.Left)
                : (dy > 0 ? BombDir.Up : BombDir.Down);
            var pn = me.Pos.Step(primary);
            if (s.Board.InBounds(pn) && s.Board.IsWalkable(pn) && s.BombAt(pn) == null) return primary;
            // Fall back to other axis.
            BombDir alt = primary == BombDir.Up || primary == BombDir.Down
                ? (dx > 0 ? BombDir.Right : BombDir.Left)
                : (dy > 0 ? BombDir.Up : BombDir.Down);
            var an = me.Pos.Step(alt);
            if (s.Board.InBounds(an) && s.Board.IsWalkable(an) && s.BombAt(an) == null) return alt;
            return BombDir.None;
        }

        private static int PackPos(BombPos p) => (p.X << 16) | (ushort)p.Y;
    }

    public sealed class CpuBombSweepController
    {
        public readonly BombSweepGameState State;
        public readonly int PlayerIndex;
        public readonly IBombSweepAI Ai;

        public CpuBombSweepController(BombSweepGameState s, int playerIndex, IBombSweepAI ai)
        { State = s; PlayerIndex = playerIndex; Ai = ai; }

        public void BeforeTick()
        {
            if (PlayerIndex < 0 || PlayerIndex >= State.Players.Count) return;
            if (!State.Players[PlayerIndex].IsAlive) return;
            var (h, b) = Ai.Choose(State, PlayerIndex);
            State.SetInput(PlayerIndex, h, b);
        }
    }
}
