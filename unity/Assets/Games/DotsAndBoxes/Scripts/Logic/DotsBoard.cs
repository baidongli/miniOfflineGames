namespace MiniGames.Games.DotsAndBoxes.Logic
{
    /// <summary>Which side of a box an edge is, or equivalently a wall direction.</summary>
    public enum EdgeKind : byte
    {
        Horizontal = 0,  // between (x, y) and (x+1, y)
        Vertical = 1,    // between (x, y) and (x, y+1)
    }

    public readonly struct EdgeId
    {
        public readonly EdgeKind Kind;
        public readonly byte X;
        public readonly byte Y;
        public EdgeId(EdgeKind kind, int x, int y) { Kind = kind; X = (byte)x; Y = (byte)y; }
        public override string ToString() => $"{(Kind == EdgeKind.Horizontal ? "H" : "V")}({X},{Y})";
    }

    /// <summary>
    /// Dots-and-Boxes grid. With BoxWidth x BoxHeight boxes, there are
    /// (BoxWidth+1) x (BoxHeight+1) dots. Each box has 4 edges; the same
    /// edge object is shared between adjacent boxes when in the interior.
    /// </summary>
    public sealed class DotsBoard
    {
        public const int DefaultBoxes = 5;

        public readonly int BoxWidth;
        public readonly int BoxHeight;

        // [(BoxWidth) x (BoxHeight+1)] horizontal edges; true = drawn.
        private readonly bool[] _hEdges;
        // [(BoxWidth+1) x (BoxHeight)] vertical edges.
        private readonly bool[] _vEdges;

        // Box owner, -1 = unclaimed.
        private readonly sbyte[] _owners;

        public DotsBoard(int boxWidth = DefaultBoxes, int boxHeight = DefaultBoxes)
        {
            BoxWidth = boxWidth;
            BoxHeight = boxHeight;
            _hEdges = new bool[boxWidth * (boxHeight + 1)];
            _vEdges = new bool[(boxWidth + 1) * boxHeight];
            _owners = new sbyte[boxWidth * boxHeight];
            for (int i = 0; i < _owners.Length; i++) _owners[i] = -1;
        }

        public bool HasHEdge(int x, int y) => _hEdges[y * BoxWidth + x];
        public bool HasVEdge(int x, int y) => _vEdges[y * (BoxWidth + 1) + x];
        public void SetHEdge(int x, int y, bool v) => _hEdges[y * BoxWidth + x] = v;
        public void SetVEdge(int x, int y, bool v) => _vEdges[y * (BoxWidth + 1) + x] = v;

        public bool IsEdgeDrawn(EdgeId e)
            => e.Kind == EdgeKind.Horizontal ? HasHEdge(e.X, e.Y) : HasVEdge(e.X, e.Y);

        public bool IsEdgeInBounds(EdgeId e) => e.Kind == EdgeKind.Horizontal
            ? e.X < BoxWidth && e.Y <= BoxHeight
            : e.X <= BoxWidth && e.Y < BoxHeight;

        public void DrawEdge(EdgeId e)
        {
            if (e.Kind == EdgeKind.Horizontal) SetHEdge(e.X, e.Y, true);
            else SetVEdge(e.X, e.Y, true);
        }

        public int BoxOwner(int x, int y) => _owners[y * BoxWidth + x];
        public void SetBoxOwner(int x, int y, int player) => _owners[y * BoxWidth + x] = (sbyte)player;

        public int CountOwned(int player)
        {
            int n = 0;
            for (int i = 0; i < _owners.Length; i++) if (_owners[i] == player) n++;
            return n;
        }

        public int BoxesRemaining()
        {
            int n = 0;
            for (int i = 0; i < _owners.Length; i++) if (_owners[i] < 0) n++;
            return n;
        }

        /// <summary>How many of box (bx, by)'s 4 edges are drawn.</summary>
        public int BoxEdgeCount(int bx, int by)
        {
            int n = 0;
            if (HasHEdge(bx, by)) n++;          // bottom
            if (HasHEdge(bx, by + 1)) n++;      // top
            if (HasVEdge(bx, by)) n++;          // left
            if (HasVEdge(bx + 1, by)) n++;      // right
            return n;
        }
    }
}
