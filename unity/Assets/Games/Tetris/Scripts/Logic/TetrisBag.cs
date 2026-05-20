using System;
using System.Collections.Generic;

namespace MiniGames.Games.Tetris.Logic
{
    /// <summary>
    /// 7-bag random source: every 7 pieces drawn = one of each tetromino,
    /// in shuffled order. This is the modern Tetris standard and ensures
    /// you can never go 14+ pieces without seeing an I. Deterministic by
    /// seed, identical sequence on every device with the same seed
    /// (multiplayer parity).
    /// </summary>
    public sealed class TetrisBag
    {
        private readonly Random _rng;
        private readonly Queue<TetrominoType> _queue = new Queue<TetrominoType>();

        public TetrisBag(int seed) { _rng = new Random(seed); }

        public TetrominoType Next()
        {
            if (_queue.Count == 0) RefillBag();
            return _queue.Dequeue();
        }

        public TetrominoType Peek()
        {
            if (_queue.Count == 0) RefillBag();
            return _queue.Peek();
        }

        private void RefillBag()
        {
            var bag = new[] {
                TetrominoType.I, TetrominoType.O, TetrominoType.T,
                TetrominoType.S, TetrominoType.Z, TetrominoType.J, TetrominoType.L
            };
            // Fisher-Yates shuffle.
            for (int i = bag.Length - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (bag[i], bag[j]) = (bag[j], bag[i]);
            }
            foreach (var t in bag) _queue.Enqueue(t);
        }
    }
}
