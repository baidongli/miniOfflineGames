using System;
using System.Collections.Generic;

namespace MiniGames.Games.BombSweep.Logic
{
    public sealed class BombSweepGameState
    {
        public const int BombFuseTicks = 24;          // ~3s at 8Hz
        public const int ExplosionFadeTicks = 4;      // explosion sticks around long enough for collision
        public const int PowerupDropChance = 30;      // percent

        public readonly BombSweepBoard Board;
        public readonly List<BombSweepPlayer> Players;
        public readonly List<Bomb> Bombs = new List<Bomb>();
        public readonly List<Explosion> Explosions = new List<Explosion>();
        public int Tick;
        public readonly Random Rng;

        public BombSweepGameState(int playerCount, int seed,
            int width = BombSweepBoard.DefaultWidth, int height = BombSweepBoard.DefaultHeight)
        {
            Rng = new Random(seed);
            Board = new BombSweepBoard(width, height);
            Board.GenerateClassic(playerCount, Rng);
            var spawns = Board.SpawnCorners(playerCount);
            Players = new List<BombSweepPlayer>(playerCount);
            for (int i = 0; i < playerCount; i++)
                Players.Add(new BombSweepPlayer(i, spawns[i]));
        }

        public void SetInput(int playerIndex, BombDir heading, bool placeBomb)
        {
            if (playerIndex < 0 || playerIndex >= Players.Count) return;
            var p = Players[playerIndex];
            if (!p.IsAlive) return;
            p.PendingHeading = heading;
            if (placeBomb) p.BombRequested = true;
        }

        public Bomb BombAt(BombPos pos)
        {
            for (int i = 0; i < Bombs.Count; i++)
                if (Bombs[i].Pos.Equals(pos)) return Bombs[i];
            return null;
        }
    }
}
