using System.Collections.Generic;
using UnityEngine;

namespace MiniGames.App.Hub
{
    /// <summary>
    /// Cosmetic per-game identity (icon color + short glyph) for the Hub
    /// cards. Lives in the App layer so game modules stay free of UI concerns.
    /// Keyed by IGameModule.Id; unknown ids fall back to a neutral default.
    /// </summary>
    public static class GameVisuals
    {
        public readonly struct Visual
        {
            public readonly Color Color;
            public readonly string Glyph;
            public Visual(Color color, string glyph) { Color = color; Glyph = glyph; }
        }

        private static readonly Visual Default =
            new Visual(new Color(0.30f, 0.55f, 0.95f), "?");

        private static readonly Dictionary<string, Visual> _map = new Dictionary<string, Visual>
        {
            ["connect_four"]   = new Visual(new Color(0.90f, 0.36f, 0.36f), "4"),
            ["reversi"]        = new Visual(new Color(0.48f, 0.52f, 0.60f), "R"),
            ["number_merge"]   = new Visual(new Color(0.93f, 0.76f, 0.30f), "2048"),
            ["dots_and_boxes"] = new Visual(new Color(0.35f, 0.55f, 0.92f), "DB"),
            ["snakes"]         = new Visual(new Color(0.35f, 0.72f, 0.42f), "Sn"),
            ["tetris"]         = new Visual(new Color(0.66f, 0.42f, 0.86f), "T"),
            ["fruit_merge"]    = new Visual(new Color(0.95f, 0.55f, 0.25f), "Fr"),
            ["bomb_sweep"]     = new Visual(new Color(0.86f, 0.40f, 0.32f), "Bo"),
            ["maze_paint"]     = new Visual(new Color(0.28f, 0.74f, 0.68f), "Mz"),
            ["color_blocks"]   = new Visual(new Color(0.85f, 0.45f, 0.80f), "CB"),
            ["battleship"]     = new Visual(new Color(0.30f, 0.45f, 0.70f), "Bt"),
        };

        public static Visual For(string id)
            => id != null && _map.TryGetValue(id, out var v) ? v : Default;
    }
}
