using MiniGames.GameModule.Tick;
using NUnit.Framework;

namespace MiniGames.Tests.GameModule.Tick
{
    public class VirtualTickerTests
    {
        [Test]
        public void Advance_fires_OnTick_at_configured_rate()
        {
            var t = new VirtualTicker();
            int fired = 0;
            t.Begin(hz: 10f, () => fired++);

            // 0.25s @ 10Hz = 2 full ticks + 0.05 leftover.
            int n = t.Advance(0.25f);
            Assert.AreEqual(2, n);
            Assert.AreEqual(2, fired);
            Assert.AreEqual(2, t.TotalTicksFired);

            // Another 0.05s -> total accumulated 0.10s -> one more tick.
            n = t.Advance(0.05f);
            Assert.AreEqual(1, n);
            Assert.AreEqual(3, fired);
        }

        [Test]
        public void AdvanceTicks_fires_n_times_directly()
        {
            var t = new VirtualTicker();
            int fired = 0;
            t.Begin(hz: 1f, () => fired++);
            t.AdvanceTicks(7);
            Assert.AreEqual(7, fired);
            Assert.AreEqual(7, t.TotalTicksFired);
        }

        [Test]
        public void Stop_inside_OnTick_halts_further_ticks_this_call()
        {
            var t = new VirtualTicker();
            int fired = 0;
            VirtualTicker captured = null;
            captured = t;
            t.Begin(hz: 100f, () =>
            {
                fired++;
                if (fired == 2) captured.Stop();
            });
            // 5 ticks worth of time, but Stop happens at tick 2.
            t.Advance(0.05f);
            Assert.AreEqual(2, fired);
            Assert.IsFalse(t.IsRunning);
        }

        [Test]
        public void Start_with_zero_hz_throws()
        {
            var t = new VirtualTicker();
            Assert.Throws<System.ArgumentOutOfRangeException>(() => t.Begin(0f, () => { }));
        }
    }
}
