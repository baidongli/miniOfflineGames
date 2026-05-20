using System;

namespace MiniGames.Games.MazePaint.Logic
{
    public enum MazeDir : byte
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3
    }

    public readonly struct MazePos : IEquatable<MazePos>
    {
        public readonly short X;
        public readonly short Y;
        public MazePos(int x, int y) { X = (short)x; Y = (short)y; }
        public bool Equals(MazePos o) => X == o.X && Y == o.Y;
        public override bool Equals(object obj) => obj is MazePos o && Equals(o);
        public override int GetHashCode() => (X << 16) ^ Y;
        public override string ToString() => $"({X},{Y})";

        public MazePos Step(MazeDir d) => d switch
        {
            MazeDir.Up    => new MazePos(X, Y + 1),
            MazeDir.Down  => new MazePos(X, Y - 1),
            MazeDir.Left  => new MazePos(X - 1, Y),
            MazeDir.Right => new MazePos(X + 1, Y),
            _ => this
        };
    }

    public static class MazeDirExt
    {
        public static MazeDir Opposite(this MazeDir d) => (MazeDir)(((byte)d + 2) & 3);
    }
}
