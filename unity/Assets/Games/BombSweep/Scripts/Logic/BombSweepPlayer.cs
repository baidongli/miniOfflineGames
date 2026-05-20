namespace MiniGames.Games.BombSweep.Logic
{
    public sealed class BombSweepPlayer
    {
        public readonly int Index;
        public BombPos Pos;
        public BombDir Heading;             // current movement direction (None = stopped)
        public BombDir PendingHeading;      // last input
        public int MaxBombs = 1;
        public int CurrentBombs = 0;
        public int Range = 2;
        public int SpeedTicksPerCell = 4;   // smaller = faster
        public int MoveAccumulator = 0;     // counts ticks toward next cell move
        public bool IsAlive = true;
        public bool BombRequested;          // set true by input; consumed in engine.Step

        public BombSweepPlayer(int index, BombPos spawn)
        {
            Index = index;
            Pos = spawn;
        }
    }

    public sealed class Bomb
    {
        public BombPos Pos;
        public int OwnerIndex;
        public int Range;
        public int TicksUntilExplode;
    }

    public sealed class Explosion
    {
        public System.Collections.Generic.List<BombPos> Cells;
        public int TicksUntilFade;
    }
}
