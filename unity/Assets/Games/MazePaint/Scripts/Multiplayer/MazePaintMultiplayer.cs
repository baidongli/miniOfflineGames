using System;
using System.Collections.Generic;
using MiniGames.Games.MazePaint.Logic;

namespace MiniGames.Games.MazePaint.Multiplayer
{
    /// <summary>
    /// Host-authoritative Maze Paint. Same pattern as Snakes' orchestrator
    /// but at a lower tick rate (~8Hz) where prediction is less critical.
    /// Client just applies snapshots; local input is also applied locally
    /// for instant turn feel.
    /// </summary>
    public sealed class MazePaintMultiplayer
    {
        public readonly bool IsHost;
        public readonly int LocalPlayerIndex;
        public readonly MazePaintGameState State;

        private readonly Dictionary<int, MazeDir> _pendingInputs = new Dictionary<int, MazeDir>();
        private MazeDir _localPendingHeading;
        private bool _hasLocalPending;

        public event Action<MazeSnapshot> SnapshotProduced;
        public event Action<MazeInputCmd> InputProduced;

        public MazePaintMultiplayer(bool isHost, int localPlayerIndex,
            int boardSize, int playerCount)
        {
            IsHost = isHost;
            LocalPlayerIndex = localPlayerIndex;
            State = new MazePaintGameState(boardSize, playerCount);
        }

        public void LocalInput(MazeDir d)
        {
            if (LocalPlayerIndex < 0 || LocalPlayerIndex >= State.Players.Count) return;
            var p = State.Players[LocalPlayerIndex];
            if (!p.IsAlive || d == p.Heading.Opposite()) return;

            if (IsHost)
            {
                _pendingInputs[LocalPlayerIndex] = d;
            }
            else
            {
                _localPendingHeading = d;
                _hasLocalPending = true;
                State.SetInput(LocalPlayerIndex, d);
                InputProduced?.Invoke(new MazeInputCmd
                {
                    PlayerIndex = LocalPlayerIndex,
                    ClientTick = State.Tick,
                    NewDirection = (byte)d
                });
            }
        }

        public void OnRemoteInput(MazeInputCmd cmd)
        {
            if (!IsHost) return;
            _pendingInputs[cmd.PlayerIndex] = (MazeDir)cmd.NewDirection;
        }

        public MazeStepResult HostTick()
        {
            if (!IsHost) throw new InvalidOperationException("HostTick on client");
            foreach (var kv in _pendingInputs) State.SetInput(kv.Key, kv.Value);
            _pendingInputs.Clear();
            var r = MazePaintEngine.Step(State);
            SnapshotProduced?.Invoke(MazePaintSerialization.Encode(State));
            return r;
        }

        public void OnSnapshot(MazeSnapshot snap)
        {
            if (IsHost) return;
            MazePaintSerialization.ApplyTo(snap, State);
            if (_hasLocalPending &&
                LocalPlayerIndex >= 0 && LocalPlayerIndex < State.Players.Count)
            {
                var p = State.Players[LocalPlayerIndex];
                if (p.IsAlive && _localPendingHeading != p.Heading.Opposite())
                    p.PendingHeading = _localPendingHeading;
            }
        }
    }
}
