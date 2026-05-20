using System;
using MiniGames.App.Shared.Energy;
using NUnit.Framework;

namespace MiniGames.Tests.App.Energy
{
    public class EnergyTimerTests
    {
        private static DateTimeOffset T0 = new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
        private static readonly TimeSpan FiveMin = TimeSpan.FromMinutes(5);

        private static EnergyTimer Fresh(int starting, int max = 5)
            => new EnergyTimer(max, FiveMin, new EnergyState
            {
                Energy = starting,
                LastUpdateUtcTicks = T0.UtcTicks
            });

        [Test]
        public void Refills_one_unit_per_interval()
        {
            var e = Fresh(starting: 0);
            Assert.AreEqual(0, e.Current(T0));
            Assert.AreEqual(0, e.Current(T0 + TimeSpan.FromMinutes(4)));
            Assert.AreEqual(1, e.Current(T0 + TimeSpan.FromMinutes(5)));
            Assert.AreEqual(2, e.Current(T0 + TimeSpan.FromMinutes(10)));
        }

        [Test]
        public void Caps_at_max()
        {
            var e = Fresh(starting: 0, max: 5);
            Assert.AreEqual(5, e.Current(T0 + TimeSpan.FromHours(10)));
        }

        [Test]
        public void At_cap_clock_does_not_bank_surplus()
        {
            // Start at cap, let an hour pass, then spend one.
            var e = Fresh(starting: 5, max: 5);
            var later = T0 + TimeSpan.FromHours(1);
            Assert.AreEqual(5, e.Current(later));
            Assert.IsTrue(e.TrySpend(later, 1));
            Assert.AreEqual(4, e.Current(later));
            // The next refill is one FULL interval from `later`, not from T0:
            // at 4:59 elapsed, still 4; at exactly 5:00 elapsed, back to 5.
            Assert.AreEqual(4, e.Current(later + TimeSpan.FromMinutes(4) + TimeSpan.FromSeconds(59)));
            Assert.AreEqual(5, e.Current(later + FiveMin));
        }

        [Test]
        public void TrySpend_returns_false_when_insufficient()
        {
            var e = Fresh(starting: 1);
            Assert.IsFalse(e.TrySpend(T0, 2));
            Assert.AreEqual(1, e.Current(T0));
        }

        [Test]
        public void TrySpend_decrements_and_keeps_partial_accrual()
        {
            var e = Fresh(starting: 0, max: 5);
            var t = T0 + TimeSpan.FromMinutes(7); // 1 full refill, 2 min remainder
            Assert.AreEqual(1, e.Current(t));
            Assert.IsTrue(e.TrySpend(t, 1));
            Assert.AreEqual(0, e.Current(t));
            // Three more minutes (5 total since the last "tick boundary"): next refill.
            Assert.AreEqual(1, e.Current(t + TimeSpan.FromMinutes(3)));
        }

        [Test]
        public void TimeUntilNextRefill_counts_down()
        {
            var e = Fresh(starting: 0);
            var t = T0 + TimeSpan.FromMinutes(2);
            Assert.AreEqual(TimeSpan.FromMinutes(3), e.TimeUntilNextRefill(t));
        }

        [Test]
        public void TimeUntilNextRefill_zero_when_full()
        {
            var e = Fresh(starting: 5);
            Assert.AreEqual(TimeSpan.Zero, e.TimeUntilNextRefill(T0));
        }

        [Test]
        public void Grant_adds_immediately_and_caps()
        {
            var e = Fresh(starting: 1, max: 5);
            e.Grant(T0, 3);
            Assert.AreEqual(4, e.Current(T0));
            e.Grant(T0, 100);
            Assert.AreEqual(5, e.Current(T0));
        }

        [Test]
        public void Construction_rejects_invalid_args()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnergyTimer(0, FiveMin, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnergyTimer(5, TimeSpan.Zero, null));
        }
    }
}
