using MiniGames.Games.Snakes.AI;
using MiniGames.Games.Snakes.Logic;
using NUnit.Framework;

namespace MiniGames.Tests.Games.Snakes
{
    public class SimpleSnakesAITests
    {
        [Test]
        public void AI_keeps_current_heading_when_safe()
        {
            var s = new SnakesGameState(20, 20, playerCount: 1, seed: 1);
            s.Food.Clear();
            var d = new SimpleSnakesAI().Choose(s, 0);
            Assert.AreEqual(s.Snakes[0].Heading, d,
                "AI should keep heading when no danger or food nearby");
        }

        [Test]
        public void AI_turns_to_avoid_a_wall()
        {
            var s = new SnakesGameState(6, 6, playerCount: 1, seed: 1);
            s.Food.Clear();
            // Park snake one cell from the right wall, heading right.
            s.Snakes.Clear();
            s.Snakes.Add(new SnakeState(0, new GridPos(5, 3), Direction.Right, initialLength: 1));

            var d = new SimpleSnakesAI().Choose(s, 0);
            Assert.AreNotEqual(Direction.Right, d, "AI should not march into a wall");
        }

        [Test]
        public void AI_never_chooses_a_U_turn()
        {
            var s = new SnakesGameState(20, 20, playerCount: 1, seed: 1);
            // Snake 0 starts heading Right with body extending left.
            // A U-turn (Left) would crash into the body.
            var d = new SimpleSnakesAI().Choose(s, 0);
            Assert.AreNotEqual(s.Snakes[0].Heading.Opposite(), d);
        }
    }
}
