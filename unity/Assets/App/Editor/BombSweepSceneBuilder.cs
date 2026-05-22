using MiniGames.App.Games;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGames.App.Editor
{
    /// <summary>
    /// One-click Bomb Sweep play scene. Builds Assets/Scenes/BombSweep.unity
    /// with a status line, a 13x11 arena grid (cells spawned at runtime by
    /// BombSweepSceneController), a hold-to-move D-pad, a Bomb button, plus
    /// Back and Restart. Run via MiniGames -> Build Bomb Sweep Scene.
    /// </summary>
    public static class BombSweepSceneBuilder
    {
        private const string SceneDir = "Assets/Scenes";
        private const string ScenePath = SceneDir + "/BombSweep.unity";

        private static readonly Color Bg = new Color(0.08f, 0.09f, 0.12f);
        private static readonly Color BoardFrame = new Color(0.12f, 0.13f, 0.16f);
        private static readonly Color Accent = new Color(0.30f, 0.55f, 0.95f);
        private static readonly Color BombAccent = new Color(0.90f, 0.40f, 0.35f);

        [MenuItem("MiniGames/Build Bomb Sweep Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Build Bomb Sweep Scene", "Stop Play mode first.", "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder(SceneDir))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            BuildScene();

            EditorUtility.DisplayDialog("Build Bomb Sweep Scene",
                "Done. From the Hub, tap Bomb Sweep -> Solo to play, or open " +
                "BombSweep.unity and press Play. D-pad/WASD to move, Bomb/Space to drop.", "OK");
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
            statusRt.anchorMin = new Vector2(0.15f, 1f);
            statusRt.anchorMax = new Vector2(0.85f, 1f);
            statusRt.pivot = new Vector2(0.5f, 1f);
            statusRt.sizeDelta = new Vector2(0f, 90f);
            statusRt.anchoredPosition = new Vector2(0f, -36f);
            var status = statusGo.AddComponent<TextMeshProUGUI>();
            status.text = "You (blue) vs CPU (red)";
            status.fontSize = 36;
            status.alignment = TextAlignmentOptions.Center;
            status.color = Color.white;

            // Arena grid (cells spawned at runtime), upper-center.
            var boardGo = NewUI("Board", canvasRt, out var boardRt);
            boardRt.anchorMin = boardRt.anchorMax = new Vector2(0.5f, 0.5f);
            boardRt.pivot = new Vector2(0.5f, 0.5f);
            boardRt.sizeDelta = new Vector2(960f, 820f);
            boardRt.anchoredPosition = new Vector2(0f, 240f);
            boardGo.AddComponent<Image>().color = BoardFrame;
            var grid = boardGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(70f, 70f);
            grid.spacing = new Vector2(2f, 2f);
            grid.padding = new RectOffset(6, 6, 6, 6);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 13;
            grid.childAlignment = TextAnchor.MiddleCenter;

            // D-pad (bottom-left, plus layout). Hold to move.
            var size = new Vector2(130f, 130f);
            var upButton    = MakeHold("UpButton",    canvasRt, "Up",    new Vector2(-300f, 320f), size);
            var downButton  = MakeHold("DownButton",  canvasRt, "Down",  new Vector2(-300f, 110f), size);
            var leftButton  = MakeHold("LeftButton",  canvasRt, "Left",  new Vector2(-430f, 215f), size);
            var rightButton = MakeHold("RightButton", canvasRt, "Right", new Vector2(-170f, 215f), size);

            // Bomb button (bottom-right, big).
            var bombButton = MakeBottomButton("BombButton", canvasRt, "BOMB",
                new Vector2(300f, 215f), new Vector2(240f, 240f), BombAccent);

            var ctrl = canvasGo.AddComponent<BombSweepSceneController>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("_boardGrid").objectReferenceValue = boardRt;
            so.FindProperty("_status").objectReferenceValue = status;
            so.FindProperty("_backButton").objectReferenceValue = backButton;
            so.FindProperty("_restartButton").objectReferenceValue = restartButton;
            so.FindProperty("_upButton").objectReferenceValue = upButton;
            so.FindProperty("_downButton").objectReferenceValue = downButton;
            so.FindProperty("_leftButton").objectReferenceValue = leftButton;
            so.FindProperty("_rightButton").objectReferenceValue = rightButton;
            so.FindProperty("_bombButton").objectReferenceValue = bombButton;
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
            BombSweepSceneController ctrl = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                ctrl = root.GetComponentInChildren<BombSweepSceneController>(true);
                if (ctrl != null) break;
            }
            if (ctrl == null)
                throw new System.Exception("Saved scene has no BombSweepSceneController.");

            var so = new SerializedObject(ctrl);
            var missing = new System.Collections.Generic.List<string>();
            foreach (var f in new[]
            {
                "_boardGrid", "_status", "_backButton", "_restartButton",
                "_upButton", "_downButton", "_leftButton", "_rightButton", "_bombButton"
            })
            {
                var prop = so.FindProperty(f);
                if (prop == null || prop.objectReferenceValue == null) missing.Add(f);
            }
            if (missing.Count > 0)
                throw new System.Exception(
                    "BombSweepSceneController wiring did not persist. Null: " + string.Join(", ", missing));
        }

        // ---- helpers (duplicated intentionally to keep builders independent) ----

        private static HoldButton MakeHold(string name, RectTransform parent, string label,
            Vector2 pos, Vector2 size)
        {
            var btn = MakeBottomButton(name, parent, label, pos, size, Accent);
            return btn.gameObject.AddComponent<HoldButton>();
        }

        private static Button MakeBottomButton(string name, RectTransform parent, string label,
            Vector2 pos, Vector2 size, Color color)
        {
            var go = NewUI(name, parent, out var rt);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = go.AddComponent<Image>();
            img.color = color;
            var button = go.AddComponent<Button>();
            button.targetGraphic = img;

            var labelGo = NewUI("Label", rt, out var labelRt);
            Stretch(labelRt);
            var text = labelGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 34;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            return button;
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
