using System;

namespace MiniGames.Games.FruitMerge.Logic
{
    /// <summary>
    /// Session wrapper: grid + next/hold fruit + score + game-over flag.
    /// </summary>
    public sealed class FruitMergeGame
    {
        public readonly FruitGrid Grid;
        private readonly FruitBag _bag;

        public byte NextFruit { get; private set; }
        public byte HoldFruit { get; private set; }
        public int Score { get; private set; }
        public int HighestTier { get; private set; }
        public bool IsGameOver { get; private set; }

        public event Action<DropResult> Dropped;
        public event Action GameOver;

        public FruitMergeGame(int seed)
        {
            Grid = new FruitGrid();
            _bag = new FruitBag(seed);
            NextFruit = _bag.Next();
            HoldFruit = 0;
        }

        public bool TryDrop(int column)
        {
            if (IsGameOver) return false;
            var r = FruitMergeEngine.Drop(Grid, column, NextFruit);
            if (!r.Placed)
            {
                if (r.GameOver) { IsGameOver = true; GameOver?.Invoke(); }
                return false;
            }
            Score += r.Score;
            if (r.HighestTierReached > HighestTier) HighestTier = r.HighestTierReached;
            NextFruit = _bag.Next();
            Dropped?.Invoke(r);

            // Game over if the column the player just dropped into is now full.
            if (Grid.LowestEmptyY(column) >= Grid.Height)
            {
                IsGameOver = true;
                GameOver?.Invoke();
            }
            return true;
        }

        /// <summary>Swap NextFruit with HoldFruit (or just stash if hold is empty).</summary>
        public void SwapHold()
        {
            if (HoldFruit == 0)
            {
                HoldFruit = NextFruit;
                NextFruit = _bag.Next();
            }
            else
            {
                var tmp = HoldFruit;
                HoldFruit = NextFruit;
                NextFruit = tmp;
            }
        }
    }
}
