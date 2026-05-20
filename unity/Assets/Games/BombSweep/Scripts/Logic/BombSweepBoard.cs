using System;

namespace MiniGames.Games.BombSweep.Logic
{
    /// <summary>
    /// Classic Bomberman-style arena. Default is 13x11 with hard walls in a
    /// checkerboard pattern at every (odd, odd) cell, and soft blocks
    /// scattered throughout the rest (except player spawn corners).
    /// </summary>
    public sealed class BombSweepBoard
    {
        public const int DefaultWidth = 13;
        public const int DefaultHeight = 11;

        public readonly int Width;
        public readonly int Height;
        private readonly byte[] _cells;

        public BombSweepBoard(int width = DefaultWidth, int height = DefaultHeight)
        {
            Width = width;
            Height = height;
            _cells = new byte[width * height];
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public bool InBounds(BombPos p) => InBounds(p.X, p.Y);

        public CellType Get(int x, int y) => (CellType)_cells[y * Width + x];
        public CellType Get(BombPos p) => Get(p.X, p.Y);
        public void Set(int x, int y, CellType c) => _cells[y * Width + x] = (byte)c;
        public void Set(BombPos p, CellType c) => Set(p.X, p.Y, c);

        public bool IsWalkable(int x, int y)
        {
            var c = Get(x, y);
            return c == CellType.Empty
                || c == CellType.PowerBombs
                || c == CellType.PowerRange
                || c == CellType.PowerSpeed;
        }
        public bool IsWalkable(BombPos p) => IsWalkable(p.X, p.Y);

        /// <summary>True if an explosion ray hitting this cell can pass through to the next cell.</summary>
        public bool ExplosionPassesThrough(CellType c) =>
            c == CellType.Empty
            || c == CellType.PowerBombs
            || c == CellType.PowerRange
            || c == CellType.PowerSpeed;

        /// <summary>
        /// Generate a standard arena: hard walls at (odd, odd), soft blocks
        /// elsewhere except corners 2x2 around each player spawn.
        /// </summary>
        public void GenerateClassic(int playerCount, System.Random rng)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    if (x == 0 || y == 0 || x == Width - 1 || y == Height - 1)
                        Set(x, y, CellType.HardWall);
                    else if ((x % 2 == 0) && (y % 2 == 0))
                        Set(x, y, CellType.HardWall);
                    else
                        Set(x, y, CellType.Empty);
                }

            // Player spawn corners: keep clear in a 2x2 L-shape.
            var spawns = SpawnCorners(playerCount);
            foreach (var spawn in spawns)
            {
                Set(spawn.X, spawn.Y, CellType.Empty);
                if (InBounds(spawn.X + 1, spawn.Y) && Get(spawn.X + 1, spawn.Y) != CellType.HardWall)
                    Set(spawn.X + 1, spawn.Y, CellType.Empty);
                if (InBounds(spawn.X - 1, spawn.Y) && Get(spawn.X - 1, spawn.Y) != CellType.HardWall)
                    Set(spawn.X - 1, spawn.Y, CellType.Empty);
                if (InBounds(spawn.X, spawn.Y + 1) && Get(spawn.X, spawn.Y + 1) != CellType.HardWall)
                    Set(spawn.X, spawn.Y + 1, CellType.Empty);
                if (InBounds(spawn.X, spawn.Y - 1) && Get(spawn.X, spawn.Y - 1) != CellType.HardWall)
                    Set(spawn.X, spawn.Y - 1, CellType.Empty);
            }

            // Scatter soft blocks: ~65% of remaining empty cells get a soft block.
            for (int y = 1; y < Height - 1; y++)
                for (int x = 1; x < Width - 1; x++)
                    if (Get(x, y) == CellType.Empty && rng.NextDouble() < 0.65)
                    {
                        // Skip if this is inside a spawn safe zone.
                        bool isSafeZone = false;
                        foreach (var s in spawns)
                        {
                            if (System.Math.Abs(x - s.X) + System.Math.Abs(y - s.Y) <= 2)
                            { isSafeZone = true; break; }
                        }
                        if (!isSafeZone) Set(x, y, CellType.SoftBlock);
                    }
        }

        /// <summary>Per-player spawn corners, in player-index order.</summary>
        public BombPos[] SpawnCorners(int playerCount)
        {
            var spots = new BombPos[]
            {
                new BombPos(1, 1),
                new BombPos(Width - 2, Height - 2),
                new BombPos(Width - 2, 1),
                new BombPos(1, Height - 2),
            };
            var result = new BombPos[playerCount];
            for (int i = 0; i < playerCount && i < spots.Length; i++) result[i] = spots[i];
            return result;
        }
    }
}
