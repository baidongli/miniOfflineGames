using System;
using System.Collections.Generic;
using MiniGames.Games.BombSweep.Logic;

namespace MiniGames.Games.BombSweep.Multiplayer
{
    /// <summary>
    /// Host-authoritative BombSweep. Same pattern as Snakes / Maze Paint.
    /// Host runs HostTick at ~8 Hz, accepts queued inputs from clients,
    /// broadcasts snapshots. Clients apply local input immediately for
    /// responsiveness and overwrite with host snapshots when they arrive.
    /// </summary>
    public sealed class MultiplayerBombSweep
    {
        public readonly bool IsHost;
        public readonly int LocalPlayerIndex;
        public readonly BombSweepGameState State;

        private readonly Dictionary<int, (BombDir heading, bool placeBomb)> _pendingInputs
            = new Dictionary<int, (BombDir, bool)>();

        public event Action<BSSnapshot> SnapshotProduced;
        public event Action<BSInputCmd> InputProduced;

        public MultiplayerBombSweep(bool isHost, int localPlayerIndex,
            int playerCount, int seed)
        {
            IsHost = isHost;
            LocalPlayerIndex = localPlayerIndex;
            State = new BombSweepGameState(playerCount, seed);
        }

        public void LocalInput(BombDir heading, bool placeBomb)
        {
            if (LocalPlayerIndex < 0 || LocalPlayerIndex >= State.Players.Count) return;
            if (!State.Players[LocalPlayerIndex].IsAlive) return;

            if (IsHost)
            {
                _pendingInputs[LocalPlayerIndex] = (heading, placeBomb);
            }
            else
            {
                // Predict locally.
                State.SetInput(LocalPlayerIndex, heading, placeBomb);
                InputProduced?.Invoke(new BSInputCmd
                {
                    PlayerIndex = LocalPlayerIndex,
                    ClientTick = State.Tick,
                    Heading = (byte)heading,
                    PlaceBomb = placeBomb
                });
            }
        }

        public void OnRemoteInput(BSInputCmd cmd)
        {
            if (!IsHost) return;
            _pendingInputs[cmd.PlayerIndex] = ((BombDir)cmd.Heading, cmd.PlaceBomb);
        }

        public BombSweepStepResult HostTick()
        {
            if (!IsHost) throw new InvalidOperationException("HostTick on client");
            foreach (var kv in _pendingInputs)
                State.SetInput(kv.Key, kv.Value.heading, kv.Value.placeBomb);
            _pendingInputs.Clear();
            var r = BombSweepEngine.Step(State);
            SnapshotProduced?.Invoke(BombSweepSerialization.Encode(State));
            return r;
        }

        public void OnSnapshot(BSSnapshot snap)
        {
            if (IsHost) return;
            BombSweepSerialization.ApplyTo(snap, State);
        }
    }
}
