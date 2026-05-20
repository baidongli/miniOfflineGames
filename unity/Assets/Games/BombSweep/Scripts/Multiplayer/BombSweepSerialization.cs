using MiniGames.Games.BombSweep.Logic;

namespace MiniGames.Games.BombSweep.Multiplayer
{
    public static class BombSweepSerialization
    {
        public static BSSnapshot Encode(BombSweepGameState s)
        {
            int n = s.Board.Width * s.Board.Height;
            var snap = new BSSnapshot
            {
                Tick = s.Tick,
                Width = s.Board.Width,
                Height = s.Board.Height,
                Cells = new byte[n]
            };
            for (int y = 0; y < s.Board.Height; y++)
                for (int x = 0; x < s.Board.Width; x++)
                    snap.Cells[y * s.Board.Width + x] = (byte)s.Board.Get(x, y);

            foreach (var p in s.Players)
                snap.Players.Add(new BSPlayerWire
                {
                    Index = p.Index,
                    X = p.Pos.X, Y = p.Pos.Y,
                    Heading = (byte)p.Heading,
                    MaxBombs = (byte)p.MaxBombs,
                    CurrentBombs = (byte)p.CurrentBombs,
                    Range = (byte)p.Range,
                    SpeedTicksPerCell = (byte)p.SpeedTicksPerCell,
                    IsAlive = p.IsAlive
                });
            foreach (var b in s.Bombs)
                snap.Bombs.Add(new BSBombWire
                {
                    X = b.Pos.X, Y = b.Pos.Y,
                    OwnerIndex = b.OwnerIndex,
                    Range = (byte)b.Range,
                    TicksUntilExplode = (short)b.TicksUntilExplode
                });
            foreach (var e in s.Explosions)
            {
                var w = new BSExplosionWire { TicksUntilFade = (byte)e.TicksUntilFade };
                foreach (var c in e.Cells) { w.CellsFlat.Add(c.X); w.CellsFlat.Add(c.Y); }
                snap.Explosions.Add(w);
            }
            return snap;
        }

        public static void ApplyTo(BSSnapshot snap, BombSweepGameState s)
        {
            s.Tick = snap.Tick;
            for (int y = 0; y < snap.Height; y++)
                for (int x = 0; x < snap.Width; x++)
                    s.Board.Set(x, y, (CellType)snap.Cells[y * snap.Width + x]);

            for (int i = 0; i < snap.Players.Count && i < s.Players.Count; i++)
            {
                var w = snap.Players[i];
                var p = s.Players[i];
                p.Pos = new BombPos(w.X, w.Y);
                p.Heading = (BombDir)w.Heading;
                p.PendingHeading = p.Heading;
                p.MaxBombs = w.MaxBombs;
                p.CurrentBombs = w.CurrentBombs;
                p.Range = w.Range;
                p.SpeedTicksPerCell = w.SpeedTicksPerCell;
                p.IsAlive = w.IsAlive;
            }

            s.Bombs.Clear();
            foreach (var bw in snap.Bombs)
                s.Bombs.Add(new Bomb
                {
                    Pos = new BombPos(bw.X, bw.Y),
                    OwnerIndex = bw.OwnerIndex,
                    Range = bw.Range,
                    TicksUntilExplode = bw.TicksUntilExplode
                });

            s.Explosions.Clear();
            foreach (var ew in snap.Explosions)
            {
                var e = new Explosion
                {
                    Cells = new System.Collections.Generic.List<BombPos>(),
                    TicksUntilFade = ew.TicksUntilFade
                };
                for (int k = 0; k + 1 < ew.CellsFlat.Count; k += 2)
                    e.Cells.Add(new BombPos(ew.CellsFlat[k], ew.CellsFlat[k + 1]));
                s.Explosions.Add(e);
            }
        }
    }
}
