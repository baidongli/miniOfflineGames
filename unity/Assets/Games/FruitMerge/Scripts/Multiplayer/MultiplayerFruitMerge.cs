using System;
using System.Collections.Generic;
using MiniGames.Games.FruitMerge.Logic;

namespace MiniGames.Games.FruitMerge.Multiplayer
{
    /// <summary>
    /// Each player runs their own FruitMergeGame, seeded identically so the
    /// NextFruit sequence matches. Score-race + last-alive (no overflow)
    /// determines winner. Drops are broadcast for opponents' UI rendering;
    /// no host authority is needed because each player's game is local.
    /// </summary>
    public sealed class MultiplayerFruitMerge
    {
        public readonly string LocalPlayerId;
        public readonly FruitMergeGame Local;
        public readonly Dictionary<string, OpponentView> Opponents = new Dictionary<string, OpponentView>();

        public event Action<DropMessage> DropOutgoing;
        public event Action<ProgressMessage> ProgressOutgoing;
        public event Action<DiedOutMessage> DiedOutOutgoing;

        public MultiplayerFruitMerge(string localPlayerId, int seed)
        {
            LocalPlayerId = localPlayerId;
            Local = new FruitMergeGame(seed);
            Local.Dropped += OnLocalDropped;
            Local.GameOver += OnLocalGameOver;
        }

        public bool TryDrop(int column)
        {
            byte tier = Local.NextFruit;
            bool ok = Local.TryDrop(column);
            if (ok)
            {
                DropOutgoing?.Invoke(new DropMessage
                {
                    PlayerId = LocalPlayerId,
                    Column = column,
                    Tier = tier,
                    Score = Local.Score,
                    HighestTier = Local.HighestTier
                });
            }
            return ok;
        }

        private void OnLocalDropped(DropResult r)
        {
            ProgressOutgoing?.Invoke(new ProgressMessage
            {
                PlayerId = LocalPlayerId,
                Score = Local.Score,
                HighestTier = Local.HighestTier,
                CellsFilled = Local.Grid.CountFruits()
            });
        }

        private void OnLocalGameOver()
        {
            DiedOutOutgoing?.Invoke(new DiedOutMessage
            {
                PlayerId = LocalPlayerId,
                FinalScore = Local.Score,
                HighestTier = Local.HighestTier
            });
        }

        public void OnDropReceived(DropMessage msg)
        {
            if (msg.PlayerId == LocalPlayerId) return;
            var v = GetOrCreate(msg.PlayerId);
            v.Score = msg.Score;
            v.HighestTier = msg.HighestTier;
            v.LastColumnDropped = msg.Column;
            v.LastTierDropped = msg.Tier;
            Opponents[msg.PlayerId] = v;
        }

        public void OnProgressReceived(ProgressMessage msg)
        {
            if (msg.PlayerId == LocalPlayerId) return;
            var v = GetOrCreate(msg.PlayerId);
            v.Score = msg.Score;
            v.HighestTier = msg.HighestTier;
            v.CellsFilled = msg.CellsFilled;
            Opponents[msg.PlayerId] = v;
        }

        public void OnDiedOutReceived(DiedOutMessage msg)
        {
            if (msg.PlayerId == LocalPlayerId) return;
            var v = GetOrCreate(msg.PlayerId);
            v.Score = msg.FinalScore;
            v.HighestTier = msg.HighestTier;
            v.IsDone = true;
            Opponents[msg.PlayerId] = v;
        }

        private OpponentView GetOrCreate(string id)
            => Opponents.TryGetValue(id, out var v) ? v : new OpponentView { PlayerId = id };
    }

    public struct OpponentView
    {
        public string PlayerId;
        public int Score;
        public int HighestTier;
        public int CellsFilled;
        public int LastColumnDropped;
        public byte LastTierDropped;
        public bool IsDone;
    }
}
