using System;
using System.Collections.Generic;

namespace MiniGames.Games.DotsAndBoxes.Logic
{
    public struct DotsMoveResult
    {
        public bool Accepted;
        public EdgeId Edge;
        public int Player;
        public List<(int bx, int by)> BoxesClaimed;
        public bool TurnPasses;
        public bool GameOver;
    }

    /// <summary>
    /// 2-4 player Dots and Boxes session. On each move:
    ///  - Draw the chosen edge if it's not already drawn.
    ///  - For each adjacent box, check if it was just completed (4 edges).
    ///    Each completed box is awarded to the current player.
    ///  - If at least one box was completed, the same player moves again.
    ///    Otherwise the turn passes to the next player.
    /// </summary>
    public sealed class DotsGame
    {
        public readonly DotsBoard Board;
        public readonly int PlayerCount;
        public int CurrentPlayer { get; private set; }
        public bool IsGameOver { get; private set; }

        public event Action<DotsMoveResult> Moved;
        public event Action GameOver;

        public DotsGame(int playerCount = 2, int boxWidth = DotsBoard.DefaultBoxes, int boxHeight = DotsBoard.DefaultBoxes)
        {
            if (playerCount < 2 || playerCount > 4) throw new ArgumentOutOfRangeException(nameof(playerCount));
            PlayerCount = playerCount;
            Board = new DotsBoard(boxWidth, boxHeight);
            CurrentPlayer = 0;
        }

        public bool TryPlay(EdgeId edge, out DotsMoveResult result)
        {
            result = default;
            if (IsGameOver) return false;
            if (!Board.IsEdgeInBounds(edge)) return false;
            if (Board.IsEdgeDrawn(edge)) return false;

            Board.DrawEdge(edge);

            // Identify boxes adjacent to this edge.
            var adjacent = new List<(int bx, int by)>();
            if (edge.Kind == EdgeKind.Horizontal)
            {
                // y is the row line: bottom of box (x, y), top of box (x, y-1).
                if (edge.Y < Board.BoxHeight) adjacent.Add((edge.X, edge.Y));
                if (edge.Y > 0) adjacent.Add((edge.X, edge.Y - 1));
            }
            else
            {
                if (edge.X < Board.BoxWidth) adjacent.Add((edge.X, edge.Y));
                if (edge.X > 0) adjacent.Add((edge.X - 1, edge.Y));
            }

            var claimed = new List<(int, int)>();
            foreach (var (bx, by) in adjacent)
            {
                if (Board.BoxOwner(bx, by) >= 0) continue;
                if (Board.BoxEdgeCount(bx, by) == 4)
                {
                    Board.SetBoxOwner(bx, by, CurrentPlayer);
                    claimed.Add((bx, by));
                }
            }

            result = new DotsMoveResult
            {
                Accepted = true,
                Edge = edge,
                Player = CurrentPlayer,
                BoxesClaimed = claimed,
                TurnPasses = claimed.Count == 0
            };

            if (Board.BoxesRemaining() == 0)
            {
                IsGameOver = true;
                result.GameOver = true;
                Moved?.Invoke(result);
                GameOver?.Invoke();
                return true;
            }

            if (claimed.Count == 0)
                CurrentPlayer = (CurrentPlayer + 1) % PlayerCount;

            Moved?.Invoke(result);
            return true;
        }

        public int[] FinalScores()
        {
            var s = new int[PlayerCount];
            for (int p = 0; p < PlayerCount; p++) s[p] = Board.CountOwned(p);
            return s;
        }

        /// <summary>Returns -1 on tie, otherwise the index of the leading player.</summary>
        public int WinnerOrDraw()
        {
            var scores = FinalScores();
            int best = scores[0]; int winner = 0; bool tie = false;
            for (int i = 1; i < scores.Length; i++)
            {
                if (scores[i] > best) { best = scores[i]; winner = i; tie = false; }
                else if (scores[i] == best) tie = true;
            }
            return tie ? -1 : winner;
        }
    }
}
