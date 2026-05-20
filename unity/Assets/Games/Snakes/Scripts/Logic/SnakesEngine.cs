using System.Collections.Generic;

namespace MiniGames.Games.Snakes.Logic
{
    public struct StepResult
    {
        public List<int> DiedThisTick;
        public List<int> AteThisTick;
        public bool MatchOver;        // true when <=1 alive in multi, or last died in solo
        public int? WinnerIndex;      // null if no single winner (everyone died simultaneously)
    }

    /// <summary>
    /// One simulation tick. Order:
    ///   1. Commit each snake's PendingHeading -> Heading.
    ///   2. Compute new heads.
    ///   3. Detect food consumption (mark grow).
    ///   4. Move bodies (add new head; if not growing, drop tail).
    ///   5. Detect collisions (wall / self / other) and kill.
    ///   6. Respawn food up to target.
    /// </summary>
    public static class SnakesEngine
    {
        public static StepResult Step(SnakesGameState s)
        {
            s.Tick++;
            var died = new List<int>();
            var ate = new List<int>();

            // 1. Commit headings.
            for (int i = 0; i < s.Snakes.Count; i++)
            {
                var snake = s.Snakes[i];
                if (!snake.IsAlive) continue;
                if (snake.PendingHeading != snake.Heading.Opposite())
                    snake.Heading = snake.PendingHeading;
            }

            // 2-3. Compute new heads + detect food.
            var newHeads = new GridPos[s.Snakes.Count];
            var willGrow = new bool[s.Snakes.Count];
            for (int i = 0; i < s.Snakes.Count; i++)
            {
                var snake = s.Snakes[i];
                if (!snake.IsAlive) continue;
                newHeads[i] = snake.Heading.Step(snake.Head);
                if (s.Food.Contains(newHeads[i]))
                {
                    willGrow[i] = true;
                    ate.Add(i);
                    snake.FoodEaten++;
                    snake.PendingGrowth++;
                }
            }

            // 4. Move bodies. Crucial: drop tails BEFORE collision check so a
            //    snake can chase its own tail into a freshly-vacated cell.
            for (int i = 0; i < s.Snakes.Count; i++)
            {
                var snake = s.Snakes[i];
                if (!snake.IsAlive) continue;
                if (snake.PendingGrowth > 0)
                {
                    snake.PendingGrowth--;
                    // Don't drop tail this tick - snake grows by one cell.
                }
                else
                {
                    snake.Body.RemoveLast();
                }
                snake.Body.AddFirst(newHeads[i]);
            }

            // 5. Collision detection - resolve all simultaneously. Decisions
            //    are made looking at the pre-resolution alive set so two
            //    snakes can kill each other in the same tick.
            var toKill = new List<int>();
            for (int i = 0; i < s.Snakes.Count; i++)
            {
                var a = s.Snakes[i];
                if (!a.IsAlive) continue;
                var head = a.Head;

                // Wall.
                if (!s.InBounds(head)) { toKill.Add(i); continue; }

                // Self (skip head: index 0).
                int nodeIdx = 0;
                bool died_ = false;
                foreach (var p in a.Body)
                {
                    if (nodeIdx > 0 && p == head) { died_ = true; break; }
                    nodeIdx++;
                }
                if (died_) { toKill.Add(i); continue; }

                // Other snakes' bodies (use current alive set; not yet-killed flags).
                for (int j = 0; j < s.Snakes.Count; j++)
                {
                    if (j == i) continue;
                    var b = s.Snakes[j];
                    if (!b.IsAlive) continue;
                    if (b.ContainsAnywhere(head)) { toKill.Add(i); break; }
                }
            }
            foreach (var idx in toKill) Kill(s.Snakes[idx], died);

            // 6. Eat food + respawn.
            foreach (var idx in ate) s.Food.Remove(s.Snakes[idx].Head);
            while (s.Food.Count < SnakesGameState.FoodTargetCount && s.SpawnFood()) { }

            // 7. Match-over check.
            int aliveCount = 0; int winner = -1;
            for (int i = 0; i < s.Snakes.Count; i++)
                if (s.Snakes[i].IsAlive) { aliveCount++; winner = i; }

            bool over = s.Snakes.Count == 1 ? aliveCount == 0 : aliveCount <= 1;

            return new StepResult
            {
                DiedThisTick = died,
                AteThisTick = ate,
                MatchOver = over,
                WinnerIndex = over && aliveCount == 1 ? winner : (int?)null
            };
        }

        private static void Kill(SnakeState s, List<int> died)
        {
            if (!s.IsAlive) return;
            s.IsAlive = false;
            died.Add(s.PlayerIndex);
        }
    }
}
