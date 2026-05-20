using System.IO;
using MiniGames.GameModule;
using UnityEngine;

namespace MiniGames.App.Shared.SaveSystem
{
    /// <summary>
    /// JSON-on-disk implementation of ISaveStore. One file per key under
    /// the configured base directory. Files are written via .tmp + Rename
    /// so a crash mid-write never leaves a half-flushed file behind.
    ///
    /// Constraints from Unity's JsonUtility:
    ///   - Save type must be a class (or struct) with [Serializable].
    ///   - Public fields only. Properties are ignored.
    ///   - No polymorphism / no Dictionary&lt;,&gt; / no Nullable&lt;T&gt;.
    /// </summary>
    public sealed class JsonSaveStore : ISaveStore
    {
        private readonly string _dir;

        public JsonSaveStore(string baseDir)
        {
            _dir = baseDir;
            Directory.CreateDirectory(_dir);
        }

        public static JsonSaveStore ForPlayer() =>
            new JsonSaveStore(Path.Combine(Application.persistentDataPath, "saves"));

        public bool TryLoad<T>(string key, out T value) where T : class
        {
            value = null;
            var path = PathFor(key);
            if (!File.Exists(path)) return false;
            try
            {
                var json = File.ReadAllText(path);
                value = JsonUtility.FromJson<T>(json);
                return value != null;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveStore] failed to load {key}: {e.Message}");
                return false;
            }
        }

        public void Save<T>(string key, T value) where T : class
        {
            var path = PathFor(key);
            var tmp = path + ".tmp";
            var json = JsonUtility.ToJson(value);
            File.WriteAllText(tmp, json);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        public void Delete(string key)
        {
            var path = PathFor(key);
            if (File.Exists(path)) File.Delete(path);
        }

        private string PathFor(string key) => Path.Combine(_dir, Sanitize(key) + ".json");

        private static string Sanitize(string key)
        {
            // Allow letters, digits, underscores, dashes; replace everything else.
            var chars = key.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (!(char.IsLetterOrDigit(chars[i]) || chars[i] == '_' || chars[i] == '-')) chars[i] = '_';
            return new string(chars);
        }
    }
}
