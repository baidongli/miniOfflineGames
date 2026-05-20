namespace MiniGames.Games.ColorBlocks.Logic
{
    public readonly struct Cell
    {
        public readonly sbyte X;
        public readonly sbyte Y;
        public Cell(int x, int y) { X = (sbyte)x; Y = (sbyte)y; }
        public override string ToString() => $"({X},{Y})";
    }
}
