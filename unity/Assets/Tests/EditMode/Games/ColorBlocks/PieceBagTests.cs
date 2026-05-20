using MiniGames.Games.ColorBlocks.Logic;
using NUnit.Framework;

namespace MiniGames.Tests.Games.ColorBlocks
{
    public class PieceBagTests
    {
        [Test]
        public void Same_seed_produces_same_sequence()
        {
            var a = new PieceBag(seed: 12345);
            var b = new PieceBag(seed: 12345);
            for (int i = 0; i < 50; i++)
                Assert.AreSame(a.Next(), b.Next(),
                    $"diverged at step {i}");
        }

        [Test]
        public void Different_seeds_diverge_within_first_few_steps()
        {
            var a = new PieceBag(seed: 1);
            var b = new PieceBag(seed: 2);
            bool anyDiff = false;
            for (int i = 0; i < 10 && !anyDiff; i++)
                if (!ReferenceEquals(a.Next(), b.Next())) anyDiff = true;
            Assert.IsTrue(anyDiff, "seeds 1 and 2 produced identical first 10 shapes");
        }

        [Test]
        public void NextHand_returns_requested_count()
        {
            var bag = new PieceBag(seed: 0);
            var hand = bag.NextHand(3);
            Assert.AreEqual(3, hand.Length);
            foreach (var s in hand) Assert.IsNotNull(s);
        }
    }
}
