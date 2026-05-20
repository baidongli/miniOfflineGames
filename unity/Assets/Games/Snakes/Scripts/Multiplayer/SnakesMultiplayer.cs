using System;
using System.Collections.Generic;
using MiniGames.Games.Snakes.Logic;

namespace MiniGames.Games.Snakes.Multiplayer
{
    /// <summary>
    /// Host-authoritative Snakes orchestrator with simple client prediction.
    ///
    /// Host:
    ///   - Owns the canonical SnakesGameState.
    ///   - Each fixed tick: applies any inputs received since last tick,
    ///     calls SnakesEngine.Step, emits a Snapshot.
    ///
    /// Client:
    ///   - Owns a *predicted* SnakesGameState seeded the same way as the host.
    ///   - Applies local input immediately (so the snake turns now, not on
    ///     round-trip), and sends the InputCmd to host.
    ///   - When a Snapshot arrives, overwrites local state with host truth.
    ///     For snake i = self, last-known input is re-applied as the pending
    ///     heading so prediction doesn't immediately overwrite the user's
    ///     intent.
    ///
    /// No-Unity. Tickers / wire transports are injected.
    /// </summary>
    public sealed class SnakesMultiplayer
    {
        public readonly bool IsHost;
        public readonly int LocalPlayerIndex;
        public readonly SnakesGameState State;

        // Host-only: inputs queued for the next tick.
        private readonly Dictionary<int, Direction> _pendingInputs = new Dictionary<int, Direction>();

        // Client-only: remember our last requested direction for re-prediction
        // after a snapshot snap.
        private Direction _localPendingHeading;
        private bool _hasLocalPending;

        public event Action<SnakeSnapshot> SnapshotProduced;       // host -> wire
        public event Action<SnakeInputCmd> InputProduced;          // client -> wire

        public SnakesMultiplayer(bool isHost, int localPlayerIndex,
            int boardWidth, int boardHeight, int playerCount, int seed)
        {
            IsHost = isHost;
            LocalPlayerIndex = localPlayerIndex;
            State = new SnakesGameState(boardWidth, boardHeight, playerCount, seed);
        }

        /// <summary>Local player wants to change direction. Predict locally + ship to host.</summary>
        public void LocalInput(Direction d)
        {
            // Reject U-turn at the source for tighter feel.
            if (LocalPlayerIndex >= 0 && LocalPlayerIndex < State.Snakes.Count)
            {
                var s = State.Snakes[LocalPlayerIndex];
                if (d == s.Heading.Opposite()) return;
            }

            if (IsHost)
            {
                _pendingInputs[LocalPlayerIndex] = d;
            }
            else
            {
                _localPendingHeading = d;
                _hasLocalPending = true;
                // Apply locally for predictive feel.
                State.SetInput(LocalPlayerIndex, d);
                InputProduced?.Invoke(new SnakeInputCmd
                {
                    PlayerIndex = LocalPlayerIndex,
                    ClientTick = State.Tick,
                    NewDirection = (byte)d
                });
            }
        }

        /// <summary>Host: process a client's input message.</summary>
        public void OnRemoteInput(SnakeInputCmd cmd)
        {
            if (!IsHost) return;
            _pendingInputs[cmd.PlayerIndex] = (Direction)cmd.NewDirection;
        }

        /// <summary>Host: advance one fixed tick and emit snapshot.</summary>
        public StepResult HostTick()
        {
            if (!IsHost) throw new InvalidOperationException("HostTick called on client");
            foreach (var kv in _pendingInputs) State.SetInput(kv.Key, kv.Value);
            _pendingInputs.Clear();
            var result = SnakesEngine.Step(State);
            SnapshotProduced?.Invoke(SnakesSerialization.Encode(State));
            return result;
        }

        /// <summary>Client: apply a snapshot from host as new ground truth, then re-apply local intent.</summary>
        public void OnSnapshot(SnakeSnapshot snap)
        {
            if (IsHost) return;
            SnakesSerialization.ApplyTo(snap, State);
            // Re-apply user's most recent intent so the snake keeps the turn
            // the local player just requested, even if it hasn't reached host yet.
            if (_hasLocalPending &&
                LocalPlayerIndex >= 0 && LocalPlayerIndex < State.Snakes.Count)
            {
                var s = State.Snakes[LocalPlayerIndex];
                if (s.IsAlive && _localPendingHeading != s.Heading.Opposite())
                    s.PendingHeading = _localPendingHeading;
            }
        }
    }
}
