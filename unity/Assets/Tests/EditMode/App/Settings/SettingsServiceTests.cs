using System.Collections.Generic;
using MiniGames.App.Shared.Settings;
using MiniGames.GameModule;
using NUnit.Framework;

namespace MiniGames.Tests.App.Settings
{
    public class SettingsServiceTests
    {
        private sealed class InMemorySaveStore : ISaveStore
        {
            private readonly Dictionary<string, object> _data = new Dictionary<string, object>();
            public bool TryLoad<T>(string key, out T value) where T : class
            {
                if (_data.TryGetValue(key, out var raw)) { value = (T)raw; return true; }
                value = null; return false;
            }
            public void Save<T>(string key, T value) where T : class { _data[key] = value; }
            public void Delete(string key) { _data.Remove(key); }
        }

        [Test]
        public void Fresh_service_returns_default_settings()
        {
            var s = new SettingsService(new InMemorySaveStore());
            Assert.AreEqual(0.7f, s.Current.BgmVolume);
            Assert.AreEqual(1f, s.Current.SfxVolume);
            Assert.IsTrue(s.Current.HapticsEnabled);
        }

        [Test]
        public void Update_persists_and_fires_event()
        {
            var s = new SettingsService(new InMemorySaveStore());
            bool fired = false;
            s.Changed += _ => fired = true;
            s.Update(x => x.Muted = true);
            Assert.IsTrue(s.Current.Muted);
            Assert.IsTrue(fired);
        }

        [Test]
        public void Settings_persist_across_service_instances()
        {
            var store = new InMemorySaveStore();
            var s1 = new SettingsService(store);
            s1.Update(x => x.DisplayName = "Alice");

            var s2 = new SettingsService(store);
            Assert.AreEqual("Alice", s2.Current.DisplayName);
        }

        [Test]
        public void MarkTutorialSeen_idempotent()
        {
            var s = new SettingsService(new InMemorySaveStore());
            Assert.IsFalse(s.TutorialSeen("snakes"));
            s.MarkTutorialSeen("snakes");
            s.MarkTutorialSeen("snakes");
            Assert.IsTrue(s.TutorialSeen("snakes"));
            Assert.AreEqual(1, s.Current.TutorialsSeen.Count, "no duplicate entries");
        }

        [Test]
        public void Reset_restores_defaults_and_persists()
        {
            var store = new InMemorySaveStore();
            var s = new SettingsService(store);
            s.Update(x => { x.Muted = true; x.DisplayName = "Bob"; x.SfxVolume = 0.1f; });
            s.Reset();
            Assert.IsFalse(s.Current.Muted);
            Assert.AreEqual("", s.Current.DisplayName);
            Assert.AreEqual(1f, s.Current.SfxVolume);

            // Persisted: a fresh service sees the reset state, not the old.
            var s2 = new SettingsService(store);
            Assert.IsFalse(s2.Current.Muted);
        }
    }
}
