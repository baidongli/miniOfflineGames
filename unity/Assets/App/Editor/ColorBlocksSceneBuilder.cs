using MiniGames.App.Games;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGames.App.Editor
{
    /// <summary>
    /// One-click Color Blocks play scene. Builds Assets/Scenes/ColorBlocks.unity
    /// with a score line, a 10x10 board grid (cells spawned at runtime), a hand
    /// area for the 3 draggable pieces, Back and Restart. Run via
    /// MiniGames -> Build Color Blocks Scene.
    /// </summary>
    public static class ColorBlocksSceneBuilder
    {
        private const string SceneDir = "Assets/Scenes";
        private const string ScenePath = SceneDir + "/ColorBlocks.unity";

        private static readonly Color Bg = new Color(0.09f, 0.09f, 0.13f);
        private static readonly Color BoardFrame = new Color(0.13f, 0.13f, 0.17f);
        private static readonly Color HandFrame = new Color(0.11f, 0.11f, 0.15f);
        private static readonly Color Accent = new Color(0.30f, 0.55f, 0.95f);

        [MenuItem("MiniGames/Build Color Blocks Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Build Color Blocks Scene", "Stop Play mode first.", "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder(SceneDir))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            BuildScene();

            EditorUtility.DisplayDialog("Build Color Blocks Scene",
                "Done. From the Hub, tap Color Blocks -> Solo to play, or open " +
                "ColorBlocks.unity and press Play. Drag a piece onto the board.", "OK");
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

            var statusGo = NewUI("Status", canvasRt, out var statusRt);
            statusRt.anchorMin = new Vector2(0.25f, 1f);
            statusRt.anchorMax = new Vector2(0.75f, 1f);
            statusRt.pivot = new Vector2(0.5f, 1f);
            statusRt.sizeDelta = new Vector2(0f, 90f);
            statusRt.anchoredPosition = new Vector2(0f, -36f);
            var status = statusGo.AddComponent<TextMeshProUGUI>();
            status.text = "Score 0";
            status.fontSize = 40;
            status.alignment = TextAlignmentOptions.Center;
            status.color = Color.white;

            // Board grid (cells spawned at runtime), upper-center.
            var boardGo = NewUI("Board", canvasRt, out var boardRt);
            boardRt.anchorMin = boardRt.anchorMax = new Vector2(0.5f, 0.5f);
            boardRt.pivot = new Vector2(0.5f, 0.5f);
            boardRt.sizeDelta = new Vector2(960f, 960f);
            boardRt.anchoredPosition = new Vector2(0f, 230f);
            boardGo.AddComponent<Image>().color = BoardFrame;
            var grid = boardGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(90f, 90f);
            grid.spacing = new Vector2(4f, 4f);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 10;
            grid.childAlignment = TextAnchor.MiddleCenter;

            // Hand area (slots spawned at runtime), bottom.
            var handGo = NewUI("HandArea", canvasRt, out var handRt);
            handRt.anchorMin = handRt.anchorMax = new Vector2(0.5f, 0f);
            handRt.pivot = new Vector2(0.5f, 0f);
            handRt.sizeDelta = new Vector2(1000f, 280f);
            handRt.anchoredPosition = new Vector2(0f, 60f);
            handGo.AddComponent<Image>().color = HandFrame;

            // Controller.
            var ctrl = canvasGo.AddComponent<ColorBlocksSceneController>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("_boardGrid").objectReferenceValue = boardRt;
            so.FindProperty("_handArea").objectReferenceValue = handRt;
            so.FindProperty("_status").objectReferenceValue = status;
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
            ColorBlocksSceneController ctrl = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                ctrl = root.GetComponentInChildren<ColorBlocksSceneController>(true);
                if (ctrl != null) break;
            }
            if (ctrl == null)
                throw new System.Exception("Saved scene has no ColorBlocksSceneController.");

            var so = new SerializedObject(ctrl);
            var missing = new System.Collections.Generic.List<string>();
            foreach (var f in new[] { "_boardGrid", "_handArea", "_status", "_backButton", "_restartButton" })
            {
                var prop = so.FindProperty(f);
                if (prop == null || prop.objectReferenceValue == null) missing.Add(f);
            }
            if (missing.Count > 0)
                throw new System.Exception(
                    "ColorBlocksSceneController wiring did not persist. Null: " + string.Join(", ", missing));
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
