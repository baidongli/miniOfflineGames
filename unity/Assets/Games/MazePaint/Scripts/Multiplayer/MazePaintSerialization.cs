using MiniGames.Games.MazePaint.Logic;

namespace MiniGames.Games.MazePaint.Multiplayer
{
    public static class MazePaintSerialization
    {
        public static MazeSnapshot Encode(MazePaintGameState s)
        {
            int size = s.Board.Size;
            int n = size * size;
            var snap = new MazeSnapshot
            {
                Tick = s.Tick,
                BoardSize = size,
                OwnerBytes = new byte[n],
                TrailBytes = new byte[n]
            };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int k = y * size + x;
                    int owner = s.Board.OwnerAt(x, y);
                    int trail = s.Board.TrailAt(x, y);
                    // Shift so we can stuff -1 sentinel as 0.
                    snap.OwnerBytes[k] = (byte)(owner + 1);
                    snap.TrailBytes[k] = (byte)(trail + 1);
                }
            }
            foreach (var p in s.Players)
            {
                snap.Players.Add(new MazePlayerWire
                {
                    Index = p.Index,
                    IsAlive = p.IsAlive,
                    Heading = (byte)p.Heading,
                    HeadX = p.Head.X,
                    HeadY = p.Head.Y,
                    OwnedCells = p.OwnedCells
                });
            }
            return snap;
        }

        public static void ApplyTo(MazeSnapshot snap, MazePaintGameState s)
        {
            s.Tick = snap.Tick;
            int size = snap.BoardSize;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int k = y * size + x;
                    s.Board.SetOwner(x, y, snap.OwnerBytes[k] - 1);
                    int trail = snap.TrailBytes[k] - 1;
                    if (trail >= 0) s.Board.SetTrail(x, y, trail);
                    else            s.Board.ClearTrail(x, y);
                }
            }
            for (int i = 0; i < snap.Players.Count && i < s.Players.Count; i++)
            {
                var w = snap.Players[i];
                var p = s.Players[i];
                p.IsAlive = w.IsAlive;
                p.Heading = (MazeDir)w.Heading;
                p.PendingHeading = p.Heading;
                p.Head = new MazePos(w.HeadX, w.HeadY);
                p.OwnedCells = w.OwnedCells;
            }
        }
    }
}
