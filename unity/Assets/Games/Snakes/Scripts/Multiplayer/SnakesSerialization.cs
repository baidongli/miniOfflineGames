using MiniGames.Games.Snakes.Logic;

namespace MiniGames.Games.Snakes.Multiplayer
{
    /// <summary>Convert SnakesGameState <-> SnakeSnapshot for wire transport.</summary>
    public static class SnakesSerialization
    {
        public static SnakeSnapshot Encode(SnakesGameState s)
        {
            var snap = new SnakeSnapshot { Tick = s.Tick };
            foreach (var snake in s.Snakes)
            {
                var w = new SnakeWire
                {
                    PlayerIndex = snake.PlayerIndex,
                    IsAlive = snake.IsAlive,
                    Heading = (byte)snake.Heading
                };
                foreach (var p in snake.Body)
                {
                    w.BodyFlat.Add(p.X);
                    w.BodyFlat.Add(p.Y);
                }
                snap.Snakes.Add(w);
            }
            foreach (var f in s.Food) { snap.FoodFlat.Add(f.X); snap.FoodFlat.Add(f.Y); }
            return snap;
        }

        /// <summary>Apply a snapshot in place to an existing game state.</summary>
        public static void ApplyTo(SnakeSnapshot snap, SnakesGameState s)
        {
            s.Tick = snap.Tick;
            // Resize snake list if needed.
            while (s.Snakes.Count < snap.Snakes.Count)
                s.Snakes.Add(new SnakeState(s.Snakes.Count, new GridPos(0, 0), Direction.Right, 1));

            for (int i = 0; i < snap.Snakes.Count; i++)
            {
                var w = snap.Snakes[i];
                var snake = s.Snakes[i];
                snake.IsAlive = w.IsAlive;
                snake.Heading = (Direction)w.Heading;
                snake.PendingHeading = snake.Heading;
                snake.Body.Clear();
                for (int k = 0; k + 1 < w.BodyFlat.Count; k += 2)
                    snake.Body.AddLast(new GridPos(w.BodyFlat[k], w.BodyFlat[k + 1]));
            }
            s.Food.Clear();
            for (int k = 0; k + 1 < snap.FoodFlat.Count; k += 2)
                s.Food.Add(new GridPos(snap.FoodFlat[k], snap.FoodFlat[k + 1]));
        }
    }
}
