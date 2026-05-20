using System.Collections.Generic;

namespace MiniGames.Games.MazePaint.Logic
{
    public sealed class MazePaintGameState
    {
        public readonly MazeBoard Board;
        public readonly List<MazePlayer> Players;
        public int Tick;

        public MazePaintGameState(int boardSize, int playerCount)
        {
            Board = new MazeBoard(boardSize);
            Players = new List<MazePlayer>(playerCount);
            SpawnPlayers(playerCount);
        }

        public void SetInput(int playerIndex, MazeDir dir)
        {
            if (playerIndex < 0 || playerIndex >= Players.Count) return;
            var p = Players[playerIndex];
            if (!p.IsAlive) return;
            if (dir == p.Heading.Opposite()) return;
            p.PendingHeading = dir;
        }

        private void SpawnPlayers(int playerCount)
        {
            // 4 corners. Player 0=BL, 1=BR, 2=TL, 3=TR.
            int s = Board.Size;
            var spots = new (MazePos head, MazeDir dir, MazePos territoryOrigin)[]
            {
                (new MazePos(2, 2),       MazeDir.Right, new MazePos(0, 0)),
                (new MazePos(s - 3, 2),   MazeDir.Left,  new MazePos(s - 3, 0)),
                (new MazePos(2, s - 3),   MazeDir.Right, new MazePos(0, s - 3)),
                (new MazePos(s - 3, s - 3), MazeDir.Left, new MazePos(s - 3, s - 3))
            };
            for (int i = 0; i < playerCount && i < spots.Length; i++)
            {
                var p = new MazePlayer(i, spots[i].head, spots[i].dir);
                Players.Add(p);
                // Paint 3x3 starting territory.
                var origin = spots[i].territoryOrigin;
                for (int dy = 0; dy < 3; dy++)
                    for (int dx = 0; dx < 3; dx++)
                    {
                        int x = origin.X + dx, y = origin.Y + dy;
                        if (Board.InBounds(x, y)) Board.SetOwner(x, y, i);
                    }
                p.OwnedCells = Board.CountOwned(i);
            }
        }
    }
}
