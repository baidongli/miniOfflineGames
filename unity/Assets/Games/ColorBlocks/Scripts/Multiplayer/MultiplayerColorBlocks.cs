using System;
using System.Collections.Generic;
using MiniGames.Games.ColorBlocks.Logic;

namespace MiniGames.Games.ColorBlocks.Multiplayer
{
    /// <summary>
    /// Wraps a local ColorBlocksGame for multiplayer. Hooks into the local
    /// game's events to:
    ///   - emit Attack when a move clears 2+ lines
    ///   - emit ProgressUpdate periodically for opponents' UI
    ///   - emit DiedOut on game over
    /// Receives the same events from peers and applies junk rows / tracks
    /// opponents' scores.
    ///
    /// Pure logic. The transport layer is provided via the IOutbox callback
    /// the caller passes in; testable with a fake.
    /// </summary>
    public sealed class MultiplayerColorBlocks
    {
        public readonly string LocalPlayerId;
        public readonly ColorBlocksGame Local;
        public readonly Dictionary<string, OpponentSnapshot> Opponents = new Dictionary<string, OpponentSnapshot>();

        public bool IsDoneLocal => Local.IsGameOver;

        public event Action<AttackMessage> AttackOutgoing;
        public event Action<ProgressMessage> ProgressOutgoing;
        public event Action<DiedOutMessage> DiedOutOutgoing;

        public MultiplayerColorBlocks(string localPlayerId, int seed)
        {
            LocalPlayerId = localPlayerId;
            Local = new ColorBlocksGame(seed);
            Local.Placed += OnLocalPlaced;
            Local.GameOver += OnLocalGameOver;
        }

        private void OnLocalPlaced(PlaceResult result, int scoreDelta)
        {
            int lines = result.TotalLinesCleared;
            if (lines >= 2)
            {
                AttackOutgoing?.Invoke(new AttackMessage
                {
                    FromPlayerId = LocalPlayerId,
                    JunkRows = lines - 1,
                    Seed = unchecked(Local.Score * 31 + Local.Board.Width * 7)
                });
            }

            ProgressOutgoing?.Invoke(new ProgressMessage
            {
                PlayerId = LocalPlayerId,
                Score = Local.Score,
                CellsFilled = CountFilled(Local.Board)
            });
        }

        private void OnLocalGameOver()
        {
            DiedOutOutgoing?.Invoke(new DiedOutMessage
            {
                PlayerId = LocalPlayerId,
                FinalScore = Local.Score
            });
        }

        public void OnAttackReceived(AttackMessage msg)
        {
            if (msg.FromPlayerId == LocalPlayerId) return;   // ignore own echoes
            if (Local.IsGameOver) return;
            var rng = new Random(msg.Seed);
            bool overflow = Local.Board.PushJunkRows(msg.JunkRows, junkColor: 8, rng);
            if (overflow)
            {
                // Treat overflow as instant loss for the player.
                ForceGameOver();
            }
        }

        public void OnProgressReceived(ProgressMessage msg)
        {
            if (msg.PlayerId == LocalPlayerId) return;
            if (!Opponents.TryGetValue(msg.PlayerId, out var snap))
                snap = new OpponentSnapshot { PlayerId = msg.PlayerId };
            snap.Score = msg.Score;
            snap.CellsFilled = msg.CellsFilled;
            Opponents[msg.PlayerId] = snap;
        }

        public void OnDiedOutReceived(DiedOutMessage msg)
        {
            if (msg.PlayerId == LocalPlayerId) return;
            if (!Opponents.TryGetValue(msg.PlayerId, out var snap))
                snap = new OpponentSnapshot { PlayerId = msg.PlayerId };
            snap.IsDone = true;
            snap.Score = msg.FinalScore;
            Opponents[msg.PlayerId] = snap;
        }

        private static int CountFilled(BoardState b)
        {
            int n = 0;
            for (int y = 0; y < b.Height; y++)
                for (int x = 0; x < b.Width; x++)
                    if (!b.IsEmpty(x, y)) n++;
            return n;
        }

        private void ForceGameOver()
        {
            // Slam the board to a known full state so HasAnyValidPlacement
            // returns false for everything; CheckGameOver runs on next play.
            // For now we just emit DiedOut; the engine state will catch up
            // when the player tries any further input.
            DiedOutOutgoing?.Invoke(new DiedOutMessage
            {
                PlayerId = LocalPlayerId,
                FinalScore = Local.Score
            });
        }
    }

    public struct OpponentSnapshot
    {
        public string PlayerId;
        public int Score;
        public int CellsFilled;
        public bool IsDone;
    }
}
