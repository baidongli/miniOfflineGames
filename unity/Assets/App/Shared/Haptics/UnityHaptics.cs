using MiniGames.GameModule;
using UnityEngine;

namespace MiniGames.App.Shared.Haptics
{
    /// <summary>
    /// Cross-platform haptics. Android: Handheld.Vibrate (deprecated but
    /// still works; replace with a vibrator-plugin if precise patterns are
    /// needed). iOS: UIImpactFeedbackGenerator via a native plugin (TBD;
    /// falls back to Handheld.Vibrate for now).
    /// </summary>
    public sealed class UnityHaptics : IHaptics
    {
        public void Light() => Trigger(15);
        public void Medium() => Trigger(35);
        public void Heavy() => Trigger(70);

        private static void Trigger(int durationMs)
        {
#if UNITY_ANDROID || UNITY_IOS
            // TODO: replace with platform-specific generators for finer control.
            Handheld.Vibrate();
#else
            _ = durationMs;
#endif
        }
    }

    /// <summary>No-op haptics for editor / desktop builds.</summary>
    public sealed class NullHaptics : IHaptics
    {
        public void Light() { } public void Medium() { } public void Heavy() { }
    }
}
