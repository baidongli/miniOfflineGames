using MiniGames.App.Games;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGames.App.Editor
{
    /// <summary>
    /// One-click Fruit Merge play scene. Builds Assets/Scenes/FruitMerge.unity
    /// with a score line, a "next" swatch, a 7x12 board grid (cells spawned at
    /// runtime by FruitMergeSceneController), Back and Restart buttons. Run via
    /// MiniGames -> Build Fruit Merge Scene.
    /// </summary>
    public static class FruitMergeSceneBuilder
    {
        private const string SceneDir = "Assets/Scenes";
        private const string ScenePath = SceneDir + "/FruitMerge.unity";

        private static readonly Color Bg = new Color(0.09f, 0.08f, 0.12f);
        private static readonly Color BoardFrame = new Color(0.13f, 0.12f, 0.16f);
        private static readonly Color Accent = new Color(0.30f, 0.55f, 0.95f);

        [MenuItem("MiniGames/Build Fruit Merge Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Build Fruit Merge Scene", "Stop Play mode first.", "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder(SceneDir))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            BuildScene();

            EditorUtility.DisplayDialog("Build Fruit Merge Scene",
                "Done. From the Hub, tap Fruit Merge -> Solo to play, or open " +
                "FruitMerge.unity and press Play. Tap a column to drop.", "OK");
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
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -32f), new Vector2(180f, 90f));
            var restartButton = MakeButton("RestartButton", canvasRt, "Restart",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -32f), new Vector2(200f, 90f));

            // Score line.
            var statusGo = NewUI("Status", canvasRt, out var statusRt);
            statusRt.anchorMin = new Vector2(0.2f, 1f);
            statusRt.anchorMax = new Vector2(0.8f, 1f);
            statusRt.pivot = new Vector2(0.5f, 1f);
            statusRt.sizeDelta = new Vector2(0f, 90f);
            statusRt.anchoredPosition = new Vector2(0f, -36f);
            var status = statusGo.AddComponent<TextMeshProUGUI>();
            status.text = "Score 0";
            status.fontSize = 38;
            status.alignment = TextAlignmentOptions.Center;
            status.color = Color.white;

            // "Next" label + swatch.
            var nextLabelGo = NewUI("NextLabel", canvasRt, out var nextLabelRt);
            nextLabelRt.anchorMin = nextLabelRt.anchorMax = new Vector2(0.5f, 1f);
            nextLabelRt.pivot = new Vector2(1f, 1f);
            nextLabelRt.sizeDelta = new Vector2(160f, 70f);
            nextLabelRt.anchoredPosition = new Vector2(-20f, -150f);
            var nextLabel = nextLabelGo.AddComponent<TextMeshProUGUI>();
            nextLabel.text = "Next";
            nextLabel.fontSize = 34;
            nextLabel.alignment = TextAlignmentOptions.Right;
            nextLabel.color = Color.white;

            var swatchGo = NewUI("NextSwatch", canvasRt, out var swatchRt);
            swatchRt.anchorMin = swatchRt.anchorMax = new Vector2(0.5f, 1f);
            swatchRt.pivot = new Vector2(0f, 1f);
            swatchRt.sizeDelta = new Vector2(70f, 70f);
            swatchRt.anchoredPosition = new Vector2(20f, -150f);
            var nextSwatch = swatchGo.AddComponent<Image>();
            nextSwatch.color = Color.gray;

            // Board grid (cells spawned at runtime).
            var boardGo = NewUI("Board", canvasRt, out var boardRt);
            boardRt.anchorMin = boardRt.anchorMax = new Vector2(0.5f, 0.5f);
            boardRt.pivot = new Vector2(0.5f, 0.5f);
            boardRt.sizeDelta = new Vector2(700f, 1190f);
            boardRt.anchoredPosition = new Vector2(0f, -40f);
            boardGo.AddComponent<Image>().color = BoardFrame;
            var grid = boardGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(92f, 92f);
            grid.spacing = new Vector2(6f, 6f);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 7;
            grid.childAlignment = TextAnchor.MiddleCenter;

            // Controller.
            var ctrl = canvasGo.AddComponent<FruitMergeSceneController>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("_boardGrid").objectReferenceValue = boardRt;
            so.FindProperty("_status").objectReferenceValue = status;
            so.FindProperty("_nextSwatch").objectReferenceValue = nextSwatch;
            so.FindProperty("_backButton").objectReferenceValue = backButton;
            so.FindProperty("_restartButton").objectReferenceValue = restartButton;
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
            FruitMergeSceneController ctrl = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                ctrl = root.GetComponentInChildren<FruitMergeSceneController>(true);
                if (ctrl != null) break;
            }
            if (ctrl == null)
                throw new System.Exception("Saved scene has no FruitMergeSceneController.");

            var so = new SerializedObject(ctrl);
            var missing = new System.Collections.Generic.List<string>();
            foreach (var f in new[] { "_boardGrid", "_status", "_nextSwatch", "_backButton", "_restartButton" })
            {
                var prop = so.FindProperty(f);
                if (prop == null || prop.objectReferenceValue == null) missing.Add(f);
            }
            if (missing.Count > 0)
                throw new System.Exception(
                    "FruitMergeSceneController wiring did not persist. Null: " + string.Join(", ", missing));
        }

        // ---- helpers (duplicated intentionally to keep builders independent) ----

        private static Button MakeButton(string name, RectTransform parent, string label,
            Vector2 anchor, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            var go = NewUI(name, parent, out var rt);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            var img = go.AddComponent<Image>();
            img.color = Accent;
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
