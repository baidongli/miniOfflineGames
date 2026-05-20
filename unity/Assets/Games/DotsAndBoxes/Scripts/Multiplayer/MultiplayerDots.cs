using System;
using MiniGames.Games.DotsAndBoxes.Logic;
using MiniGames.Networking.Protocol;

namespace MiniGames.Games.DotsAndBoxes.Multiplayer
{
    public enum DBMessageType : byte
    {
        Move    = (byte)MessageType.GameSpecificBase,        // 0x80
        Resign  = (byte)MessageType.GameSpecificBase + 1,    // 0x81
        Rematch = (byte)MessageType.GameSpecificBase + 2,    // 0x82
    }
    public sealed class DBMoveMessage
    {
        public string PlayerId;
        public byte Kind;     // 0 horizontal, 1 vertical
        public int X, Y;
        public int MoveNumber;
    }
    public sealed class DBResignMessage
    {
        public string PlayerId;
    }
    public sealed class DBRematchMessage
    {
        public string PlayerId;
    }

    /// <summary>
    /// Turn-based orchestrator with one twist: Dots and Boxes lets the
    /// current player keep moving when they complete a box. The wire
    /// protocol stays simple - each Move message just contains the edge.
    /// The receiver applies it; whose turn it is afterward is fully
    /// determined by the engine.
    ///
    /// Multi-player (3-4) seat assignment is deterministic by sorting all
    /// player ids alphabetically; seat 0 is the lex-smallest id.
    /// </summary>
    public sealed class MultiplayerDots
    {
        public readonly DotsGame Game;
        public readonly string LocalPlayerId;
        public readonly string[] PlayerIds;       // ordered to match seats
        public readonly int LocalSeat;
        public int RemoteMoveCount { get; private set; }
        public int LocalMoveCount { get; private set; }

        public bool IsLocalTurn => Game.CurrentPlayer == LocalSeat && !Game.IsGameOver;

        public event Action<DBMoveMessage> MoveOutgoing;
        public event Action<DBResignMessage> ResignOutgoing;
        public event Action<DBRematchMessage> RematchOutgoing;
        public event Action<DotsMoveResult> MoveApplied;
        public event Action GameEnded;

        public MultiplayerDots(string localPlayerId, string[] allPlayerIds,
            int boxWidth = DotsBoard.DefaultBoxes, int boxHeight = DotsBoard.DefaultBoxes)
        {
            LocalPlayerId = localPlayerId;
            PlayerIds = (string[])allPlayerIds.Clone();
            Array.Sort(PlayerIds, StringComparer.Ordinal);
            LocalSeat = Array.IndexOf(PlayerIds, localPlayerId);
            if (LocalSeat < 0) LocalSeat = 0;
            Game = new DotsGame(PlayerIds.Length, boxWidth, boxHeight);
            Game.Moved += r =>
            {
                MoveApplied?.Invoke(r);
                if (r.GameOver) GameEnded?.Invoke();
            };
        }

        public bool TryLocalPlay(EdgeId edge)
        {
            if (!IsLocalTurn) return false;
            if (!Game.TryPlay(edge, out _)) return false;
            LocalMoveCount++;
            MoveOutgoing?.Invoke(new DBMoveMessage
            {
                PlayerId = LocalPlayerId,
                Kind = (byte)edge.Kind,
                X = edge.X, Y = edge.Y,
                MoveNumber = LocalMoveCount
            });
            return true;
        }

        public void OnMoveReceived(DBMoveMessage m)
        {
            if (m.PlayerId == LocalPlayerId) return;
            if (Game.IsGameOver) return;
            // Find the seat for the sender.
            int senderSeat = Array.IndexOf(PlayerIds, m.PlayerId);
            if (senderSeat < 0 || senderSeat != Game.CurrentPlayer) return;
            if (m.MoveNumber != RemoteMoveCount + 1) return;

            var edge = new EdgeId((EdgeKind)m.Kind, m.X, m.Y);
            if (Game.TryPlay(edge, out _)) RemoteMoveCount++;
        }

        public void Resign()
        {
            if (Game.IsGameOver) return;
            ResignOutgoing?.Invoke(new DBResignMessage { PlayerId = LocalPlayerId });
        }

        public void OnResignReceived(DBResignMessage m)
        {
            // For Dots and Boxes, resigning just removes the player from
            // future participation; for simplicity we just let the game
            // continue and reflect resigned state at UI layer.
        }

        public void RequestRematch()
            => RematchOutgoing?.Invoke(new DBRematchMessage { PlayerId = LocalPlayerId });

        public void OnRematchReceived(DBRematchMessage _)
        {
        }
    }
}
