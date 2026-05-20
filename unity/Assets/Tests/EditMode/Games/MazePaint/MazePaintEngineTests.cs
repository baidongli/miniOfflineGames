using MiniGames.Games.MazePaint.Logic;
using NUnit.Framework;

namespace MiniGames.Tests.Games.MazePaint
{
    public class MazePaintEngineTests
    {
        private static MazePaintGameState NewState(int playerCount = 2, int size = 12)
            => new MazePaintGameState(size, playerCount);

        [Test]
        public void Player_moves_one_cell_per_tick()
        {
            var s = NewState(playerCount: 1);
            var p = s.Players[0];
            var startHead = p.Head;
            MazePaintEngine.Step(s);
            Assert.AreEqual(startHead.Step(p.Heading), p.Head);
        }

        [Test]
        public void Off_board_kills_the_player()
        {
            var s = NewState(playerCount: 1, size: 6);
            // Force head near right edge, heading right.
            s.Players[0].Head = new MazePos(5, 3);
            s.Players[0].Heading = MazeDir.Right;
            s.Players[0].PendingHeading = MazeDir.Right;
            MazePaintEngine.Step(s);
            Assert.IsFalse(s.Players[0].IsAlive);
        }

        [Test]
        public void Stepping_onto_own_trail_kills_the_player()
        {
            var s = NewState(playerCount: 1, size: 12);
            // Park the player off-home so each move starts laying trail.
            s.Players[0].Head = new MazePos(5, 5);
            s.Players[0].Heading = MazeDir.Right;
            s.Players[0].PendingHeading = MazeDir.Right;

            // Walk: (5,5)->(6,5)[trail]->(6,6)[trail]->(5,6)[trail]->(5,5)->(6,5) SELF-HIT.
            var seq = new[]
            {
                MazeDir.Right, MazeDir.Up, MazeDir.Left, MazeDir.Down, MazeDir.Right
            };
            foreach (var d in seq)
            {
                s.SetInput(0, d);
                MazePaintEngine.Step(s);
                if (!s.Players[0].IsAlive) break;
            }

            Assert.IsFalse(s.Players[0].IsAlive,
                "player should die when stepping onto its own trail");
        }

        [Test]
        public void Trail_paints_owned_when_returning_to_home()
        {
            // 1 player, size 8. Home territory at (0,0)-(2,2).
            // Walk: (2,2)->(3,2)->(4,2)->(4,1)->(4,0)->(3,0)->(2,0).
            // (2,0) is back on home -> loop closes. Trail cells become owned,
            // plus the one cell (3,1) enclosed by trail+home.
            var s = NewState(playerCount: 1, size: 8);
            var p = s.Players[0];
            p.Head = new MazePos(2, 2);
            p.Heading = MazeDir.Right;
            p.PendingHeading = MazeDir.Right;
            int ownedBefore = s.Board.CountOwned(0);

            var seq = new[]
            {
                MazeDir.Right, MazeDir.Right,   // (3,2) (4,2)
                MazeDir.Down,                   // (4,1)
                MazeDir.Down,                   // (4,0)
                MazeDir.Left,                   // (3,0)
                MazeDir.Left                    // (2,0) - home again
            };
            foreach (var d in seq)
            {
                s.SetInput(0, d);
                MazePaintEngine.Step(s);
            }

            int ownedAfter = s.Board.CountOwned(0);
            Assert.IsTrue(p.IsAlive);
            Assert.AreEqual(0, p.ActiveTrail.Count, "trail should be cleared after capture");
            // 5 trail cells + 1 enclosed = +6.
            Assert.AreEqual(ownedBefore + 6, ownedAfter,
                $"expected {ownedBefore + 6} owned cells after capture, got {ownedAfter}");
            Assert.AreEqual(0, s.Board.OwnerAt(3, 1), "enclosed cell should belong to player 0");
        }

        [Test]
        public void Stepping_onto_another_players_trail_kills_them()
        {
            // Two players. P0 lays a trail; P1 walks into one of P0's trail cells.
            var s = NewState(playerCount: 2, size: 12);
            var p0 = s.Players[0];
            var p1 = s.Players[1];

            // Position them so paths cross with one tick of separation:
            // tick 1: P0 lays trail at (6,5); P1 moves to (6,4).
            // tick 2: P0 moves to (7,5); P1 moves to (6,5) -> hits P0's trail at (6,5).
            p0.Head = new MazePos(5, 5);
            p0.Heading = MazeDir.Right;  p0.PendingHeading = MazeDir.Right;
            p1.Head = new MazePos(6, 3);
            p1.Heading = MazeDir.Up;     p1.PendingHeading = MazeDir.Up;

            MazePaintEngine.Step(s);
            Assert.IsTrue(p0.IsAlive && p1.IsAlive, "both should survive tick 1");

            MazePaintEngine.Step(s);
            Assert.IsFalse(p0.IsAlive, "P0 should die when P1 crosses its trail");
            Assert.IsTrue(p1.IsAlive,  "P1 should survive");
        }

        [Test]
        public void U_turn_input_is_rejected()
        {
            var s = NewState(playerCount: 1);
            var p = s.Players[0];
            var heading = p.Heading;
            s.SetInput(0, heading.Opposite());
            Assert.AreEqual(heading, p.PendingHeading);
        }
    }
}
