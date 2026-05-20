using System;

namespace MiniGames.Games.ColorBlocks.Logic
{
    /// <summary>
    /// Single-player game session. Holds board + current hand of 3 pieces,
    /// awards score, detects game over. Multiplayer wraps several of these
    /// (one per player) and forwards events between them.
    /// </summary>
    public sealed class ColorBlocksGame
    {
        private const int HandSize = 3;

        public BoardState Board { get; }
        public PieceShape[] Hand { get; }
        public int Score { get; private set; }
        public bool IsGameOver { get; private set; }

        private readonly PieceBag _bag;

        public event Action<PlaceResult, int> Placed;  // result, scoreDelta
        public event Action GameOver;
        public event Action HandRefilled;

        public ColorBlocksGame(int seed)
        {
            Board = new BoardState();
            _bag = new PieceBag(seed);
            Hand = _bag.NextHand(HandSize);
        }

        public bool CanPlay(int handIndex, int originX, int originY)
        {
            if (IsGameOver) return false;
            if (handIndex < 0 || handIndex >= Hand.Length) return false;
            var shape = Hand[handIndex];
            if (shape == null) return false;
            return BoardEngine.CanPlace(Board, shape, originX, originY);
        }

        public bool TryPlay(int handIndex, int originX, int originY, out PlaceResult result)
        {
            result = default;
            if (!CanPlay(handIndex, originX, originY)) return false;

            var shape = Hand[handIndex];
            if (!BoardEngine.TryPlace(Board, shape, originX, originY, out result)) return false;

            Hand[handIndex] = null;
            var delta = ScoringRules.ScoreFor(result);
            Score += delta;
            Placed?.Invoke(result, delta);

            if (HandEmpty())
            {
                RefillHand();
                HandRefilled?.Invoke();
            }

            CheckGameOver();
            return true;
        }

        private bool HandEmpty()
        {
            for (int i = 0; i < Hand.Length; i++)
                if (Hand[i] != null) return false;
            return true;
        }

        private void RefillHand()
        {
            var fresh = _bag.NextHand(HandSize);
            for (int i = 0; i < Hand.Length; i++) Hand[i] = fresh[i];
        }

        private void CheckGameOver()
        {
            // Game over only when none of the remaining pieces in hand fit.
            for (int i = 0; i < Hand.Length; i++)
            {
                var s = Hand[i];
                if (s == null) continue;
                if (BoardEngine.HasAnyValidPlacement(Board, s)) return;
            }
            IsGameOver = true;
            GameOver?.Invoke();
        }
    }
}
