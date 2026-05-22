using TMPro;
using UnityEngine;

namespace MiniGames.App.Games
{
    /// <summary>
    /// The bundled TMP font (LiberationSans) has no CJK glyphs, so Chinese
    /// shows as boxes. At startup we build a dynamic TMP font asset from an OS
    /// font that does have CJK coverage and register it as a global fallback on
    /// the default font, so any missing glyph (all Chinese) resolves through it.
    /// Latin keeps using LiberationSans; Chinese falls back to the OS font.
    /// </summary>
    public static class CjkFont
    {
        private static bool _installed;

        // Common CJK-capable system font families across Mac/iOS, Windows,
        // Android/Linux. CreateFontAsset returns null when a family is absent.
        private static readonly string[] Families =
        {
            "PingFang SC", "Heiti SC", "Hiragino Sans GB",   // macOS / iOS
            "Microsoft YaHei", "SimHei", "SimSun",           // Windows
            "Noto Sans CJK SC", "Noto Sans SC",              // Android / Linux
            "Source Han Sans SC", "Arial Unicode MS",
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Install()
        {
            if (_installed) return;
            _installed = true;

            var def = TMP_Settings.defaultFontAsset;
            if (def == null) return;

            foreach (var family in Families)
            {
                TMP_FontAsset fa = null;
                try { fa = TMP_FontAsset.CreateFontAsset(family, "Regular"); }
                catch { fa = null; }
                if (fa == null) continue;

                fa.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                def.fallbackFontAssetTable ??= new System.Collections.Generic.List<TMP_FontAsset>();
                if (!def.fallbackFontAssetTable.Contains(fa))
                    def.fallbackFontAssetTable.Add(fa);
                return;
            }

            Debug.LogWarning("[CjkFont] No CJK system font found; Chinese text may not render.");
        }
    }
}
