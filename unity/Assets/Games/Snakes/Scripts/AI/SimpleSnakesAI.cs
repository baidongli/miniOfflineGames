using MiniGames.Games.Snakes.Logic;

namespace MiniGames.Games.Snakes.AI
{
    public interface ISnakesAI
    {
        Direction Choose(SnakesGameState state, int playerIndex);
    }

    /// <summary>
    /// One-step safe-move policy: keep heading if next cell is safe; else
    /// rotate left or right - whichever survives a wall / self / other-snake
    /// check. Lightly biases toward food on the chosen axis.
    /// </summary>
    public sealed class SimpleSnakesAI : ISnakesAI
    {
        public Direction Choose(SnakesGameState s, int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= s.Snakes.Count) return Direction.Up;
            var snake = s.Snakes[playerIndex];
            if (!snake.IsAlive) return snake.Heading;

            var current = snake.Heading;
            var left = RotateLeft(current);
            var right = RotateRight(current);

            // Candidate order: current, side that's toward nearest food, then the other side.
            var candidates = OrderByFoodPreference(s, snake.Head, current, left, right);
            foreach (var d in candidates)
                if (IsSafe(s, playerIndex, d.Step(snake.Head))) return d;

            // No safe move - keep heading and crash.
            return current;
        }

        private static Direction RotateLeft(Direction d) => d switch
        {
            Direction.Up => Direction.Left,
            Direction.Left => Direction.Down,
            Direction.Down => Direction.Right,
            Direction.Right => Direction.Up,
            _ => d
        };

        private static Direction RotateRight(Direction d) => d switch
        {
            Direction.Up => Direction.Right,
            Direction.Right => Direction.Down,
            Direction.Down => Direction.Left,
            Direction.Left => Direction.Up,
            _ => d
        };

        private static bool IsSafe(SnakesGameState s, int playerIndex, GridPos next)
        {
            if (!s.InBounds(next)) return false;
            for (int i = 0; i < s.Snakes.Count; i++)
            {
                var snake = s.Snakes[i];
                if (!snake.IsAlive) continue;
                if (i == playerIndex)
                {
                    if (snake.ContainsExcludingTail(next)) return false;
                }
                else
                {
                    if (snake.ContainsAnywhere(next)) return false;
                }
            }
            return true;
        }

        private static Direction[] OrderByFoodPreference(SnakesGameState s, GridPos head,
            Direction current, Direction left, Direction right)
        {
            GridPos? nearest = null;
            int bestDist = int.MaxValue;
            foreach (var f in s.Food)
            {
                int d = System.Math.Abs(f.X - head.X) + System.Math.Abs(f.Y - head.Y);
                if (d < bestDist) { bestDist = d; nearest = f; }
            }
            if (nearest == null) return new[] { current, left, right };

            // Pick the side that points toward the food when current doesn't.
            var food = nearest.Value;
            bool foodIsToTheLeft = WouldStepCloser(left, head, food) > WouldStepCloser(right, head, food);
            return foodIsToTheLeft
                ? new[] { current, left, right }
                : new[] { current, right, left };
        }

        private static int WouldStepCloser(Direction d, GridPos head, GridPos food)
        {
            var n = d.Step(head);
            int before = System.Math.Abs(food.X - head.X) + System.Math.Abs(food.Y - head.Y);
            int after = System.Math.Abs(food.X - n.X) + System.Math.Abs(food.Y - n.Y);
            return before - after; // positive if step is closer
        }
    }
}
