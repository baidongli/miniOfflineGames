using MiniGames.Games.MazePaint.Logic;

namespace MiniGames.Games.MazePaint.AI
{
    public interface IMazePaintAI
    {
        MazeDir Choose(MazePaintGameState state, int playerIndex);
    }

    /// <summary>
    /// One-step safe-move policy. Prefers continuing current heading.
    /// When in trail mode, lightly steers back toward owned territory to
    /// close the loop. Avoids own trail and walls.
    /// </summary>
    public sealed class SimpleMazePaintAI : IMazePaintAI
    {
        public MazeDir Choose(MazePaintGameState s, int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= s.Players.Count) return MazeDir.Up;
            var me = s.Players[playerIndex];
            if (!me.IsAlive) return me.Heading;

            var current = me.Heading;
            var left = RotateLeft(current);
            var right = RotateRight(current);

            // If carrying a trail, try to head home; else prefer current.
            var homePref = me.ActiveTrail.Count > 0
                ? OrderByHomePreference(s, me, current, left, right)
                : new[] { current, left, right };

            foreach (var d in homePref)
                if (IsSafe(s, playerIndex, me.Head.Step(d))) return d;

            return current;
        }

        private static MazeDir RotateLeft(MazeDir d) => d switch
        {
            MazeDir.Up => MazeDir.Left,
            MazeDir.Left => MazeDir.Down,
            MazeDir.Down => MazeDir.Right,
            MazeDir.Right => MazeDir.Up,
            _ => d
        };

        private static MazeDir RotateRight(MazeDir d) => d switch
        {
            MazeDir.Up => MazeDir.Right,
            MazeDir.Right => MazeDir.Down,
            MazeDir.Down => MazeDir.Left,
            MazeDir.Left => MazeDir.Up,
            _ => d
        };

        private static bool IsSafe(MazePaintGameState s, int playerIndex, MazePos next)
        {
            if (!s.Board.InBounds(next)) return false;
            // Stepping on own active trail = suicide.
            if (s.Board.TrailAt(next) == playerIndex) return false;
            return true;
        }

        private static MazeDir[] OrderByHomePreference(MazePaintGameState s, MazePlayer me,
            MazeDir current, MazeDir left, MazeDir right)
        {
            // Find nearest owned cell of mine.
            MazePos target = me.Head;
            int bestDist = int.MaxValue;
            int size = s.Board.Size;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (s.Board.OwnerAt(x, y) == me.Index)
                    {
                        int d = System.Math.Abs(x - me.Head.X) + System.Math.Abs(y - me.Head.Y);
                        if (d < bestDist) { bestDist = d; target = new MazePos(x, y); }
                    }
            int dCurrent = Manhattan(me.Head.Step(current), target);
            int dLeft = Manhattan(me.Head.Step(left), target);
            int dRight = Manhattan(me.Head.Step(right), target);
            // Smaller distance preferred. Stable order: current then left then right ties broken by index.
            return SortAsc(
                (current, dCurrent),
                (left, dLeft),
                (right, dRight));
        }

        private static int Manhattan(MazePos a, MazePos b)
            => System.Math.Abs(a.X - b.X) + System.Math.Abs(a.Y - b.Y);

        private static MazeDir[] SortAsc(params (MazeDir dir, int d)[] xs)
        {
            // Tiny manual sort (3 elements).
            System.Array.Sort(xs, (l, r) => l.d.CompareTo(r.d));
            var result = new MazeDir[xs.Length];
            for (int i = 0; i < xs.Length; i++) result[i] = xs[i].dir;
            return result;
        }
    }
}
