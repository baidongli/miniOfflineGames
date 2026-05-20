using System;
using MiniGames.Games.ConnectFour.Logic;

namespace MiniGames.Games.ConnectFour.Multiplayer
{
    /// <summary>
    /// Turn-based orchestrator. Each peer runs the same canonical game.
    /// LocalPlayerId vs RemotePlayerId determine who controls which color.
    /// PlayerA is whoever has the lexicographically smaller PlayerId
    /// (deterministic seat assignment with no extra handshake).
    /// </summary>
    public sealed class MultiplayerConnectFour
    {
        public readonly ConnectFourGame Game;
        public readonly string LocalPlayerId;
        public readonly string RemotePlayerId;
        public readonly byte LocalSeat;   // PlayerA (1) or PlayerB (2)
        public readonly byte RemoteSeat;

        public int LocalMoveCount { get; private set; }
        public int RemoteMoveCount { get; private set; }

        public bool IsLocalTurn => Game.CurrentPlayer == LocalSeat && !Game.IsGameOver;
        public bool OpponentResigned { get; private set; }
        public bool RematchRequestedByOpponent { get; private set; }

        public event Action<MoveMessage> MoveOutgoing;
        public event Action<ResignMessage> ResignOutgoing;
        public event Action<RematchMessage> RematchOutgoing;
        public event Action<MoveResult> MoveApplied;
        public event Action GameEnded;

        public MultiplayerConnectFour(string localPlayerId, string remotePlayerId,
            int width = 7, int height = 6, int winLength = 4)
        {
            LocalPlayerId = localPlayerId;
            RemotePlayerId = remotePlayerId;

            // Deterministic seat assignment so both peers agree without a separate handshake.
            bool localIsA = string.CompareOrdinal(localPlayerId, remotePlayerId) < 0;
            LocalSeat = localIsA ? ConnectFourBoard.PlayerA : ConnectFourBoard.PlayerB;
            RemoteSeat = localIsA ? ConnectFourBoard.PlayerB : ConnectFourBoard.PlayerA;

            Game = new ConnectFourGame(width, height, winLength);
            Game.Moved += r =>
            {
                MoveApplied?.Invoke(r);
                if (r.ResultAfter != GameResult.InProgress) GameEnded?.Invoke();
            };
        }

        /// <summary>Local player plays a column. Validated + broadcast.</summary>
        public bool TryLocalPlay(int column)
        {
            if (!IsLocalTurn) return false;
            if (!Game.TryPlay(column, out var move)) return false;
            LocalMoveCount++;
            MoveOutgoing?.Invoke(new MoveMessage
            {
                PlayerId = LocalPlayerId,
                Column = column,
                MoveNumber = LocalMoveCount
            });
            return true;
        }

        public void OnMoveReceived(MoveMessage msg)
        {
            if (msg.PlayerId == LocalPlayerId) return;  // ignore self-echo
            if (Game.IsGameOver) return;
            if (Game.CurrentPlayer != RemoteSeat) return;  // out of turn - ignore

            // De-dup: messages outside the expected sequence are dropped.
            if (msg.MoveNumber != RemoteMoveCount + 1) return;

            if (Game.TryPlay(msg.Column, out _))
                RemoteMoveCount++;
        }

        public void Resign()
        {
            if (Game.IsGameOver) return;
            ResignOutgoing?.Invoke(new ResignMessage { PlayerId = LocalPlayerId });
            Game.ConcedeFrom(LocalSeat);
        }

        public void OnResignReceived(ResignMessage msg)
        {
            if (msg.PlayerId == LocalPlayerId) return;
            if (Game.IsGameOver) return;
            OpponentResigned = true;
            Game.ConcedeFrom(RemoteSeat);
        }

        public void RequestRematch()
        {
            RematchOutgoing?.Invoke(new RematchMessage { PlayerId = LocalPlayerId });
        }

        public void OnRematchReceived(RematchMessage msg)
        {
            if (msg.PlayerId == LocalPlayerId) return;
            RematchRequestedByOpponent = true;
        }
    }
}
