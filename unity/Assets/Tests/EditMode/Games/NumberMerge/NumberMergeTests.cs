using MiniGames.Games.NumberMerge.AI;
using MiniGames.Games.NumberMerge.Logic;
using MiniGames.Games.NumberMerge.Multiplayer;
using NUnit.Framework;

namespace MiniGames.Tests.Games.NumberMerge
{
    public class NumberMergeEngineTests
    {
        [Test]
        public void CollapseLeft_merges_adjacent_equals_once()
        {
            byte[] row = { 1, 1, 2, 2 };  // 2 2 4 4 -> 4 8
            int score = NumberMergeEngine.CollapseLeft(row);
            Assert.AreEqual(2, row[0]);   // 4
            Assert.AreEqual(3, row[1]);   // 8
            Assert.AreEqual(0, row[2]);
            Assert.AreEqual(0, row[3]);
            Assert.AreEqual(4 + 8, score);
        }

        [Test]
        public void CollapseLeft_does_not_chain_three_equals()
        {
            byte[] row = { 1, 1, 1, 0 };  // 2 2 2 -> 4 2 (not 4 alone)
            NumberMergeEngine.CollapseLeft(row);
            Assert.AreEqual(2, row[0]);
            Assert.AreEqual(1, row[1]);
            Assert.AreEqual(0, row[2]);
        }

        [Test]
        public void Swipe_left_slides_and_merges()
        {
            var b = new NumberMergeBoard();
            b.Set(0, 0, 1); b.Set(2, 0, 1);  // [2, 0, 2, 0] in row 0
            var r = NumberMergeEngine.Swipe(b, SwipeDir.Left);
            Assert.IsTrue(r.AnyMoved);
            Assert.AreEqual(2, b.Get(0, 0));  // merged to 4
            Assert.AreEqual(0, b.Get(1, 0));
            Assert.AreEqual(0, b.Get(2, 0));
        }

        [Test]
        public void Swipe_with_no_changes_returns_AnyMoved_false()
        {
            var b = new NumberMergeBoard();
            // Actually-left-collapsed row: [2, 4, _, _]. Swiping left = no-op.
            b.Set(0, 0, 1); b.Set(1, 0, 2);
            var r = NumberMergeEngine.Swipe(b, SwipeDir.Left);
            Assert.IsFalse(r.AnyMoved);
        }

        [Test]
        public void HasAnyValidSwipe_false_when_full_and_no_merges()
        {
            var b = new NumberMergeBoard();
            byte v = 1;
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    b.Set(x, y, v++);  // 1..16, all unique -> no merges
            Assert.IsFalse(NumberMergeEngine.HasAnyValidSwipe(b));
        }

        [Test]
        public void HasAnyValidSwipe_true_when_empty_cell_exists()
        {
            var b = new NumberMergeBoard();
            Assert.IsTrue(NumberMergeEngine.HasAnyValidSwipe(b));
        }
    }

    public class NumberMergeGameTests
    {
        [Test]
        public void Fresh_game_has_two_tiles_and_score_zero()
        {
            var g = new NumberMergeGame(seed: 1);
            int filled = 0;
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    if (!g.Board.IsEmpty(x, y)) filled++;
            Assert.AreEqual(2, filled);
            Assert.AreEqual(0, g.Score);
            Assert.IsFalse(g.IsGameOver);
        }

        [Test]
        public void Same_seed_produces_same_initial_layout()
        {
            var a = new NumberMergeGame(seed: 42);
            var b = new NumberMergeGame(seed: 42);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    Assert.AreEqual(a.Board.Get(x, y), b.Board.Get(x, y));
        }
    }

    public class SimpleNumberMergeAITests
    {
        [Test]
        public void Cpu_can_play_a_long_game_without_immediate_lockup()
        {
            var g = new NumberMergeGame(seed: 1);
            var cpu = new CpuNumberMergeController(g, new SimpleNumberMergeAI());
            int turns = 0;
            while (!g.IsGameOver && turns < 500 && cpu.TakeTurn()) turns++;
            Assert.Greater(turns, 30, "CPU should manage at least 30 swipes");
        }
    }

    public class MultiplayerNumberMergeTests
    {
        [Test]
        public void Same_seed_produces_identical_initial_boards_across_peers()
        {
            var a = new MultiplayerNumberMerge("alice", seed: 7);
            var b = new MultiplayerNumberMerge("bob", seed: 7);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    Assert.AreEqual(a.Local.Board.Get(x, y), b.Local.Board.Get(x, y));
        }

        [Test]
        public void Progress_message_lands_in_opponent_view()
        {
            var mp = new MultiplayerNumberMerge("self", seed: 1);
            mp.OnProgressReceived(new NMProgressMessage
            {
                PlayerId = "rival", Score = 256, MaxExponent = 8, Swipes = 20
            });
            Assert.IsTrue(mp.Opponents.ContainsKey("rival"));
            Assert.AreEqual(256, mp.Opponents["rival"].Score);
        }
    }
}
