using System;

namespace MiniGames.App.Shared.Energy
{
    /// <summary>
    /// Soft-currency timer: energy refills one unit every RefillInterval,
    /// capped at Max. Once at cap, the timer stops accruing (LastUpdate
    /// snaps to "now" so it doesn't bank surplus). All math is pure; no
    /// Unity dependency.
    ///
    /// Time source is injected as a DateTimeOffset; callers normally pass
    /// DateTimeOffset.UtcNow. Tests can pass arbitrary times.
    /// </summary>
    public sealed class EnergyTimer
    {
        public int Max { get; }
        public TimeSpan RefillInterval { get; }
        public EnergyState State { get; private set; }

        public EnergyTimer(int max, TimeSpan refillInterval, EnergyState initial)
        {
            if (max <= 0) throw new ArgumentOutOfRangeException(nameof(max));
            if (refillInterval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(refillInterval));
            Max = max;
            RefillInterval = refillInterval;
            State = initial ?? EnergyState.Fresh(max);
        }

        public int Current(DateTimeOffset now)
        {
            Advance(now);
            return State.Energy;
        }

        public bool TrySpend(DateTimeOffset now, int amount)
        {
            if (amount <= 0) return true;
            Advance(now);
            if (State.Energy < amount) return false;
            State.Energy -= amount;
            // If we were at cap before spending, restart the refill clock from now.
            // If we were already below cap, preserve the existing partial accrual
            // (LastUpdate stays at the last completed-tick boundary).
            return true;
        }

        public void Grant(DateTimeOffset now, int amount)
        {
            if (amount <= 0) return;
            Advance(now);
            State.Energy = Math.Min(Max, State.Energy + amount);
            if (State.Energy >= Max)
                State.LastUpdateUtcTicks = now.UtcTicks;
        }

        public TimeSpan TimeUntilNextRefill(DateTimeOffset now)
        {
            if (State.Energy >= Max) return TimeSpan.Zero;
            Advance(now);
            if (State.Energy >= Max) return TimeSpan.Zero;
            var elapsed = now - new DateTimeOffset(State.LastUpdateUtcTicks, TimeSpan.Zero);
            var sinceTick = TimeSpan.FromTicks(elapsed.Ticks % RefillInterval.Ticks);
            return RefillInterval - sinceTick;
        }

        public TimeSpan TimeUntilFull(DateTimeOffset now)
        {
            Advance(now);
            int deficit = Max - State.Energy;
            if (deficit <= 0) return TimeSpan.Zero;
            return TimeUntilNextRefill(now) + TimeSpan.FromTicks(RefillInterval.Ticks * (deficit - 1));
        }

        private void Advance(DateTimeOffset now)
        {
            if (State.Energy >= Max)
            {
                State.LastUpdateUtcTicks = now.UtcTicks;
                return;
            }
            var last = new DateTimeOffset(State.LastUpdateUtcTicks, TimeSpan.Zero);
            var elapsed = now - last;
            if (elapsed <= TimeSpan.Zero) return;
            long ticksPerRefill = RefillInterval.Ticks;
            long completed = elapsed.Ticks / ticksPerRefill;
            if (completed <= 0) return;
            int gained = (int)Math.Min(completed, Max - State.Energy);
            State.Energy += gained;
            // Advance LastUpdate by the consumed ticks, preserving the partial accrual.
            State.LastUpdateUtcTicks += gained * ticksPerRefill;
            if (State.Energy >= Max) State.LastUpdateUtcTicks = now.UtcTicks;
        }
    }
}
