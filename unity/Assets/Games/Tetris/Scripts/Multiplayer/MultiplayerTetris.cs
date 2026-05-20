using System;
using System.Collections.Generic;
using MiniGames.Games.Tetris.Logic;

namespace MiniGames.Games.Tetris.Multiplayer
{
    /// <summary>
    /// Wraps a local TetrisGame for multiplayer.
    ///  - When local lock awards attack lines, broadcast TetrisAttackMessage.
    ///  - Periodically broadcast progress (score, lines, height) for opponent UI.
    ///  - On local game over, broadcast TetrisDiedOutMessage.
    ///  - On incoming Attack, push junk rows into the local board.
    /// </summary>
    public sealed class MultiplayerTetris
    {
        public readonly string LocalPlayerId;
        public readonly TetrisGame Local;
        public readonly Dictionary<string, OpponentView> Opponents = new Dictionary<string, OpponentView>();

        public event Action<TetrisAttackMessage> AttackOutgoing;
        public event Action<TetrisProgressMessage> ProgressOutgoing;
        public event Action<TetrisDiedOutMessage> DiedOutOutgoing;

        public MultiplayerTetris(string localPlayerId, int seed)
        {
            LocalPlayerId = localPlayerId;
            Local = new TetrisGame(seed);
            Local.Locked += OnLocked;
            Local.GameOver += OnLocalGameOver;
        }

        private void OnLocked(LockResult r)
        {
            if (r.AttackLines > 0)
            {
                AttackOutgoing?.Invoke(new TetrisAttackMessage
                {
                    FromPlayerId = LocalPlayerId,
                    JunkRows = r.AttackLines,
                    Seed = unchecked(Local.Score * 31 + Local.Lines * 7)
                });
            }
            ProgressOutgoing?.Invoke(new TetrisProgressMessage
            {
                PlayerId = LocalPlayerId,
                Score = Local.Score,
                Lines = Local.Lines,
                Level = Local.Level,
                Height = TallestColumn(Local.Board)
            });
        }

        private void OnLocalGameOver()
        {
            DiedOutOutgoing?.Invoke(new TetrisDiedOutMessage
            {
                PlayerId = LocalPlayerId,
                FinalScore = Local.Score,
                FinalLines = Local.Lines
            });
        }

        public void OnAttackReceived(TetrisAttackMessage msg)
        {
            if (msg.FromPlayerId == LocalPlayerId) return;
            if (Local.IsGameOver) return;
            Local.ReceiveJunk(msg.JunkRows, junkColor: 8, rngSeed: msg.Seed);
        }

        public void OnProgressReceived(TetrisProgressMessage msg)
        {
            if (msg.PlayerId == LocalPlayerId) return;
            var v = Opponents.TryGetValue(msg.PlayerId, out var prev) ? prev : new OpponentView { PlayerId = msg.PlayerId };
            v.Score = msg.Score;
            v.Lines = msg.Lines;
            v.Level = msg.Level;
            v.Height = msg.Height;
            Opponents[msg.PlayerId] = v;
        }

        public void OnDiedOutReceived(TetrisDiedOutMessage msg)
        {
            if (msg.PlayerId == LocalPlayerId) return;
            var v = Opponents.TryGetValue(msg.PlayerId, out var prev) ? prev : new OpponentView { PlayerId = msg.PlayerId };
            v.Score = msg.FinalScore;
            v.Lines = msg.FinalLines;
            v.IsDone = true;
            Opponents[msg.PlayerId] = v;
        }

        private static int TallestColumn(TetrisBoard board)
        {
            int max = 0;
            for (int x = 0; x < TetrisBoard.Width; x++)
            {
                for (int y = TetrisBoard.TotalHeight - 1; y >= 0; y--)
                {
                    if (!board.IsEmpty(x, y)) { if (y + 1 > max) max = y + 1; break; }
                }
            }
            return max;
        }
    }

    public struct OpponentView
    {
        public string PlayerId;
        public int Score;
        public int Lines;
        public int Level;
        public int Height;
        public bool IsDone;
    }
}
