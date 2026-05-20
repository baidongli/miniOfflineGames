using MiniGames.GameModule.Random;
using NUnit.Framework;

namespace MiniGames.Tests.GameModule.Random
{
    public class RngTests
    {
        [Test]
        public void SystemRng_with_same_seed_produces_same_sequence()
        {
            var a = new SystemRng(42);
            var b = new SystemRng(42);
            for (int i = 0; i < 50; i++) Assert.AreEqual(a.NextInt(100), b.NextInt(100));
        }

        [Test]
        public void FixedSequenceRng_returns_values_in_order_and_cycles()
        {
            var r = new FixedSequenceRng(3, 1, 4, 1, 5);
            Assert.AreEqual(3, r.NextInt());
            Assert.AreEqual(1, r.NextInt());
            Assert.AreEqual(4, r.NextInt());
            Assert.AreEqual(1, r.NextInt());
            Assert.AreEqual(5, r.NextInt());
            Assert.AreEqual(3, r.NextInt());  // cycled
        }

        [Test]
        public void FixedSequenceRng_NextInt_maps_into_range_without_throwing()
        {
            var r = new FixedSequenceRng(-7, 100, 0);
            Assert.GreaterOrEqual(r.NextInt(5), 0);
            Assert.Less(r.NextInt(5), 5);
            Assert.AreEqual(0, r.NextInt(5));
        }

        [Test]
        public void RngFactory_FromSeed_returns_SystemRng_like_behavior()
        {
            var a = RngFactory.FromSeed(7);
            var b = RngFactory.FromSeed(7);
            Assert.AreEqual(a.NextInt(1000), b.NextInt(1000));
        }
    }
}
