using System;
using System.Collections.Generic;

namespace MiniGames.Games.ColorBlocks.Logic
{
    /// <summary>
    /// Deterministic shape generator. Same seed produces the same sequence on
    /// every device, which is what we need for multiplayer parity.
    /// </summary>
    public sealed class PieceBag
    {
        private readonly Random _rng;
        private readonly IReadOnlyList<PieceShape> _catalog;

        public PieceBag(int seed, IReadOnlyList<PieceShape> catalog = null)
        {
            _rng = new Random(seed);
            _catalog = catalog ?? PieceCatalog.All;
        }

        public PieceShape Next() => _catalog[_rng.Next(_catalog.Count)];

        public PieceShape[] NextHand(int count = 3)
        {
            var hand = new PieceShape[count];
            for (int i = 0; i < count; i++) hand[i] = Next();
            return hand;
        }
    }
}
