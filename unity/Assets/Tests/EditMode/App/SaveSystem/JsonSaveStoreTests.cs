using System;
using System.IO;
using MiniGames.App.Shared.SaveSystem;
using NUnit.Framework;

namespace MiniGames.Tests.App.SaveSystem
{
    public class JsonSaveStoreTests
    {
        private string _dir;
        private JsonSaveStore _store;

        [Serializable]
        public class Sample
        {
            public string Name;
            public int Score;
        }

        [SetUp]
        public void Setup()
        {
            _dir = Path.Combine(Path.GetTempPath(), "minigames_savetest_" + Guid.NewGuid().ToString("N"));
            _store = new JsonSaveStore(_dir);
        }

        [TearDown]
        public void Teardown()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        [Test]
        public void Save_then_load_round_trips()
        {
            _store.Save("profile", new Sample { Name = "Ada", Score = 42 });
            Assert.IsTrue(_store.TryLoad<Sample>("profile", out var loaded));
            Assert.AreEqual("Ada", loaded.Name);
            Assert.AreEqual(42, loaded.Score);
        }

        [Test]
        public void TryLoad_returns_false_for_missing_key()
        {
            Assert.IsFalse(_store.TryLoad<Sample>("nope", out _));
        }

        [Test]
        public void Delete_removes_the_file()
        {
            _store.Save("k", new Sample { Name = "x", Score = 1 });
            _store.Delete("k");
            Assert.IsFalse(_store.TryLoad<Sample>("k", out _));
        }

        [Test]
        public void Overwriting_a_key_works()
        {
            _store.Save("k", new Sample { Name = "first", Score = 1 });
            _store.Save("k", new Sample { Name = "second", Score = 2 });
            Assert.IsTrue(_store.TryLoad<Sample>("k", out var loaded));
            Assert.AreEqual("second", loaded.Name);
            Assert.AreEqual(2, loaded.Score);
        }

        [Test]
        public void Disallowed_filename_chars_are_sanitized_not_rejected()
        {
            _store.Save("evil/key:with*chars", new Sample { Name = "ok", Score = 3 });
            Assert.IsTrue(_store.TryLoad<Sample>("evil/key:with*chars", out var loaded));
            Assert.AreEqual("ok", loaded.Name);
        }
    }
}
