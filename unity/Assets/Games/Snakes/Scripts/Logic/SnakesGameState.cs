using System;
using System.Collections.Generic;

namespace MiniGames.Games.Snakes.Logic
{
    /// <summary>
    /// All mutable state of a Snakes session. The engine reads inputs, calls
    /// Step(), produces deterministic results. Same seed + same input
    /// sequence = same outcome on every device.
    /// </summary>
    public sealed class SnakesGameState
    {
        public const int InitialLength = 3;
        public const int FoodTargetCount = 3;

        public readonly int Width;
        public readonly int Height;
        public readonly List<SnakeState> Snakes;
        public readonly HashSet<GridPos> Food = new HashSet<GridPos>();
        public int Tick;
        private readonly Random _rng;

        public SnakesGameState(int width, int height, int playerCount, int seed)
        {
            Width = width;
            Height = height;
            _rng = new Random(seed);
            Snakes = new List<SnakeState>(playerCount);
            SpawnSnakes(playerCount);
            for (int i = 0; i < FoodTargetCount; i++) SpawnFood();
        }

        public bool InBounds(GridPos p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;

        public void SetInput(int playerIndex, Direction dir)
        {
            if (playerIndex < 0 || playerIndex >= Snakes.Count) return;
            var s = Snakes[playerIndex];
            if (!s.IsAlive) return;
            // Reject immediate U-turn.
            if (dir == s.Heading.Opposite()) return;
            s.PendingHeading = dir;
        }

        private void SpawnSnakes(int playerCount)
        {
            // Spawn at corners, heading inward, so 4 players never start adjacent.
            var spots = new (GridPos head, Direction dir)[]
            {
                (new GridPos(2, 2),                      Direction.Right),
                (new GridPos(Width - 3, Height - 3),     Direction.Left),
                (new GridPos(2, Height - 3),             Direction.Right),
                (new GridPos(Width - 3, 2),              Direction.Left),
            };
            for (int i = 0; i < playerCount && i < spots.Length; i++)
                Snakes.Add(new SnakeState(i, spots[i].head, spots[i].dir, InitialLength));
        }

        public bool SpawnFood()
        {
            // Pick a random empty cell. Bail out after a bounded number of
            // attempts to keep this O(1)-amortized even on a near-full board.
            for (int attempt = 0; attempt < 64; attempt++)
            {
                var p = new GridPos(_rng.Next(Width), _rng.Next(Height));
                if (Food.Contains(p)) continue;
                if (AnySnakeOccupies(p)) continue;
                Food.Add(p);
                return true;
            }
            return false;
        }

        public bool AnySnakeOccupies(GridPos p)
        {
            for (int i = 0; i < Snakes.Count; i++)
                if (Snakes[i].IsAlive && Snakes[i].ContainsAnywhere(p)) return true;
            return false;
        }
    }
}
