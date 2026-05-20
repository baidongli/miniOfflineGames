using System;

namespace MiniGames.GameModule.Tick
{
    /// <summary>
    /// Drives a fixed-rate action. Production uses UnityTicker (MonoBehaviour
    /// + Time.deltaTime). Tests use VirtualTicker and call Advance() to step.
    ///
    /// One subscriber per ticker. The owner stops the ticker when done.
    /// </summary>
    public interface ITicker
    {
        bool IsRunning { get; }
        float Hz { get; }
        void Begin(float hz, Action onTick);
        void Stop();
    }
}
