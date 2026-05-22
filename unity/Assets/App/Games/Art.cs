using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGames.App.Games
{
    /// <summary>
    /// Optional art pipeline. Drop PNGs under
    ///   Assets/Resources/Art/Games/&lt;gameId&gt;/&lt;name&gt;.png
    /// and they're picked up at runtime; when a file is missing the caller
    /// falls back to the procedural color/shape look. Sprites are cached
    /// (including misses) so lookups are cheap on every render.
    ///
    /// See docs/art_assets.md for the per-game filename convention.
    /// </summary>
    public static class Art
    {
        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        public static Sprite Load(string gameId, string name)
        {
            string key = gameId + "/" + name;
            if (_cache.TryGetValue(key, out var cached)) return cached;

            string path = "Art/Games/" + key;
            var sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
            {
                // Texture imported as "Default" rather than "Sprite": wrap it.
                var tex = Resources.Load<Texture2D>(path);
                if (tex != null)
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), 100f);
            }
            _cache[key] = sprite; // caches null too (means "no art, use fallback")
            return sprite;
        }

        public static bool Has(string gameId, string name) => Load(gameId, name) != null;

        /// <summary>Apply art to an Image if present. Returns false (Image untouched) when missing.</summary>
        public static bool TryApply(Image img, string gameId, string name,
            bool preserveAspect = true, bool keepColor = false)
        {
            if (img == null) return false;
            var sprite = Load(gameId, name);
            if (sprite == null) return false;
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = preserveAspect;
            if (!keepColor) img.color = Color.white; // keepColor: caller tints a white sprite
            return true;
        }
    }
}
