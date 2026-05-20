using System;
using System.Collections.Generic;
using MiniGames.App.Shared.HighScores;
using MiniGames.GameModule;
using NUnit.Framework;

namespace MiniGames.Tests.App.HighScores
{
    public class HighScoresServiceTests
    {
        private sealed class InMemorySaveStore : ISaveStore
        {
            private readonly Dictionary<string, object> _data = new Dictionary<string, object>();
            public bool TryLoad<T>(string key, out T value) where T : class
            {
                if (_data.TryGetValue(key, out var raw)) { value = (T)raw; return true; }
                value = null;
                return false;
            }
            public void Save<T>(string key, T value) where T : class { _data[key] = value; }
            public void Delete(string key) { _data.Remove(key); }
        }

        private InMemorySaveStore _store;
        private DateTimeOffset _now;
        private HighScoresService _service;

        [SetUp]
        public void Setup()
        {
            _store = new InMemorySaveStore();
            _now = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
            _service = new HighScoresService(_store, () => _now);
        }

        [Test]
        public void First_score_takes_rank_1()
        {
            int rank = _service.Submit("color_blocks", "p1", "Alice", 100);
            Assert.AreEqual(1, rank);
        }

        [Test]
        public void Higher_score_ranks_above_lower()
        {
            _service.Submit("color_blocks", "p1", "Alice", 100);
            int rank = _service.Submit("color_blocks", "p2", "Bob", 200);
            Assert.AreEqual(1, rank);
            var top = _service.Top("color_blocks");
            Assert.AreEqual("Bob", top[0].DisplayName);
            Assert.AreEqual("Alice", top[1].DisplayName);
        }

        [Test]
        public void Leaderboard_caps_at_MaxEntriesPerGame()
        {
            for (int i = 0; i < HighScoresService.MaxEntriesPerGame + 5; i++)
                _service.Submit("g", $"p{i}", $"P{i}", i);
            Assert.AreEqual(HighScoresService.MaxEntriesPerGame, _service.Top("g").Count);
        }

        [Test]
        public void Score_below_lowest_top_entry_returns_rank_0()
        {
            for (int i = 0; i < HighScoresService.MaxEntriesPerGame; i++)
                _service.Submit("g", $"p{i}", $"P{i}", 1000 + i);
            int rank = _service.Submit("g", "low", "Low", 1);
            Assert.AreEqual(0, rank);
        }

        [Test]
        public void BestFor_returns_players_top_entry()
        {
            _service.Submit("g", "p1", "Alice", 50);
            _service.Submit("g", "p1", "Alice", 150);
            _service.Submit("g", "p1", "Alice", 100);
            Assert.AreEqual(150, _service.BestFor("g", "p1").Score);
        }

        [Test]
        public void BestFor_returns_null_for_unknown_player()
        {
            Assert.IsNull(_service.BestFor("g", "ghost"));
        }

        [Test]
        public void Different_games_keep_independent_leaderboards()
        {
            _service.Submit("a", "p", "P", 100);
            _service.Submit("b", "p", "P", 50);
            Assert.AreEqual(100, _service.Top("a")[0].Score);
            Assert.AreEqual(50, _service.Top("b")[0].Score);
        }

        [Test]
        public void Clear_drops_a_games_leaderboard()
        {
            _service.Submit("g", "p", "P", 100);
            _service.Clear("g");
            Assert.AreEqual(0, _service.Top("g").Count);
        }

        [Test]
        public void Scores_persist_across_service_instances_via_store()
        {
            _service.Submit("g", "p", "Alice", 42);
            var freshService = new HighScoresService(_store, () => _now);
            var top = freshService.Top("g");
            Assert.AreEqual(1, top.Count);
            Assert.AreEqual(42, top[0].Score);
        }
    }
}
