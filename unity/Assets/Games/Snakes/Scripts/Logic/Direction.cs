using System;

namespace MiniGames.Games.Snakes.Logic
{
    public enum Direction : byte
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3
    }

    public static class DirectionExt
    {
        public static Direction Opposite(this Direction d) => (Direction)(((byte)d + 2) & 3);
        public static GridPos Step(this Direction d, GridPos p) => d switch
        {
            Direction.Up    => new GridPos(p.X, p.Y + 1),
            Direction.Down  => new GridPos(p.X, p.Y - 1),
            Direction.Left  => new GridPos(p.X - 1, p.Y),
            Direction.Right => new GridPos(p.X + 1, p.Y),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
