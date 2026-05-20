using System;

namespace MiniGames.Games.ConnectFour.Logic
{
    public enum GameResult : byte
    {
        InProgress = 0,
        PlayerAWins = 1,
        PlayerBWins = 2,
        Draw = 3
    }

    public struct MoveResult
    {
        public bool Accepted;
        public int Column;
        public int Row;
        public byte Player;
        public GameResult ResultAfter;
    }

    /// <summary>
    /// 2-player turn-based session. PlayerA always moves first. After each
    /// move, win/draw is checked; the local turn passes to the other player.
    /// </summary>
    public sealed class ConnectFourGame
    {
        public readonly ConnectFourBoard Board;
        public byte CurrentPlayer { get; private set; } = ConnectFourBoard.PlayerA;
        public GameResult Result { get; private set; } = GameResult.InProgress;

        public event Action<MoveResult> Moved;

        public ConnectFourGame(int width = 7, int height = 6, int winLength = 4)
        {
            Board = new ConnectFourBoard(width, height, winLength);
        }

        public bool IsGameOver => Result != GameResult.InProgress;

        /// <summary>End the game by conceding from `seat`; the other seat wins. Used for resignation / forfeits.</summary>
        public void ConcedeFrom(byte seat)
        {
            if (IsGameOver) return;
            Result = seat == ConnectFourBoard.PlayerA
                ? GameResult.PlayerBWins : GameResult.PlayerAWins;
            Moved?.Invoke(new MoveResult
            {
                Accepted = false,
                Player = seat,
                ResultAfter = Result
            });
        }

        public bool TryPlay(int column, out MoveResult result)
        {
            result = default;
            if (IsGameOver) return false;
            if (column < 0 || column >= Board.Width) return false;
            if (Board.IsColumnFull(column)) return false;

            int row = Board.Drop(column, CurrentPlayer);
            if (row < 0) return false;

            result = new MoveResult
            {
                Accepted = true,
                Column = column,
                Row = row,
                Player = CurrentPlayer
            };

            if (Board.IsWinAt(column, row))
            {
                Result = CurrentPlayer == ConnectFourBoard.PlayerA
                    ? GameResult.PlayerAWins : GameResult.PlayerBWins;
            }
            else if (Board.IsFull())
            {
                Result = GameResult.Draw;
            }
            else
            {
                CurrentPlayer = CurrentPlayer == ConnectFourBoard.PlayerA
                    ? ConnectFourBoard.PlayerB : ConnectFourBoard.PlayerA;
            }

            result.ResultAfter = Result;
            Moved?.Invoke(result);
            return true;
        }
    }
}
