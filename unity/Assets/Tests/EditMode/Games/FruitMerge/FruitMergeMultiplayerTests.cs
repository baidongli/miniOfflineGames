using MiniGames.Games.FruitMerge.Multiplayer;
using NUnit.Framework;

namespace MiniGames.Tests.Games.FruitMerge
{
    public class FruitMergeMultiplayerTests
    {
        [Test]
        public void Local_drop_emits_DropMessage_and_ProgressMessage()
        {
            var mp = new MultiplayerFruitMerge("p1", seed: 1);
            DropMessage drop = null;
            ProgressMessage prog = null;
            mp.DropOutgoing += d => drop = d;
            mp.ProgressOutgoing += p => prog = p;

            Assert.IsTrue(mp.TryDrop(0));

            Assert.IsNotNull(drop);
            Assert.AreEqual("p1", drop.PlayerId);
            Assert.AreEqual(0, drop.Column);
            Assert.IsNotNull(prog);
            Assert.AreEqual("p1", prog.PlayerId);
        }

        [Test]
        public void Opponent_drop_updates_view()
        {
            var mp = new MultiplayerFruitMerge("self", seed: 1);
            mp.OnDropReceived(new DropMessage
            {
                PlayerId = "other", Column = 3, Tier = 2, Score = 7, HighestTier = 2
            });
            Assert.IsTrue(mp.Opponents.ContainsKey("other"));
            Assert.AreEqual(3, mp.Opponents["other"].LastColumnDropped);
            Assert.AreEqual(2, mp.Opponents["other"].LastTierDropped);
            Assert.AreEqual(7, mp.Opponents["other"].Score);
        }

        [Test]
        public void Echoed_drop_from_self_is_ignored()
        {
            var mp = new MultiplayerFruitMerge("self", seed: 1);
            mp.OnDropReceived(new DropMessage { PlayerId = "self", Column = 1, Tier = 1 });
            Assert.IsFalse(mp.Opponents.ContainsKey("self"));
        }

        [Test]
        public void Diedout_marks_opponent_done()
        {
            var mp = new MultiplayerFruitMerge("self", seed: 1);
            mp.OnDiedOutReceived(new DiedOutMessage
            {
                PlayerId = "other", FinalScore = 200, HighestTier = 5
            });
            Assert.IsTrue(mp.Opponents["other"].IsDone);
            Assert.AreEqual(200, mp.Opponents["other"].Score);
        }

        [Test]
        public void Same_seed_produces_same_NextFruit_for_both_players()
        {
            var a = new MultiplayerFruitMerge("a", seed: 99);
            var b = new MultiplayerFruitMerge("b", seed: 99);
            Assert.AreEqual(a.Local.NextFruit, b.Local.NextFruit);
        }
    }
}
