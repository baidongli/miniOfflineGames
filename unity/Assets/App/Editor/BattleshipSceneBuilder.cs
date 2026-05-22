using MiniGames.App.Games;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGames.App.Editor
{
    /// <summary>
    /// One-click Battleship play scene. Builds Assets/Scenes/Battleship.unity
    /// with two 10x10 grids (enemy waters on top - tap to fire; your fleet
    /// below), a status line, Randomize/Start setup buttons, Back and Restart.
    /// Cells are spawned at runtime by BattleshipSceneController. Run via
    /// MiniGames -> Build Battleship Scene.
    /// </summary>
    public static class BattleshipSceneBuilder
    {
        private const string SceneDir = "Assets/Scenes";
        private const string ScenePath = SceneDir + "/Battleship.unity";

        private static readonly Color Bg = new Color(0.06f, 0.10f, 0.16f);
        private static readonly Color GridFrame = new Color(0.10f, 0.16f, 0.24f);
        private static readonly Color Accent = new Color(0.30f, 0.55f, 0.95f);

        [MenuItem("MiniGames/Build Battleship Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Build Battleship Scene", "Stop Play mode first.", "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder(SceneDir))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            BuildScene();

            EditorUtility.DisplayDialog("Build Battleship Scene",
                "Done. From the Hub, tap Battleship -> Solo to play, or open " +
                "Battleship.unity and press Play. Randomize/Start, then tap enemy waters.", "OK");
        }

        private static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var canvasGo = new GameObject("Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));

            var canvasRt = canvas.transform as RectTransform;

            var bgGo = NewUI("Background", canvasRt, out var bgRt);
            Stretch(bgRt);
            bgGo.AddComponent<Image>().color = Bg;

            var backButton = MakeButton("BackButton", canvasRt, "Back",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -32f), new Vector2(180f, 90f), Accent);
            var restartButton = MakeButton("RestartButton", canvasRt, "Restart",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -32f), new Vector2(200f, 90f), Accent);

            var statusGo = NewUI("Status", canvasRt, out var statusRt);
            statusRt.anchorMin = new Vector2(0.1f, 1f);
            statusRt.anchorMax = new Vector2(0.9f, 1f);
            statusRt.pivot = new Vector2(0.5f, 1f);
            statusRt.sizeDelta = new Vector2(0f, 90f);
            statusRt.anchoredPosition = new Vector2(0f, -36f);
            var status = statusGo.AddComponent<TextMeshProUGUI>();
            status.text = "Place your fleet: Randomize or Start";
            status.fontSize = 36;
            status.alignment = TextAlignmentOptions.Center;
            status.color = Color.white;

            // Enemy waters grid (tap to fire), upper.
            MakeLabel(canvasRt, "Enemy Waters", new Vector2(0f, 880f), 30, new Color(0.85f, 0.6f, 0.6f));
            var enemyGrid = MakeGrid("EnemyGrid", canvasRt, new Vector2(870f, 870f),
                new Vector2(0f, 360f), cell: 84f, spacing: 3f, pad: 6);

            // Own fleet grid, lower.
            MakeLabel(canvasRt, "Your Fleet", new Vector2(0f, -240f), 28, new Color(0.6f, 0.75f, 0.9f));
            var ownGrid = MakeGrid("OwnGrid", canvasRt, new Vector2(430f, 430f),
                new Vector2(0f, -480f), cell: 40f, spacing: 2f, pad: 4);

            // Setup buttons (hidden once the battle starts).
            var randomizeButton = MakeButton("RandomizeButton", canvasRt, "Randomize",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-150f, 60f), new Vector2(260f, 100f), Accent);
            var startButton = MakeButton("StartButton", canvasRt, "Start",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(150f, 60f), new Vector2(260f, 100f),
                new Color(0.35f, 0.75f, 0.45f));

            var ctrl = canvasGo.AddComponent<BattleshipSceneController>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("_enemyGrid").objectReferenceValue = enemyGrid;
            so.FindProperty("_ownGrid").objectReferenceValue = ownGrid;
            so.FindProperty("_status").objectReferenceValue = status;
            so.FindProperty("_backButton").objectReferenceValue = backButton;
            so.FindProperty("_restartButton").objectReferenceValue = restartButton;
            so.FindProperty("_randomizeButton").objectReferenceValue = randomizeButton;
            so.FindProperty("_startButton").objectReferenceValue = startButton;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ctrl);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            VerifyWiring();
        }

        private static void VerifyWiring()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            BattleshipSceneController ctrl = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                ctrl = root.GetComponentInChildren<BattleshipSceneController>(true);
                if (ctrl != null) break;
            }
            if (ctrl == null)
                throw new System.Exception("Saved scene has no BattleshipSceneController.");

            var so = new SerializedObject(ctrl);
            var missing = new System.Collections.Generic.List<string>();
            foreach (var f in new[]
            {
                "_enemyGrid", "_ownGrid", "_status", "_backButton",
                "_restartButton", "_randomizeButton", "_startButton"
            })
            {
                var prop = so.FindProperty(f);
                if (prop == null || prop.objectReferenceValue == null) missing.Add(f);
            }
            if (missing.Count > 0)
                throw new System.Exception(
                    "BattleshipSceneController wiring did not persist. Null: " + string.Join(", ", missing));
        }

        // ---- helpers (duplicated intentionally to keep builders independent) ----

        private static RectTransform MakeGrid(string name, RectTransform parent, Vector2 size,
            Vector2 pos, float cell, float spacing, int pad)
        {
            var go = NewUI(name, parent, out var rt);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            go.AddComponent<Image>().color = GridFrame;
            var grid = go.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(cell, cell);
            grid.spacing = new Vector2(spacing, spacing);
            grid.padding = new RectOffset(pad, pad, pad, pad);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 10;
            grid.childAlignment = TextAnchor.MiddleCenter;
            return rt;
        }

        private static void MakeLabel(RectTransform parent, string text, Vector2 pos, int size, Color color)
        {
            var go = NewUI(text + "Label", parent, out var rt);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(600f, 50f);
            rt.anchoredPosition = pos;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = TextAlignmentOptions.Center;
            t.color = color;
        }

        private static Button MakeButton(string name, RectTransform parent, string label,
            Vector2 anchor, Vector2 pivot, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var go = NewUI(name, parent, out var rt);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            var img = go.AddComponent<Image>();
            img.color = color;
            var button = go.AddComponent<Button>();
            button.targetGraphic = img;

            var labelGo = NewUI("Label", rt, out var labelRt);
            Stretch(labelRt);
            var text = labelGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 30;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            return button;
        }

        private static GameObject NewUI(string name, RectTransform parent, out RectTransform rt)
        {
            var go = new GameObject(name, typeof(RectTransform));
            rt = go.GetComponent<RectTransform>();
            if (parent != null) rt.SetParent(parent, false);
            return go;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);
            foreach (var s in scenes)
                if (s.path == path) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
