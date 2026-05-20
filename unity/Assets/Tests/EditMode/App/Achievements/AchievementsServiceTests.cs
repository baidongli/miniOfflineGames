using System;
using System.Collections.Generic;
using MiniGames.App.Shared.Achievements;
using MiniGames.GameModule;
using NUnit.Framework;

namespace MiniGames.Tests.App.Achievements
{
    public class AchievementsServiceTests
    {
        private sealed class InMemorySaveStore : ISaveStore
        {
            private readonly Dictionary<string, object> _d = new Dictionary<string, object>();
            public bool TryLoad<T>(string key, out T value) where T : class
            { if (_d.TryGetValue(key, out var v)) { value = (T)v; return true; } value = null; return false; }
            public void Save<T>(string key, T value) where T : class { _d[key] = value; }
            public void Delete(string key) { _d.Remove(key); }
        }

        private AchievementsService NewService(InMemorySaveStore store = null)
        {
            return new AchievementsService(store ?? new InMemorySaveStore());
        }

        private static AchievementDef Def(string id, string gameId = "tetris")
            => new AchievementDef(id, gameId, $"ach.{id}.title", $"ach.{id}.desc");

        [Test]
        public void Unlock_unknown_achievement_returns_false()
        {
            var s = NewService();
            Assert.IsFalse(s.Unlock("nope"));
        }

        [Test]
        public void Unlock_registered_achievement_returns_true_and_marks_unlocked()
        {
            var s = NewService();
            s.Register(new[] { Def("first_tetris") });
            Assert.IsTrue(s.Unlock("first_tetris"));
            Assert.IsTrue(s.IsUnlocked("first_tetris"));
        }

        [Test]
        public void Unlock_is_idempotent()
        {
            var s = NewService();
            s.Register(new[] { Def("x") });
            Assert.IsTrue(s.Unlock("x"));
            Assert.IsFalse(s.Unlock("x"));    // second unlock is a no-op
            Assert.AreEqual(1, s.AllUnlocked.Count);
        }

        [Test]
        public void UnlockedEvent_fires_once()
        {
            var s = NewService();
            s.Register(new[] { Def("x") });
            int fires = 0;
            s.UnlockedEvent += _ => fires++;
            s.Unlock("x"); s.Unlock("x");
            Assert.AreEqual(1, fires);
        }

        [Test]
        public void Unlocks_persist_across_service_instances()
        {
            var store = new InMemorySaveStore();
            var defs = new[] { Def("survivor") };
            var a = NewService(store); a.Register(defs); a.Unlock("survivor");
            var b = NewService(store); b.Register(defs);
            Assert.IsTrue(b.IsUnlocked("survivor"));
        }

        [Test]
        public void DefsFor_filters_by_game_id()
        {
            var s = NewService();
            s.Register(new[] {
                Def("a", "tetris"),
                Def("b", "snakes"),
                Def("c", "tetris"),
            });
            Assert.AreEqual(2, s.DefsFor("tetris").Count);
            Assert.AreEqual(1, s.DefsFor("snakes").Count);
        }

        [Test]
        public void ResetAll_clears_unlocks()
        {
            var s = NewService();
            s.Register(new[] { Def("x") });
            s.Unlock("x");
            s.ResetAll();
            Assert.IsFalse(s.IsUnlocked("x"));
            Assert.AreEqual(0, s.AllUnlocked.Count);
        }
    }
}
