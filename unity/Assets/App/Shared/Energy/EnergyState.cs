using System;

namespace MiniGames.App.Shared.Energy
{
    /// <summary>
    /// Persisted snapshot of energy. Stored to ISaveStore; advanced in
    /// memory by EnergyTimer.
    /// </summary>
    [Serializable]
    public sealed class EnergyState
    {
        public int Energy;
        public long LastUpdateUtcTicks;

        public static EnergyState Fresh(int starting) => new EnergyState
        {
            Energy = starting,
            LastUpdateUtcTicks = DateTimeOffset.UtcNow.UtcTicks
        };
    }
}
