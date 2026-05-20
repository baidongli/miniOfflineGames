using System;
using System.Collections.Generic;
using MessagePack;
using MiniGames.Games.NumberMerge.Logic;
using MiniGames.Networking.Protocol;

namespace MiniGames.Games.NumberMerge.Multiplayer
{
    public enum NMMessageType : byte
    {
        Progress = (byte)MessageType.GameSpecificBase,        // 0x80
        DiedOut  = (byte)MessageType.GameSpecificBase + 1,    // 0x81
        ReachedGoal = (byte)MessageType.GameSpecificBase + 2, // 0x82
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public sealed class NMProgressMessage
    {
        public string PlayerId;
        public int Score;
        public byte MaxExponent;
        public int Swipes;
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public sealed class NMDiedOutMessage
    {
        public string PlayerId;
        public int FinalScore;
        public byte FinalMaxExponent;
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public sealed class NMReachedGoalMessage
    {
        public string PlayerId;
        public int Swipes;
    }

    /// <summary>
    /// Each peer runs their own game seeded identically; the tile-spawn
    /// sequence is deterministic so the race is fair. Players broadcast
    /// progress + game-over events for opponent UI / leaderboards.
    /// </summary>
    public sealed class MultiplayerNumberMerge
    {
        public readonly string LocalPlayerId;
        public readonly NumberMergeGame Local;
        public readonly Dictionary<string, OpponentView> Opponents = new Dictionary<string, OpponentView>();

        public event Action<NMProgressMessage> ProgressOutgoing;
        public event Action<NMDiedOutMessage> DiedOutOutgoing;
        public event Action<NMReachedGoalMessage> ReachedGoalOutgoing;

        public MultiplayerNumberMerge(string localPlayerId, int seed)
        {
            LocalPlayerId = localPlayerId;
            Local = new NumberMergeGame(seed);
            Local.Swiped += _ => Emit();
            Local.GameOver += () => DiedOutOutgoing?.Invoke(new NMDiedOutMessage
            {
                PlayerId = LocalPlayerId,
                FinalScore = Local.Score,
                FinalMaxExponent = Local.MaxExponent
            });
            Local.GoalReached += () => ReachedGoalOutgoing?.Invoke(new NMReachedGoalMessage
            {
                PlayerId = LocalPlayerId,
                Swipes = Local.SwipeCount
            });
        }

        private void Emit() => ProgressOutgoing?.Invoke(new NMProgressMessage
        {
            PlayerId = LocalPlayerId,
            Score = Local.Score,
            MaxExponent = Local.MaxExponent,
            Swipes = Local.SwipeCount
        });

        public void OnProgressReceived(NMProgressMessage m)
        {
            if (m.PlayerId == LocalPlayerId) return;
            var v = Opponents.TryGetValue(m.PlayerId, out var prev)
                ? prev : new OpponentView { PlayerId = m.PlayerId };
            v.Score = m.Score; v.MaxExponent = m.MaxExponent; v.Swipes = m.Swipes;
            Opponents[m.PlayerId] = v;
        }

        public void OnDiedOutReceived(NMDiedOutMessage m)
        {
            if (m.PlayerId == LocalPlayerId) return;
            var v = Opponents.TryGetValue(m.PlayerId, out var prev)
                ? prev : new OpponentView { PlayerId = m.PlayerId };
            v.Score = m.FinalScore; v.MaxExponent = m.FinalMaxExponent; v.IsDone = true;
            Opponents[m.PlayerId] = v;
        }

        public void OnReachedGoalReceived(NMReachedGoalMessage m)
        {
            if (m.PlayerId == LocalPlayerId) return;
            var v = Opponents.TryGetValue(m.PlayerId, out var prev)
                ? prev : new OpponentView { PlayerId = m.PlayerId };
            v.ReachedGoalAtSwipe = m.Swipes;
            Opponents[m.PlayerId] = v;
        }
    }

    public struct OpponentView
    {
        public string PlayerId;
        public int Score;
        public byte MaxExponent;
        public int Swipes;
        public int? ReachedGoalAtSwipe;
        public bool IsDone;
    }
}
