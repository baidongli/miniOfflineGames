namespace MiniGames.Games.MazePaint.Logic
{
    /// <summary>
    /// 2D grid where each cell stores either an owner (a player index 0-3,
    /// or -1 for empty) and an optional in-progress trail by some player.
    /// Owner and trail are independent layers because a trail can run
    /// through opponent territory without immediately claiming it.
    /// </summary>
    public sealed class MazeBoard
    {
        public const int DefaultSize = 24;
        public readonly int Size;
        private readonly sbyte[] _owner;
        private readonly sbyte[] _trail;

        public MazeBoard(int size = DefaultSize)
        {
            Size = size;
            _owner = new sbyte[size * size];
            _trail = new sbyte[size * size];
            for (int i = 0; i < _owner.Length; i++) { _owner[i] = -1; _trail[i] = -1; }
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Size && y < Size;
        public bool InBounds(MazePos p) => InBounds(p.X, p.Y);

        public int OwnerAt(int x, int y) => _owner[y * Size + x];
        public int OwnerAt(MazePos p) => OwnerAt(p.X, p.Y);
        public void SetOwner(int x, int y, int p) { _owner[y * Size + x] = (sbyte)p; }
        public void SetOwner(MazePos p, int player) => SetOwner(p.X, p.Y, player);

        public int TrailAt(int x, int y) => _trail[y * Size + x];
        public int TrailAt(MazePos p) => TrailAt(p.X, p.Y);
        public void SetTrail(int x, int y, int p) { _trail[y * Size + x] = (sbyte)p; }
        public void SetTrail(MazePos p, int player) => SetTrail(p.X, p.Y, player);
        public void ClearTrail(int x, int y) { _trail[y * Size + x] = -1; }
        public void ClearTrail(MazePos p) => ClearTrail(p.X, p.Y);

        public int CountOwned(int player)
        {
            int n = 0;
            for (int i = 0; i < _owner.Length; i++)
                if (_owner[i] == player) n++;
            return n;
        }
    }
}
