using MiniGames.Games.BombSweep.AI;
using MiniGames.Games.BombSweep.Logic;
using NUnit.Framework;

namespace MiniGames.Tests.Games.BombSweep
{
    public class BombSweepBoardTests
    {
        [Test]
        public void Generated_board_has_hard_walls_on_borders_and_odd_odd()
        {
            var rng = new System.Random(1);
            var b = new BombSweepBoard();
            b.GenerateClassic(playerCount: 4, rng);
            // Borders.
            for (int x = 0; x < b.Width; x++)
            {
                Assert.AreEqual(CellType.HardWall, b.Get(x, 0));
                Assert.AreEqual(CellType.HardWall, b.Get(x, b.Height - 1));
            }
            // Interior checkerboard hard walls at (even, even).
            Assert.AreEqual(CellType.HardWall, b.Get(2, 2));
        }

        [Test]
        public void Spawn_corners_are_walkable()
        {
            var rng = new System.Random(1);
            var b = new BombSweepBoard();
            b.GenerateClassic(4, rng);
            foreach (var c in b.SpawnCorners(4))
                Assert.IsTrue(b.IsWalkable(c.X, c.Y));
        }
    }

    public class BombSweepEngineTests
    {
        [Test]
        public void Player_moves_after_speed_ticks_elapse()
        {
            var s = new BombSweepGameState(playerCount: 1, seed: 1);
            var p = s.Players[0];
            var start = p.Pos;
            s.SetInput(0, BombDir.Right, false);

            // Need SpeedTicksPerCell ticks to step.
            int ticks = p.SpeedTicksPerCell;
            for (int i = 0; i < ticks; i++) BombSweepEngine.Step(s);
            // The step happens on the tick where MoveAccumulator reaches SpeedTicksPerCell.
            Assert.AreNotEqual(start, p.Pos, "player should have moved after speed ticks");
        }

        [Test]
        public void Bomb_placement_creates_a_bomb_and_decrements_after_ticks()
        {
            var s = new BombSweepGameState(playerCount: 1, seed: 1);
            s.SetInput(0, BombDir.None, placeBomb: true);
            BombSweepEngine.Step(s);
            Assert.AreEqual(1, s.Bombs.Count);
            Assert.AreEqual(1, s.Players[0].CurrentBombs);
            Assert.Less(s.Bombs[0].TicksUntilExplode, BombSweepGameState.BombFuseTicks);
        }

        [Test]
        public void Bomb_explodes_after_fuse_and_creates_explosion_cells()
        {
            var s = new BombSweepGameState(playerCount: 1, seed: 1);
            s.SetInput(0, BombDir.None, placeBomb: true);
            // Tick until the bomb explodes. The first Step places the bomb
            // (BombsCount goes 0 -> 1), so checking BombsCount as the loop
            // condition would skip the loop entirely. Drive on Explosions.
            int safety = 100;
            while (s.Explosions.Count == 0 && safety-- > 0)
                BombSweepEngine.Step(s);
            Assert.Greater(s.Explosions.Count, 0, "explosion should have been generated");
            Assert.AreEqual(0, s.Players[0].CurrentBombs, "owner's bomb count should reset on detonation");
        }

        [Test]
        public void Player_caught_in_explosion_dies()
        {
            var s = new BombSweepGameState(playerCount: 2, seed: 1);
            // Stand still right next to player 0 (forcibly relocate player 1).
            s.Players[1].Pos = s.Players[0].Pos.Step(BombDir.Right);
            // Force walkability of that cell.
            s.Board.Set(s.Players[1].Pos, CellType.Empty);
            s.SetInput(0, BombDir.None, placeBomb: true);
            int safety = 100;
            while (s.Players[1].IsAlive && safety-- > 0)
                BombSweepEngine.Step(s);
            Assert.IsFalse(s.Players[1].IsAlive);
        }

        [Test]
        public void Match_over_when_last_alive_in_multiplayer()
        {
            var s = new BombSweepGameState(playerCount: 2, seed: 1);
            // Kill player 1 directly for the test purpose.
            s.Players[1].IsAlive = false;
            var r = BombSweepEngine.Step(s);
            Assert.IsTrue(r.MatchOver);
            Assert.AreEqual(0, r.WinnerIndex);
        }
    }

    public class SimpleBombSweepAITests
    {
        [Test]
        public void AI_can_play_a_few_ticks_without_dying_immediately()
        {
            var s = new BombSweepGameState(playerCount: 2, seed: 1);
            var ai0 = new CpuBombSweepController(s, 0, new SimpleBombSweepAI());
            var ai1 = new CpuBombSweepController(s, 1, new SimpleBombSweepAI());
            for (int i = 0; i < 20; i++)
            {
                ai0.BeforeTick();
                ai1.BeforeTick();
                BombSweepEngine.Step(s);
            }
            // At least one survived 20 ticks (~2.5s).
            Assert.IsTrue(s.Players[0].IsAlive || s.Players[1].IsAlive);
        }
    }
}
