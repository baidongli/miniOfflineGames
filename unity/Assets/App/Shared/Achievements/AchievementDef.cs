using System;
using System.Collections.Generic;

namespace MiniGames.App.Shared.Achievements
{
    /// <summary>Static description of an achievement. Read-only at runtime.</summary>
    public sealed class AchievementDef
    {
        public readonly string Id;
        public readonly string GameId;          // owning game id ("color_blocks") or null for global
        public readonly string TitleKey;        // l10n key, e.g. "ach.color_blocks.first_clear.title"
        public readonly string DescriptionKey;  // l10n key
        public readonly string IconRef;         // sprite resource path or addressable key
        public readonly bool IsHidden;          // hidden in UI until unlocked

        public AchievementDef(string id, string gameId, string titleKey, string descKey,
            string iconRef = null, bool isHidden = false)
        {
            Id = id;
            GameId = gameId;
            TitleKey = titleKey;
            DescriptionKey = descKey;
            IconRef = iconRef;
            IsHidden = isHidden;
        }
    }

    [Serializable]
    public sealed class UnlockedAchievement
    {
        public string Id;
        public long UnlockedAtUtcTicks;
    }

    [Serializable]
    public sealed class AchievementsPayload
    {
        public List<UnlockedAchievement> Unlocked = new List<UnlockedAchievement>();
    }
}
