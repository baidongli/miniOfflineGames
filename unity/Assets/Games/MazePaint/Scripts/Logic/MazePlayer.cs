using System.Collections.Generic;

namespace MiniGames.Games.MazePaint.Logic
{
    public sealed class MazePlayer
    {
        public readonly int Index;
        public MazePos Head;
        public MazeDir Heading;
        public MazeDir PendingHeading;
        public bool IsAlive = true;

        /// <summary>Cells laid since leaving owned territory, in chronological order. Empty when standing on home.</summary>
        public readonly List<MazePos> ActiveTrail = new List<MazePos>();

        public int OwnedCells; // updated by engine after each capture for quick UI access

        public MazePlayer(int index, MazePos head, MazeDir heading)
        {
            Index = index;
            Head = head;
            Heading = heading;
            PendingHeading = heading;
        }
    }
}
