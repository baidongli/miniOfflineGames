using MiniGames.App.Games;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGames.App.Editor
{
    /// <summary>
    /// One-click 2048 play scene. Builds Assets/Scenes/NumberMerge.unity with a
    /// score line, a 4x4 board container (tiles spawned at runtime by
    /// NumberMergeSceneController), Back and New Game buttons. Run via
    /// MiniGames -> Build 2048 Scene.
    /// </summary>
    public static class NumberMergeSceneBuilder
    {
        private const string SceneDir = "Assets/Scenes";
        private const string ScenePath = SceneDir + "/NumberMerge.unity";

        private static readonly Color Bg = new Color(0.10f, 0.10f, 0.12f);
        private static readonly Color BoardFrame = new Color(0.74f, 0.68f, 0.62f);
        private static readonly Color Accent = new Color(0.30f, 0.55f, 0.95f);

        [MenuItem("MiniGames/Build 2048 Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Build 2048 Scene", "Stop Play mode first.", "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder(SceneDir))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            BuildScene();

            EditorUtility.DisplayDialog("Build 2048 Scene",
                "Done. From the Hub, tap 2048 -> Solo to play, or open " +
                "NumberMerge.unity and press Play directly. Swipe / arrow keys to move.", "OK");
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
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -32f));
            var newGameButton = MakeButton("NewGameButton", canvasRt, "New Game",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -32f));

            // Score line (top center).
            var statusGo = NewUI("Status", canvasRt, out var statusRt);
            statusRt.anchorMin = new Vector2(0.2f, 1f);
            statusRt.anchorMax = new Vector2(0.8f, 1f);
            statusRt.pivot = new Vector2(0.5f, 1f);
            statusRt.sizeDelta = new Vector2(0f, 100f);
            statusRt.anchoredPosition = new Vector2(0f, -36f);
            var status = statusGo.AddComponent<TextMeshProUGUI>();
            status.text = "Score 0";
            status.fontSize = 40;
            status.alignment = TextAlignmentOptions.Center;
            status.color = Color.white;

            // Board container (tiles spawned at runtime).
            var boardGo = NewUI("Board", canvasRt, out var boardRt);
            boardRt.anchorMin = boardRt.anchorMax = new Vector2(0.5f, 0.5f);
            boardRt.pivot = new Vector2(0.5f, 0.5f);
            boardRt.sizeDelta = new Vector2(1000f, 1000f);
            boardRt.anchoredPosition = Vector2.zero;
            boardGo.AddComponent<Image>().color = BoardFrame;
            var grid = boardGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(220f, 220f);
            grid.spacing = new Vector2(18f, 18f);
            grid.padding = new RectOffset(16, 16, 16, 16);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.MiddleCenter;

            // Hint line (bottom).
            var hintGo = NewUI("Hint", canvasRt, out var hintRt);
            hintRt.anchorMin = new Vector2(0.1f, 0f);
            hintRt.anchorMax = new Vector2(0.9f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.sizeDelta = new Vector2(0f, 80f);
            hintRt.anchoredPosition = new Vector2(0f, 40f);
            var hint = hintGo.AddComponent<TextMeshProUGUI>();
            hint.text = "Swipe or use arrow keys";
            hint.fontSize = 30;
            hint.alignment = TextAlignmentOptions.Center;
            hint.color = new Color(0.7f, 0.7f, 0.75f);

            // Controller.
            var ctrl = canvasGo.AddComponent<NumberMergeSceneController>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("_boardGrid").objectReferenceValue = boardRt;
            so.FindProperty("_status").objectReferenceValue = status;
            so.FindProperty("_backButton").objectReferenceValue = backButton;
            so.FindProperty("_newGameButton").objectReferenceValue = newGameButton;
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
            NumberMergeSceneController ctrl = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                ctrl = root.GetComponentInChildren<NumberMergeSceneController>(true);
                if (ctrl != null) break;
            }
            if (ctrl == null)
                throw new System.Exception("Saved scene has no NumberMergeSceneController.");

            var so = new SerializedObject(ctrl);
            var missing = new System.Collections.Generic.List<string>();
            foreach (var f in new[] { "_boardGrid", "_status", "_backButton", "_newGameButton" })
            {
                var prop = so.FindProperty(f);
                if (prop == null || prop.objectReferenceValue == null) missing.Add(f);
            }
            if (missing.Count > 0)
                throw new System.Exception(
                    "NumberMergeSceneController wiring did not persist. Null: " +
                    string.Join(", ", missing));
        }

        // ---- helpers (duplicated intentionally to keep builders independent) ----

        private static Button MakeButton(string name, RectTransform parent, string label,
            Vector2 anchor, Vector2 pivot, Vector2 anchoredPos)
        {
            var go = NewUI(name, parent, out var rt);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = new Vector2(220f, 90f);
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
