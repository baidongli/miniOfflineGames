using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace MiniGames.App.Editor
{
    /// <summary>
    /// Generates every game scene in one click. Game scenes aren't committed
    /// (they're produced locally by each builder), so a fresh clone - or any
    /// game you haven't built yet - won't load from the Hub until its scene
    /// exists and is registered in Build Settings. This runs all 11 builders.
    ///
    /// Build the Hub separately via "MiniGames -> Build Hub Scene" (it also
    /// regenerates the GameCard prefab).
    /// </summary>
    public static class BuildAllScenesMenu
    {
        private static readonly Type[] Builders =
        {
            typeof(ConnectFourSceneBuilder),
            typeof(ReversiSceneBuilder),
            typeof(NumberMergeSceneBuilder),
            typeof(DotsAndBoxesSceneBuilder),
            typeof(SnakesSceneBuilder),
            typeof(TetrisSceneBuilder),
            typeof(FruitMergeSceneBuilder),
            typeof(BombSweepSceneBuilder),
            typeof(MazePaintSceneBuilder),
            typeof(ColorBlocksSceneBuilder),
            typeof(BattleshipSceneBuilder),
        };

        [MenuItem("MiniGames/Build All Game Scenes")]
        public static void BuildAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Build All Game Scenes", "Stop Play mode first.", "OK");
                return;
            }

            var failures = new List<string>();
            foreach (var t in Builders)
            {
                var m = t.GetMethod("BuildScene", BindingFlags.Static | BindingFlags.NonPublic);
                if (m == null) { failures.Add($"{t.Name}: no BuildScene"); continue; }
                try { m.Invoke(null, null); }
                catch (Exception e) { failures.Add($"{t.Name}: {(e.InnerException ?? e).Message}"); }
            }

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Build All Game Scenes",
                failures.Count == 0
                    ? $"Done. All {Builders.Length} game scenes built and registered.\n\n" +
                      "If you haven't yet, also run MiniGames -> Build Hub Scene."
                    : "Completed with errors:\n" + string.Join("\n", failures),
                "OK");
        }
    }
}
