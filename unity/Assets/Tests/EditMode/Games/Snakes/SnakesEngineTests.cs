using MiniGames.Games.Snakes.Logic;
using NUnit.Framework;

namespace MiniGames.Tests.Games.Snakes
{
    public class SnakesEngineTests
    {
        private static SnakesGameState NewState(int players = 1, int w = 20, int h = 20, int seed = 1)
            => new SnakesGameState(w, h, players, seed);

        [Test]
        public void Snake_moves_one_cell_per_tick_in_heading_direction()
        {
            var s = NewState();
            var snake = s.Snakes[0];
            var startHead = snake.Head;
            var headingBefore = snake.Heading;

            SnakesEngine.Step(s);

            var expected = headingBefore.Step(startHead);
            Assert.AreEqual(expected, snake.Head);
        }

        [Test]
        public void Snake_grows_when_eating_food()
        {
            var s = NewState();
            var snake = s.Snakes[0];
            // Place food directly in front of head.
            var foodPos = snake.Heading.Step(snake.Head);
            s.Food.Clear();
            s.Food.Add(foodPos);

            int lengthBefore = snake.Length;
            var result = SnakesEngine.Step(s);

            Assert.Contains(0, result.AteThisTick);
            Assert.AreEqual(lengthBefore + 1, snake.Length,
                "snake should grow by one cell when it eats");
        }

        [Test]
        public void Snake_dies_when_it_hits_a_wall()
        {
            var s = NewState(w: 6, h: 6);
            var snake = s.Snakes[0];
            s.Food.Clear();
            // Drive into the right wall.
            s.SetInput(0, Direction.Right);
            for (int i = 0; i < 20 && snake.IsAlive; i++) SnakesEngine.Step(s);

            Assert.IsFalse(snake.IsAlive, "snake never died running into wall");
        }

        [Test]
        public void Snake_dies_when_it_runs_into_itself()
        {
            var s = NewState(w: 20, h: 20);
            var snake = s.Snakes[0];
            s.Food.Clear();

            // Inflate to length 6 so we can make a tight square.
            for (int i = 0; i < 4; i++) snake.PendingGrowth++;
            // Walk a tight box: R, R, U, L, D (head will overlap body).
            var seq = new[] { Direction.Right, Direction.Right, Direction.Up, Direction.Left, Direction.Down };
            foreach (var d in seq)
            {
                s.SetInput(0, d);
                SnakesEngine.Step(s);
                if (!snake.IsAlive) break;
            }

            Assert.IsFalse(snake.IsAlive, "snake should have hit its own body");
        }

        [Test]
        public void Two_snakes_can_kill_each_other_in_same_tick()
        {
            var s = NewState(players: 2);
            s.Food.Clear();
            // Re-place snakes head-to-head, two cells apart.
            s.Snakes.Clear();
            s.Snakes.Add(new SnakeState(0, new GridPos(5, 5), Direction.Right, 1));
            s.Snakes.Add(new SnakeState(1, new GridPos(7, 5), Direction.Left, 1));
            s.SetInput(0, Direction.Right);
            s.SetInput(1, Direction.Left);

            var result = SnakesEngine.Step(s);

            Assert.IsFalse(s.Snakes[0].IsAlive);
            Assert.IsFalse(s.Snakes[1].IsAlive);
            Assert.Contains(0, result.DiedThisTick);
            Assert.Contains(1, result.DiedThisTick);
            Assert.IsTrue(result.MatchOver);
            Assert.IsNull(result.WinnerIndex);
        }

        [Test]
        public void Match_ends_with_winner_when_one_dies()
        {
            var s = NewState(players: 2, w: 6, h: 6);
            s.Food.Clear();
            // Replace the corner-spawned snakes with a configuration where P1
            // is one step from the wall and P2 has room to move.
            s.Snakes.Clear();
            s.Snakes.Add(new SnakeState(0, new GridPos(4, 3), Direction.Right, 1));
            s.Snakes.Add(new SnakeState(1, new GridPos(2, 2), Direction.Up, 1));

            StepResult last = default;
            for (int i = 0; i < 5 && !last.MatchOver; i++)
                last = SnakesEngine.Step(s);

            Assert.IsTrue(last.MatchOver);
            Assert.AreEqual(1, last.WinnerIndex);
        }

        [Test]
        public void U_turn_input_is_rejected()
        {
            var s = NewState();
            var snake = s.Snakes[0];
            var heading = snake.Heading;
            s.SetInput(0, heading.Opposite());
            Assert.AreEqual(heading, snake.PendingHeading,
                "should refuse a 180-degree input");
        }

        [Test]
        public void Same_seed_produces_same_food_sequence()
        {
            var a = NewState(seed: 42);
            var b = NewState(seed: 42);
            for (int i = 0; i < 30; i++)
            {
                SnakesEngine.Step(a);
                SnakesEngine.Step(b);
            }
            Assert.AreEqual(a.Food.Count, b.Food.Count);
            foreach (var p in a.Food) Assert.IsTrue(b.Food.Contains(p),
                $"food mismatch at {p}");
        }
    }
}
