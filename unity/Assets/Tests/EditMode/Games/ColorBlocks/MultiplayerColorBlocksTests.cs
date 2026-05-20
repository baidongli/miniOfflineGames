using System.Collections.Generic;
using MiniGames.Games.ColorBlocks.Logic;
using MiniGames.Games.ColorBlocks.Multiplayer;
using NUnit.Framework;

namespace MiniGames.Tests.Games.ColorBlocks
{
    public class MultiplayerColorBlocksTests
    {
        [Test]
        public void Single_line_clear_does_not_attack()
        {
            var mp = new MultiplayerColorBlocks("p1", seed: 1);
            int attacks = 0;
            mp.AttackOutgoing += _ => attacks++;

            // Fill row 0 except last cell, then drop a dot to clear it.
            for (int x = 0; x < 9; x++) mp.Local.Board.Set(x, 0, 1);
            mp.Local.Hand[0] = new PieceShape("dot", 5, new Cell(0, 0));
            Assert.IsTrue(mp.Local.TryPlay(0, 9, 0, out var r));
            Assert.AreEqual(1, r.TotalLinesCleared);
            Assert.AreEqual(0, attacks, "single-line clears should not attack");
        }

        [Test]
        public void Two_line_clear_emits_one_junk_row()
        {
            var mp = new MultiplayerColorBlocks("p1", seed: 1);
            AttackMessage attack = null;
            mp.AttackOutgoing += a => attack = a;

            // Make row 0 and col 0 both completable by a single dot at (0,0).
            for (int x = 1; x < 10; x++) mp.Local.Board.Set(x, 0, 1);
            for (int y = 1; y < 10; y++) mp.Local.Board.Set(0, y, 1);
            mp.Local.Hand[0] = new PieceShape("dot", 5, new Cell(0, 0));
            Assert.IsTrue(mp.Local.TryPlay(0, 0, 0, out var r));
            Assert.AreEqual(2, r.TotalLinesCleared);

            Assert.IsNotNull(attack, "attack message should fire");
            Assert.AreEqual(1, attack.JunkRows);
            Assert.AreEqual("p1", attack.FromPlayerId);
        }

        [Test]
        public void Received_attack_pushes_junk_rows_into_local_board()
        {
            var mp = new MultiplayerColorBlocks("victim", seed: 1);
            // Mark one cell so we can verify it shifts up by N.
            mp.Local.Board.Set(5, 0, 7);

            mp.OnAttackReceived(new AttackMessage
            {
                FromPlayerId = "attacker",
                JunkRows = 2,
                Seed = 42
            });

            Assert.AreEqual(7, mp.Local.Board.Get(5, 2),
                "cell should have shifted up by JunkRows");
        }

        [Test]
        public void Own_attacks_are_ignored_when_echoed_back()
        {
            var mp = new MultiplayerColorBlocks("self", seed: 1);
            mp.Local.Board.Set(0, 0, 9);
            mp.OnAttackReceived(new AttackMessage
            {
                FromPlayerId = "self",
                JunkRows = 3,
                Seed = 1
            });
            Assert.AreEqual(9, mp.Local.Board.Get(0, 0), "self-attack should be a no-op");
        }

        [Test]
        public void Opponent_progress_is_tracked()
        {
            var mp = new MultiplayerColorBlocks("p1", seed: 1);
            mp.OnProgressReceived(new ProgressMessage { PlayerId = "p2", Score = 42, CellsFilled = 17 });
            Assert.IsTrue(mp.Opponents.ContainsKey("p2"));
            Assert.AreEqual(42, mp.Opponents["p2"].Score);
            Assert.AreEqual(17, mp.Opponents["p2"].CellsFilled);
            Assert.IsFalse(mp.Opponents["p2"].IsDone);
        }

        [Test]
        public void Opponent_diedout_marks_done()
        {
            var mp = new MultiplayerColorBlocks("p1", seed: 1);
            mp.OnDiedOutReceived(new DiedOutMessage { PlayerId = "p2", FinalScore = 100 });
            Assert.IsTrue(mp.Opponents["p2"].IsDone);
            Assert.AreEqual(100, mp.Opponents["p2"].Score);
        }
    }

    public class BoardStateJunkTests
    {
        [Test]
        public void Push_one_row_shifts_existing_cells_up()
        {
            var b = new BoardState();
            b.Set(3, 0, 5);
            b.PushJunkRows(1, junkColor: 9, rng: new System.Random(0));
            Assert.AreEqual(5, b.Get(3, 1));
        }

        [Test]
        public void Junk_rows_are_full_except_one_gap()
        {
            var b = new BoardState();
            b.PushJunkRows(1, junkColor: 9, rng: new System.Random(123));
            int gaps = 0;
            for (int x = 0; x < BoardState.Size; x++)
                if (b.Get(x, 0) == 0) gaps++;
            Assert.AreEqual(1, gaps);
        }

        [Test]
        public void Overflow_reported_when_cells_pushed_off_top()
        {
            var b = new BoardState();
            // Mark a cell in the top row that will be pushed out.
            b.Set(0, BoardState.Size - 1, 7);
            bool overflow = b.PushJunkRows(1, junkColor: 9, rng: new System.Random(0));
            Assert.IsTrue(overflow);
        }
    }
}
