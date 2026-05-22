using MiniGames.App.Games;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGames.App.Editor
{
    /// <summary>
    /// One-click Dots and Boxes play scene. Builds
    /// Assets/Scenes/DotsAndBoxes.unity with a status line, a square board
    /// container (dots/edges/boxes are positioned at runtime by
    /// DotsAndBoxesSceneController), and a back button. Run via
    /// MiniGames -> Build Dots And Boxes Scene.
    /// </summary>
    public static class DotsAndBoxesSceneBuilder
    {
        private const string SceneDir = "Assets/Scenes";
        private const string ScenePath = SceneDir + "/DotsAndBoxes.unity";

        private static readonly Color Bg = new Color(0.10f, 0.11f, 0.16f);
        private static readonly Color BoardFrame = new Color(0.14f, 0.15f, 0.22f);
        private static readonly Color Accent = new Color(0.30f, 0.55f, 0.95f);

        [MenuItem("MiniGames/Build Dots And Boxes Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Build Dots And Boxes Scene", "Stop Play mode first.", "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder(SceneDir))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            BuildScene();

            EditorUtility.DisplayDialog("Build Dots And Boxes Scene",
                "Done. From the Hub, tap Dots and Boxes -> Solo to play, or open " +
                "DotsAndBoxes.unity and press Play directly.", "OK");
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

            // Back button (top-left).
            var backGo = NewUI("BackButton", canvasRt, out var backRt);
            backRt.anchorMin = backRt.anchorMax = new Vector2(0f, 1f);
            backRt.pivot = new Vector2(0f, 1f);
            backRt.sizeDelta = new Vector2(180f, 90f);
            backRt.anchoredPosition = new Vector2(32f, -32f);
            var backImg = backGo.AddComponent<Image>();
            backImg.color = Accent;
            var backButton = backGo.AddComponent<Button>();
            backButton.targetGraphic = backImg;
            var backLabelGo = NewUI("Label", backRt, out var backLabelRt);
            Stretch(backLabelRt);
            var backLabel = backLabelGo.AddComponent<TextMeshProUGUI>();
            backLabel.text = "Back";
            backLabel.fontSize = 32;
            backLabel.alignment = TextAlignmentOptions.Center;
            backLabel.color = Color.white;

            // Status line (top center).
            var statusGo = NewUI("Status", canvasRt, out var statusRt);
            statusRt.anchorMin = new Vector2(0.18f, 1f);
            statusRt.anchorMax = new Vector2(0.82f, 1f);
            statusRt.pivot = new Vector2(0.5f, 1f);
            statusRt.sizeDelta = new Vector2(0f, 100f);
            statusRt.anchoredPosition = new Vector2(0f, -32f);
            var status = statusGo.AddComponent<TextMeshProUGUI>();
            status.text = "Your turn";
            status.fontSize = 40;
            status.alignment = TextAlignmentOptions.Center;
            status.color = Color.white;

            // Square board container (children positioned at runtime).
            var boardGo = NewUI("Board", canvasRt, out var boardRt);
            boardRt.anchorMin = boardRt.anchorMax = new Vector2(0.5f, 0.5f);
            boardRt.pivot = new Vector2(0.5f, 0.5f);
            boardRt.sizeDelta = new Vector2(1000f, 1000f);
            boardRt.anchoredPosition = Vector2.zero;
            boardGo.AddComponent<Image>().color = BoardFrame;

            // Controller.
            var ctrl = canvasGo.AddComponent<DotsAndBoxesSceneController>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("_boardArea").objectReferenceValue = boardRt;
            so.FindProperty("_status").objectReferenceValue = status;
            so.FindProperty("_backButton").objectReferenceValue = backButton;
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
            DotsAndBoxesSceneController ctrl = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                ctrl = root.GetComponentInChildren<DotsAndBoxesSceneController>(true);
                if (ctrl != null) break;
            }
            if (ctrl == null)
                throw new System.Exception("Saved scene has no DotsAndBoxesSceneController.");

            var so = new SerializedObject(ctrl);
            var missing = new System.Collections.Generic.List<string>();
            foreach (var f in new[] { "_boardArea", "_status", "_backButton" })
            {
                var prop = so.FindProperty(f);
                if (prop == null || prop.objectReferenceValue == null) missing.Add(f);
            }
            if (missing.Count > 0)
                throw new System.Exception(
                    "DotsAndBoxesSceneController wiring did not persist. Null: " +
                    string.Join(", ", missing));
        }

        // ---- helpers (duplicated intentionally to keep builders independent) ----

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
