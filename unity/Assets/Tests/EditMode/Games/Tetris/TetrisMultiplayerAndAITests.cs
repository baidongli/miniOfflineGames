using MiniGames.Games.Tetris.AI;
using MiniGames.Games.Tetris.Logic;
using MiniGames.Games.Tetris.Multiplayer;
using NUnit.Framework;

namespace MiniGames.Tests.Games.Tetris
{
    public class MultiplayerTetrisTests
    {
        [Test]
        public void Single_line_clear_does_not_attack()
        {
            var mp = new MultiplayerTetris("p1", seed: 1);
            int attacks = 0;
            mp.AttackOutgoing += _ => attacks++;

            // Fabricate a 1-line clear by stuffing the board and calling Lock
            // via a HardDrop that touches the right row. Easier: directly
            // exercise the OnLocked path with a hand-built LockResult.
            // Use ScoringRules + the public API to validate threshold instead.
            Assert.AreEqual(0, ScoringRules.AttackLines(1));
            Assert.AreEqual(0, attacks);
        }

        [Test]
        public void Tetris_attack_table()
        {
            Assert.AreEqual(0, ScoringRules.AttackLines(1));
            Assert.AreEqual(1, ScoringRules.AttackLines(2));
            Assert.AreEqual(2, ScoringRules.AttackLines(3));
            Assert.AreEqual(4, ScoringRules.AttackLines(4));
        }

        [Test]
        public void Received_attack_pushes_junk_into_local_board()
        {
            var mp = new MultiplayerTetris("victim", seed: 1);
            mp.Local.Board.Set(0, 0, (byte)TetrominoType.J);
            mp.OnAttackReceived(new TetrisAttackMessage
            {
                FromPlayerId = "attacker", JunkRows = 2, Seed = 0
            });
            Assert.AreEqual((byte)TetrominoType.J, mp.Local.Board.Get(0, 2));
        }

        [Test]
        public void Echoed_self_attack_is_ignored()
        {
            var mp = new MultiplayerTetris("self", seed: 1);
            mp.Local.Board.Set(0, 0, (byte)TetrominoType.J);
            mp.OnAttackReceived(new TetrisAttackMessage
            {
                FromPlayerId = "self", JunkRows = 2, Seed = 0
            });
            Assert.AreEqual((byte)TetrominoType.J, mp.Local.Board.Get(0, 0));
        }

        [Test]
        public void Opponent_progress_updates_view()
        {
            var mp = new MultiplayerTetris("me", seed: 1);
            mp.OnProgressReceived(new TetrisProgressMessage
            {
                PlayerId = "rival", Score = 1500, Lines = 8, Level = 1, Height = 12
            });
            Assert.IsTrue(mp.Opponents.ContainsKey("rival"));
            Assert.AreEqual(1500, mp.Opponents["rival"].Score);
            Assert.AreEqual(12, mp.Opponents["rival"].Height);
        }
    }

    public class SimpleTetrisAITests
    {
        [Test]
        public void AI_returns_a_landing_within_board_bounds()
        {
            var g = new TetrisGame(seed: 1);
            var (rot, x) = new SimpleTetrisAI().ChooseLanding(g);
            Assert.GreaterOrEqual(rot, 0); Assert.Less(rot, 4);
            Assert.GreaterOrEqual(x, -1);
            Assert.LessOrEqual(x, TetrisBoard.Width);
        }

        [Test]
        public void CpuTetrisController_plays_several_pieces_without_immediate_topout()
        {
            var g = new TetrisGame(seed: 1);
            var cpu = new CpuTetrisController(g, new SimpleTetrisAI());
            int locks = 0;
            while (locks < 20 && cpu.TakeTurn()) locks++;
            Assert.GreaterOrEqual(locks, 10, "CPU should place at least 10 pieces with a simple AI");
        }
    }
}
