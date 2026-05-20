using System;

namespace MiniGames.Games.NumberMerge.Logic
{
    public sealed class NumberMergeGame
    {
        public readonly NumberMergeBoard Board = new NumberMergeBoard();
        public int Score { get; private set; }
        public byte MaxExponent { get; private set; }
        public bool ReachedGoal => MaxExponent >= NumberMergeBoard.WinExponent;
        public bool IsGameOver { get; private set; }
        public int SwipeCount { get; private set; }

        public event Action<SwipeResult> Swiped;
        public event Action GameOver;
        public event Action GoalReached;

        private readonly Random _rng;
        private bool _goalAnnounced;

        public NumberMergeGame(int seed)
        {
            _rng = new Random(seed);
            // Two initial tiles.
            NumberMergeEngine.SpawnTile(Board, _rng);
            NumberMergeEngine.SpawnTile(Board, _rng);
        }

        public bool TrySwipe(SwipeDir dir, out SwipeResult result)
        {
            result = default;
            if (IsGameOver) return false;
            result = NumberMergeEngine.Swipe(Board, dir);
            if (!result.AnyMoved) return false;

            Score += result.ScoreGained;
            if (result.MaxExponentReached > MaxExponent) MaxExponent = result.MaxExponentReached;
            SwipeCount++;

            NumberMergeEngine.SpawnTile(Board, _rng);

            if (!_goalAnnounced && ReachedGoal)
            {
                _goalAnnounced = true;
                GoalReached?.Invoke();
            }
            if (!NumberMergeEngine.HasAnyValidSwipe(Board))
            {
                IsGameOver = true;
                GameOver?.Invoke();
            }
            Swiped?.Invoke(result);
            return true;
        }
    }
}
