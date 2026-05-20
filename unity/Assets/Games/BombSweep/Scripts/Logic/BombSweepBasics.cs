using System;

namespace MiniGames.Games.BombSweep.Logic
{
    public enum BombDir : byte
    {
        None = 0,
        Up = 1,
        Right = 2,
        Down = 3,
        Left = 4
    }

    public enum CellType : byte
    {
        Empty       = 0,
        HardWall    = 1,  // indestructible, blocks movement + explosion
        SoftBlock   = 2,  // destructible, blocks movement until destroyed
        PowerBombs  = 3,  // pickup: +1 max simultaneous bombs
        PowerRange  = 4,  // pickup: +1 explosion range
        PowerSpeed  = 5,  // pickup: faster movement
    }

    public readonly struct BombPos : IEquatable<BombPos>
    {
        public readonly short X;
        public readonly short Y;
        public BombPos(int x, int y) { X = (short)x; Y = (short)y; }
        public bool Equals(BombPos o) => X == o.X && Y == o.Y;
        public override bool Equals(object obj) => obj is BombPos o && Equals(o);
        public override int GetHashCode() => (X << 16) ^ Y;
        public override string ToString() => $"({X},{Y})";
        public BombPos Step(BombDir d) => d switch
        {
            BombDir.Up    => new BombPos(X, Y + 1),
            BombDir.Down  => new BombPos(X, Y - 1),
            BombDir.Left  => new BombPos(X - 1, Y),
            BombDir.Right => new BombPos(X + 1, Y),
            _ => this
        };
    }
}
