using System;
using System.Collections.Generic;

namespace MiniGames.Games.Reversi.Logic
{
    public enum ReversiResult : byte
    {
        InProgress = 0,
        BlackWins = 1,
        WhiteWins = 2,
        Draw = 3
    }

    public struct ReversiMoveResult
    {
        public bool Accepted;
        public bool WasPass;
        public int X, Y;
        public byte Player;
        public List<(int x, int y)> Flipped;
        public ReversiResult ResultAfter;
    }

    /// <summary>
    /// 2-player Reversi session. Black moves first. If the current player has
    /// no legal move, the turn passes; if neither side can move, the game ends.
    /// </summary>
    public sealed class ReversiGame
    {
        public readonly ReversiBoard Board = new ReversiBoard();
        public byte CurrentPlayer { get; private set; } = ReversiBoard.Black;
        public ReversiResult Result { get; private set; } = ReversiResult.InProgress;
        public int ConsecutivePasses { get; private set; }

        public event Action<ReversiMoveResult> Moved;

        public bool IsGameOver => Result != ReversiResult.InProgress;

        /// <summary>Attempt a placement. Returns false on illegal moves; the turn does not advance.</summary>
        public bool TryPlay(int x, int y, out ReversiMoveResult result)
        {
            result = default;
            if (IsGameOver) return false;
            var flips = Board.FlipsFor(x, y, CurrentPlayer);
            if (flips.Count == 0) return false;

            Board.Set(x, y, CurrentPlayer);
            foreach (var (fx, fy) in flips) Board.Set(fx, fy, CurrentPlayer);

            result = new ReversiMoveResult
            {
                Accepted = true,
                X = x, Y = y,
                Player = CurrentPlayer,
                Flipped = flips
            };
            ConsecutivePasses = 0;
            AdvanceTurnOrEnd(ref result);
            Moved?.Invoke(result);
            return true;
        }

        /// <summary>The current player explicitly passes (only valid when no legal move exists).</summary>
        public bool Pass(out ReversiMoveResult result)
        {
            result = default;
            if (IsGameOver) return false;
            if (Board.HasAnyLegalMove(CurrentPlayer)) return false;  // can't pass when moves exist

            result = new ReversiMoveResult
            {
                Accepted = true, WasPass = true, Player = CurrentPlayer
            };
            ConsecutivePasses++;
            AdvanceTurnOrEnd(ref result);
            Moved?.Invoke(result);
            return true;
        }

        public void ConcedeFrom(byte player)
        {
            if (IsGameOver) return;
            Result = player == ReversiBoard.Black ? ReversiResult.WhiteWins : ReversiResult.BlackWins;
            Moved?.Invoke(new ReversiMoveResult { Player = player, ResultAfter = Result });
        }

        private void AdvanceTurnOrEnd(ref ReversiMoveResult result)
        {
            // Board full or both sides have no moves -> game over.
            if (Board.IsFull() || ConsecutivePasses >= 2)
            {
                FinalizeResult();
                result.ResultAfter = Result;
                return;
            }

            byte other = ReversiBoard.Opposite(CurrentPlayer);
            if (Board.HasAnyLegalMove(other))
            {
                CurrentPlayer = other;
                ConsecutivePasses = 0;
            }
            else if (!Board.HasAnyLegalMove(CurrentPlayer))
            {
                // Neither side can move -> game over now.
                FinalizeResult();
            }
            // Else: same player keeps moving because opponent has no moves.
            result.ResultAfter = Result;
        }

        private void FinalizeResult()
        {
            int b = Board.Count(ReversiBoard.Black);
            int w = Board.Count(ReversiBoard.White);
            Result = b > w ? ReversiResult.BlackWins
                   : w > b ? ReversiResult.WhiteWins
                           : ReversiResult.Draw;
        }
    }
}
