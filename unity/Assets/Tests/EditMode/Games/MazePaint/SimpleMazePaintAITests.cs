using MiniGames.Games.MazePaint.AI;
using MiniGames.Games.MazePaint.Logic;
using NUnit.Framework;

namespace MiniGames.Tests.Games.MazePaint
{
    public class SimpleMazePaintAITests
    {
        [Test]
        public void AI_returns_a_direction_that_stays_on_board()
        {
            var s = new MazePaintGameState(12, playerCount: 1);
            var d = new SimpleMazePaintAI().Choose(s, 0);
            var next = s.Players[0].Head.Step(d);
            Assert.IsTrue(s.Board.InBounds(next),
                $"AI chose {d} which would move off the board");
        }

        [Test]
        public void AI_avoids_own_trail_when_possible()
        {
            var s = new MazePaintGameState(12, playerCount: 1);
            var p = s.Players[0];
            p.Head = new MazePos(5, 5);
            p.Heading = MazeDir.Right;
            p.PendingHeading = MazeDir.Right;
            // Plant own trail directly to the right of the head.
            s.Board.SetTrail(6, 5, 0);
            p.ActiveTrail.Add(new MazePos(6, 5));

            var d = new SimpleMazePaintAI().Choose(s, 0);
            Assert.AreNotEqual(MazeDir.Right, d,
                "AI should avoid stepping into its own trail");
        }
    }
}
