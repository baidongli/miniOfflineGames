using System;
using MiniGames.Games.Reversi.Logic;

namespace MiniGames.Games.Reversi.Multiplayer
{
    /// <summary>
    /// Turn-based orchestrator (same pattern as Connect Four).
    /// Seat assignment by lex-order of PlayerId: smaller id = Black (first).
    /// </summary>
    public sealed class MultiplayerReversi
    {
        public readonly ReversiGame Game;
        public readonly string LocalPlayerId;
        public readonly string RemotePlayerId;
        public readonly byte LocalSeat;
        public readonly byte RemoteSeat;
        public int LocalMoveCount { get; private set; }
        public int RemoteMoveCount { get; private set; }

        public bool IsLocalTurn => Game.CurrentPlayer == LocalSeat && !Game.IsGameOver;
        public bool OpponentResigned { get; private set; }
        public bool RematchRequestedByOpponent { get; private set; }

        public event Action<RVMoveMessage> MoveOutgoing;
        public event Action<RVPassMessage> PassOutgoing;
        public event Action<RVResignMessage> ResignOutgoing;
        public event Action<RVRematchMessage> RematchOutgoing;
        public event Action<ReversiMoveResult> MoveApplied;
        public event Action GameEnded;

        public MultiplayerReversi(string localPlayerId, string remotePlayerId)
        {
            LocalPlayerId = localPlayerId;
            RemotePlayerId = remotePlayerId;
            bool localIsBlack = string.CompareOrdinal(localPlayerId, remotePlayerId) < 0;
            LocalSeat = localIsBlack ? ReversiBoard.Black : ReversiBoard.White;
            RemoteSeat = localIsBlack ? ReversiBoard.White : ReversiBoard.Black;
            Game = new ReversiGame();
            Game.Moved += r =>
            {
                MoveApplied?.Invoke(r);
                if (r.ResultAfter != ReversiResult.InProgress) GameEnded?.Invoke();
            };
        }

        public bool TryLocalPlay(int x, int y)
        {
            if (!IsLocalTurn) return false;
            if (!Game.TryPlay(x, y, out _)) return false;
            LocalMoveCount++;
            MoveOutgoing?.Invoke(new RVMoveMessage
            {
                PlayerId = LocalPlayerId, X = x, Y = y, MoveNumber = LocalMoveCount
            });
            return true;
        }

        public bool TryLocalPass()
        {
            if (!IsLocalTurn) return false;
            if (!Game.Pass(out _)) return false;
            LocalMoveCount++;
            PassOutgoing?.Invoke(new RVPassMessage
            {
                PlayerId = LocalPlayerId, MoveNumber = LocalMoveCount
            });
            return true;
        }

        public void OnMoveReceived(RVMoveMessage m)
        {
            if (m.PlayerId == LocalPlayerId) return;
            if (Game.IsGameOver) return;
            if (Game.CurrentPlayer != RemoteSeat) return;
            if (m.MoveNumber != RemoteMoveCount + 1) return;
            if (Game.TryPlay(m.X, m.Y, out _)) RemoteMoveCount++;
        }

        public void OnPassReceived(RVPassMessage m)
        {
            if (m.PlayerId == LocalPlayerId) return;
            if (Game.IsGameOver) return;
            if (Game.CurrentPlayer != RemoteSeat) return;
            if (m.MoveNumber != RemoteMoveCount + 1) return;
            if (Game.Pass(out _)) RemoteMoveCount++;
        }

        public void Resign()
        {
            if (Game.IsGameOver) return;
            ResignOutgoing?.Invoke(new RVResignMessage { PlayerId = LocalPlayerId });
            Game.ConcedeFrom(LocalSeat);
        }

        public void OnResignReceived(RVResignMessage m)
        {
            if (m.PlayerId == LocalPlayerId) return;
            if (Game.IsGameOver) return;
            OpponentResigned = true;
            Game.ConcedeFrom(RemoteSeat);
        }

        public void RequestRematch()
            => RematchOutgoing?.Invoke(new RVRematchMessage { PlayerId = LocalPlayerId });

        public void OnRematchReceived(RVRematchMessage m)
        {
            if (m.PlayerId == LocalPlayerId) return;
            RematchRequestedByOpponent = true;
        }
    }
}
