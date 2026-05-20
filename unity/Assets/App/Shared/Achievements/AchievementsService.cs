using System;
using System.Collections.Generic;
using MiniGames.GameModule;

namespace MiniGames.App.Shared.Achievements
{
    /// <summary>
    /// Achievement tracking.
    ///
    /// Games declare AchievementDef[] at startup (via Register). At runtime,
    /// game-side code calls Unlock("ach_id") when their conditions trigger.
    /// The service de-duplicates and persists.
    ///
    /// Each unlock fires UnlockedEvent so the UI can show a toast.
    /// </summary>
    public sealed class AchievementsService
    {
        private const string Key = "achievements";

        private readonly ISaveStore _store;
        private readonly Func<DateTimeOffset> _now;
        private readonly Dictionary<string, AchievementDef> _defs = new Dictionary<string, AchievementDef>();
        private AchievementsPayload _state;

        public event Action<AchievementDef> UnlockedEvent;

        public AchievementsService(ISaveStore store, Func<DateTimeOffset> now = null)
        {
            _store = store;
            _now = now ?? (() => DateTimeOffset.UtcNow);
        }

        public void Register(IEnumerable<AchievementDef> defs)
        {
            foreach (var d in defs)
            {
                if (d == null || string.IsNullOrEmpty(d.Id)) continue;
                _defs[d.Id] = d;
            }
        }

        /// <summary>Records an achievement as unlocked. Idempotent (re-unlock is a no-op).</summary>
        public bool Unlock(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId)) return false;
            if (!_defs.TryGetValue(achievementId, out var def)) return false;
            EnsureLoaded();
            foreach (var u in _state.Unlocked)
                if (u.Id == achievementId) return false;

            _state.Unlocked.Add(new UnlockedAchievement
            {
                Id = achievementId,
                UnlockedAtUtcTicks = _now().UtcTicks
            });
            _store.Save(Key, _state);
            UnlockedEvent?.Invoke(def);
            return true;
        }

        public bool IsUnlocked(string achievementId)
        {
            EnsureLoaded();
            foreach (var u in _state.Unlocked) if (u.Id == achievementId) return true;
            return false;
        }

        public IReadOnlyCollection<AchievementDef> AllDefs => _defs.Values;

        public IReadOnlyList<AchievementDef> DefsFor(string gameId)
        {
            var list = new List<AchievementDef>();
            foreach (var d in _defs.Values)
                if (d.GameId == gameId) list.Add(d);
            return list;
        }

        public IReadOnlyList<UnlockedAchievement> AllUnlocked
        {
            get { EnsureLoaded(); return _state.Unlocked; }
        }

        public void ResetAll()
        {
            _state = new AchievementsPayload();
            _store.Delete(Key);
        }

        private void EnsureLoaded()
        {
            if (_state != null) return;
            if (!_store.TryLoad<AchievementsPayload>(Key, out _state) || _state == null)
                _state = new AchievementsPayload();
        }
    }
}
