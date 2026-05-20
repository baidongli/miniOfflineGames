using MiniGames.Games.ColorBlocks.AI;
using MiniGames.Games.ColorBlocks.Logic;
using MiniGames.Games.FruitMerge.AI;
using MiniGames.Games.FruitMerge.Logic;
using MiniGames.Games.MazePaint.AI;
using MiniGames.Games.MazePaint.Logic;
using MiniGames.Games.Snakes.AI;
using MiniGames.Games.Snakes.Logic;
using NUnit.Framework;

namespace MiniGames.Tests.Games
{
    /// <summary>
    /// Smoke tests: each CPU controller can drive its game forward for
    /// multiple ticks/turns without crashing or making illegal moves.
    /// </summary>
    public class CpuControllerTests
    {
        [Test]
        public void CpuColorBlocks_plays_several_turns()
        {
            var game = new ColorBlocksGame(seed: 1);
            var cpu = new CpuColorBlocksController(game, new GreedyColorBlocksAI());
            int turns = 0;
            // Cap at 50 turns to keep the test fast even if the CPU is bad.
            while (turns < 50 && cpu.TakeTurn()) turns++;
            Assert.GreaterOrEqual(turns, 3, "CPU should manage at least a few turns");
        }

        [Test]
        public void CpuSnakes_keeps_snake_alive_for_more_than_one_tick()
        {
            var s = new SnakesGameState(20, 20, playerCount: 1, seed: 1);
            var cpu = new CpuSnakesController(s, 0, new SimpleSnakesAI());
            int ticks = 0;
            while (ticks < 30 && s.Snakes[0].IsAlive)
            {
                cpu.BeforeTick();
                SnakesEngine.Step(s);
                ticks++;
            }
            Assert.Greater(ticks, 5, "CPU snake should survive more than a couple of ticks");
        }

        [Test]
        public void CpuMazePaint_keeps_player_alive_for_more_than_one_tick()
        {
            var s = new MazePaintGameState(20, playerCount: 1);
            var cpu = new CpuMazePaintController(s, 0, new SimpleMazePaintAI());
            int ticks = 0;
            while (ticks < 30 && s.Players[0].IsAlive)
            {
                cpu.BeforeTick();
                MazePaintEngine.Step(s);
                ticks++;
            }
            Assert.Greater(ticks, 5);
        }

        [Test]
        public void CpuFruitMerge_drops_several_turns_without_immediate_game_over()
        {
            var game = new FruitMergeGame(seed: 1);
            var cpu = new CpuFruitMergeController(game, new GreedyFruitMergeAI());
            int turns = 0;
            while (turns < 30 && cpu.TakeTurn()) turns++;
            Assert.GreaterOrEqual(turns, 5);
        }

        [Test]
        public void CpuSnakes_eventually_ends_a_solo_game()
        {
            // 6x6 board so the snake runs out of room reasonably fast.
            var s = new SnakesGameState(6, 6, playerCount: 1, seed: 1);
            var cpu = new CpuSnakesController(s, 0, new SimpleSnakesAI());
            int ticks = 0;
            while (ticks < 500 && s.Snakes[0].IsAlive)
            {
                cpu.BeforeTick();
                SnakesEngine.Step(s);
                ticks++;
            }
            Assert.IsFalse(s.Snakes[0].IsAlive, "snake should eventually die on a tiny board");
            Assert.Less(ticks, 500, "should not loop forever");
        }
    }
}
