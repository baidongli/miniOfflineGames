using System;

namespace MiniGames.Games.Snakes.Logic
{
    public readonly struct GridPos : IEquatable<GridPos>
    {
        public readonly short X;
        public readonly short Y;
        public GridPos(int x, int y) { X = (short)x; Y = (short)y; }
        public bool Equals(GridPos o) => X == o.X && Y == o.Y;
        public override bool Equals(object obj) => obj is GridPos o && Equals(o);
        public override int GetHashCode() => (X << 16) ^ Y;
        public override string ToString() => $"({X},{Y})";
        public static bool operator ==(GridPos a, GridPos b) => a.Equals(b);
        public static bool operator !=(GridPos a, GridPos b) => !a.Equals(b);
    }
}
