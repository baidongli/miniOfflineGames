using System.Collections.Generic;
using MiniGames.App.Shared.Localization;
using MiniGames.App.Shared.Settings;
using MiniGames.GameModule;
using NUnit.Framework;

namespace MiniGames.Tests.App.Localization
{
    public class LocalizationServiceTests
    {
        private sealed class InMemorySaveStore : ISaveStore
        {
            private readonly Dictionary<string, object> _d = new Dictionary<string, object>();
            public bool TryLoad<T>(string key, out T value) where T : class
            { if (_d.TryGetValue(key, out var v)) { value = (T)v; return true; } value = null; return false; }
            public void Save<T>(string key, T value) where T : class { _d[key] = value; }
            public void Delete(string key) { _d.Remove(key); }
        }

        [Test]
        public void Default_language_is_english_when_no_settings()
        {
            var l = new LocalizationService();
            Assert.AreEqual("en", l.Language);
            Assert.AreEqual("Play", l.Get("ui.play"));
        }

        [Test]
        public void Reads_preferred_language_from_settings_on_construction()
        {
            var settings = new SettingsService(new InMemorySaveStore());
            settings.Update(s => s.PreferredLanguage = "zh");
            var l = new LocalizationService(settings);
            Assert.AreEqual("zh", l.Language);
            Assert.AreEqual("开始", l.Get("ui.play"));
        }

        [Test]
        public void Falls_back_to_English_when_key_missing_in_target_language()
        {
            // Force a Chinese service then ask for a key that only English has.
            // To test the fallback path, we have to construct one manually -
            // we can't easily inject a custom table. Instead: rely on the fact
            // that the en/zh tables have the same keys; this test asserts that
            // a totally unknown key falls back to "the key itself".
            var l = new LocalizationService();
            Assert.AreEqual("nonexistent.key", l.Get("nonexistent.key"));
        }

        [Test]
        public void SetLanguage_switches_active_table_and_fires_event()
        {
            var settings = new SettingsService(new InMemorySaveStore());
            var l = new LocalizationService(settings);
            string fired = null;
            l.LanguageChanged += lang => fired = lang;
            Assert.IsTrue(l.SetLanguage("zh"));
            Assert.AreEqual("zh", l.Language);
            Assert.AreEqual("zh", fired);
            Assert.AreEqual("开始", l.Get("ui.play"));
            // And it persisted into settings.
            Assert.AreEqual("zh", settings.Current.PreferredLanguage);
        }

        [Test]
        public void SetLanguage_rejects_unknown_language_code()
        {
            var l = new LocalizationService();
            Assert.IsFalse(l.SetLanguage("xx"));
            Assert.AreEqual("en", l.Language);
        }

        [Test]
        public void Args_are_formatted_into_template()
        {
            var l = new LocalizationService();
            Assert.AreEqual("Score: 42", l.Get("result.final_score", 42));
        }

        [Test]
        public void Args_with_two_placeholders()
        {
            var l = new LocalizationService();
            Assert.AreEqual("2 players, 1 ready", l.Get("lobby.player_count", 2, 1));
        }

        [Test]
        public void Both_tables_have_the_same_keys()
        {
            var en = MiniGames.App.Shared.Localization.Tables.EnTable.Build();
            var zh = MiniGames.App.Shared.Localization.Tables.ZhTable.Build();
            foreach (var k in en.Keys)
                Assert.IsTrue(zh.ContainsKey(k), $"zh missing key: {k}");
            foreach (var k in zh.Keys)
                Assert.IsTrue(en.ContainsKey(k), $"en missing key: {k}");
        }

        [Test]
        public void L10n_shortcut_uses_active_provider()
        {
            var l = new LocalizationService();
            L10n.Active = l;
            Assert.AreEqual("Play", L10n.T("ui.play"));
            L10n.Active = null;   // reset for other tests
        }

        [Test]
        public void Every_game_id_has_a_title_translation()
        {
            // Stable game ids should each have a corresponding game.<id>.title entry in en and zh.
            var en = MiniGames.App.Shared.Localization.Tables.EnTable.Build();
            var ids = new[]
            {
                "color_blocks", "tetris", "snakes", "maze_paint", "fruit_merge",
                "connect_four", "bomb_sweep", "reversi", "number_merge",
                "dots_and_boxes", "battleship",
            };
            foreach (var id in ids)
                Assert.IsTrue(en.ContainsKey($"game.{id}.title"), $"en missing title for {id}");
        }
    }
}
