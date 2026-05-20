using System;

namespace MiniGames.App.Shared.Settings
{
    /// <summary>Persistent user preferences. JsonUtility-serializable.</summary>
    [Serializable]
    public sealed class AppSettings
    {
        // Audio
        public float BgmVolume = 0.7f;
        public float SfxVolume = 1.0f;
        public bool Muted = false;

        // Haptics
        public bool HapticsEnabled = true;

        // Profile
        public string DisplayName = "";
        public int ColorIndex = 0;        // 0..7 deterministic palette index

        // UX preferences
        public string PreferredLanguage = "en";   // "en", "zh", ...
        public bool ReducedMotion = false;        // accessibility: skip flashy animations
        public bool LeftHandedControls = false;

        // Per-game seen-tutorial flags. Game id -> true once tutorial completed.
        // Using a flat array for JsonUtility compatibility (it can't serialize dictionaries).
        public System.Collections.Generic.List<string> TutorialsSeen
            = new System.Collections.Generic.List<string>();

        public AppSettings Clone()
        {
            var copy = new AppSettings
            {
                BgmVolume = BgmVolume,
                SfxVolume = SfxVolume,
                Muted = Muted,
                HapticsEnabled = HapticsEnabled,
                DisplayName = DisplayName,
                ColorIndex = ColorIndex,
                PreferredLanguage = PreferredLanguage,
                ReducedMotion = ReducedMotion,
                LeftHandedControls = LeftHandedControls,
            };
            copy.TutorialsSeen.AddRange(TutorialsSeen);
            return copy;
        }
    }
}
