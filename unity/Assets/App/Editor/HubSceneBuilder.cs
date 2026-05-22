using MiniGames.App.Hub;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGames.App.Editor
{
    /// <summary>
    /// One-click Hub scaffolding. Builds Assets/Prefabs/UI/GameCard.prefab and
    /// Assets/Scenes/Hub.unity with a Canvas + scrollable game grid, then wires
    /// every HubController / GameCardView reference. Purely functional layout -
    /// visual polish (real icons, palettes, fonts) is a later phase.
    ///
    /// Run via the menu: MiniGames -> Build Hub Scene.
    /// </summary>
    public static class HubSceneBuilder
    {
        private const string PrefabDir = "Assets/Prefabs/UI";
        private const string PrefabPath = PrefabDir + "/GameCard.prefab";
        private const string SceneDir = "Assets/Scenes";
        private const string ScenePath = SceneDir + "/Hub.unity";

        private static readonly Color Bg = new Color(0.10f, 0.11f, 0.16f);
        private static readonly Color CardBg = new Color(0.18f, 0.20f, 0.28f);
        private static readonly Color Accent = new Color(0.30f, 0.55f, 0.95f);
        private static readonly Color MpColor = new Color(0.35f, 0.80f, 0.45f);
        private static readonly Color SdColor = new Color(0.95f, 0.70f, 0.25f);

        [MenuItem("MiniGames/Build Hub Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Build Hub Scene",
                    "Stop Play mode first - scenes can't be created while the " +
                    "editor is playing.",
                    "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                "Build Hub Scene",
                "This generates GameCard.prefab and Hub.unity (overwriting any " +
                "existing ones) and replaces the currently open scene.\n\n" +
                "Tip: import TMP Essentials first (Window > TextMeshPro > Import " +
                "TMP Essential Resources) or card text won't render.\n\nContinue?",
                "Build", "Cancel"))
            {
                return;
            }

            EnsureFolder(PrefabDir);
            EnsureFolder(SceneDir);

            var cardPrefab = BuildGameCardPrefab();
            BuildHubScene(cardPrefab);

            EditorUtility.DisplayDialog(
                "Build Hub Scene",
                "Done.\n\nGameCard.prefab + Hub.unity created. Open Hub.unity and " +
                "press Play - you should see 11 game cards.",
                "OK");
        }

        // ---- GameCard prefab ------------------------------------------------

        private static GameCardView BuildGameCardPrefab()
        {
            var root = NewUI("GameCard", null, out var rootRt);
            Stretch(rootRt);
            var bg = root.AddComponent<Image>();
            bg.color = CardBg;
            var button = root.AddComponent<Button>();
            button.targetGraphic = bg;
            var view = root.AddComponent<GameCardView>();

            // Icon: top portion of the card.
            var iconGo = NewUI("Icon", rootRt, out var iconRt);
            iconRt.anchorMin = new Vector2(0.15f, 0.40f);
            iconRt.anchorMax = new Vector2(0.85f, 0.92f);
            iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;
            var icon = iconGo.AddComponent<Image>();
            icon.color = Accent;

            // Title: bottom strip.
            var titleGo = NewUI("Title", rootRt, out var titleRt);
            titleRt.anchorMin = new Vector2(0.05f, 0.05f);
            titleRt.anchorMax = new Vector2(0.95f, 0.36f);
            titleRt.offsetMin = titleRt.offsetMax = Vector2.zero;
            var title = titleGo.AddComponent<TextMeshProUGUI>();
            title.text = "Game";
            title.fontSize = 36;
            title.alignment = TextAlignmentOptions.Center;
            title.enableAutoSizing = true;
            title.fontSizeMin = 14;
            title.fontSizeMax = 40;
            title.color = Color.white;

            // Badges: tiny corner squares.
            var mpGo = NewUI("MultiplayerBadge", rootRt, out var mpRt);
            Corner(mpRt, new Vector2(1f, 1f), new Vector2(-12f, -12f));
            mpGo.AddComponent<Image>().color = MpColor;

            var sdGo = NewUI("SameDeviceBadge", rootRt, out var sdRt);
            Corner(sdRt, new Vector2(0f, 1f), new Vector2(12f, -12f));
            sdGo.AddComponent<Image>().color = SdColor;

            // Wire the private [SerializeField] refs by field name.
            var so = new SerializedObject(view);
            so.FindProperty("_title").objectReferenceValue = title;
            so.FindProperty("_icon").objectReferenceValue = icon;
            so.FindProperty("_button").objectReferenceValue = button;
            so.FindProperty("_multiplayerBadge").objectReferenceValue = mpGo;
            so.FindProperty("_sameDeviceBadge").objectReferenceValue = sdGo;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            // Re-load from disk: the object returned by SaveAsPrefabAsset can
            // hand back a null component before the import settles, which would
            // wire a null _cardPrefab into the Hub.
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(PrefabPath);
            var reloaded = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var prefabView = reloaded != null ? reloaded.GetComponent<GameCardView>() : null;
            if (prefabView == null)
                throw new System.Exception(
                    "GameCard prefab failed to save with a GameCardView component.");
            return prefabView;
        }

        // ---- Hub scene ------------------------------------------------------

        private static void BuildHubScene(GameCardView cardPrefab)
        {
            if (cardPrefab == null)
                throw new System.ArgumentNullException(nameof(cardPrefab),
                    "Card prefab is null; aborting before building a broken Hub scene.");

            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Canvas.
            var canvasGo = new GameObject("Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            // EventSystem.
            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));

            // Full-screen background.
            var bgGo = NewUI("Background", canvas.transform as RectTransform, out var bgRt);
            Stretch(bgRt);
            bgGo.AddComponent<Image>().color = Bg;

            // Header bar (title + menu + remove-ads).
            var headerGo = NewUI("Header", canvas.transform as RectTransform, out var headerRt);
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(0f, 160f);
            headerRt.anchoredPosition = Vector2.zero;

            var titleGo = NewUI("AppTitle", headerRt, out var titleRt);
            titleRt.anchorMin = new Vector2(0f, 0f);
            titleRt.anchorMax = new Vector2(0.6f, 1f);
            titleRt.offsetMin = new Vector2(32f, 0f);
            titleRt.offsetMax = Vector2.zero;
            var appTitle = titleGo.AddComponent<TextMeshProUGUI>();
            appTitle.text = "Mini Games";
            appTitle.fontSize = 56;
            appTitle.alignment = TextAlignmentOptions.Left;
            appTitle.color = Color.white;

            var menuButton = MakeButton("MenuButton", headerRt, "Menu",
                new Vector2(1f, 0.5f), new Vector2(-32f, 0f), new Vector2(180f, 90f));
            var removeAdsButton = MakeButton("RemoveAdsButton", headerRt, "Remove Ads",
                new Vector2(1f, 0.5f), new Vector2(-232f, 0f), new Vector2(220f, 90f));

            // Energy bar (small label under the title).
            var energyGo = NewUI("EnergyBar", headerRt, out var energyRt);
            energyRt.anchorMin = new Vector2(0f, 0f);
            energyRt.anchorMax = new Vector2(0.6f, 0f);
            energyRt.pivot = new Vector2(0f, 1f);
            energyRt.sizeDelta = new Vector2(0f, 48f);
            energyRt.anchoredPosition = new Vector2(32f, -4f);
            var energyView = energyGo.AddComponent<EnergyBarView>();
            var energyLabelGo = NewUI("Label", energyRt, out var energyLabelRt);
            Stretch(energyLabelRt);
            var energyLabel = energyLabelGo.AddComponent<TextMeshProUGUI>();
            energyLabel.text = "Energy: 5";
            energyLabel.fontSize = 28;
            energyLabel.alignment = TextAlignmentOptions.Left;
            energyLabel.color = new Color(0.8f, 0.85f, 1f);
            var energySo = new SerializedObject(energyView);
            energySo.FindProperty("_label").objectReferenceValue = energyLabel;
            energySo.ApplyModifiedPropertiesWithoutUndo();

            // Scroll view holding the card grid.
            var scrollGo = NewUI("GameScroll", canvas.transform as RectTransform, out var scrollRt);
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(0f, 0f);
            scrollRt.offsetMax = new Vector2(0f, -160f); // leave room for header
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            var viewportGo = NewUI("Viewport", scrollRt, out var viewportRt);
            Stretch(viewportRt);
            var viewportImg = viewportGo.AddComponent<Image>();
            viewportImg.color = new Color(1f, 1f, 1f, 0.01f);
            viewportGo.AddComponent<Mask>().showMaskGraphic = false;

            var contentGo = NewUI("Content", viewportRt, out var contentRt);
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            var grid = contentGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(320f, 380f);
            grid.spacing = new Vector2(24f, 24f);
            grid.padding = new RectOffset(24, 24, 24, 24);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRt;
            scroll.content = contentRt;

            // HubController on the canvas, wired to everything.
            var hub = canvasGo.AddComponent<HubController>();
            var hubSo = new SerializedObject(hub);
            hubSo.FindProperty("_gameGrid").objectReferenceValue = contentRt;
            hubSo.FindProperty("_cardPrefab").objectReferenceValue = cardPrefab;
            hubSo.FindProperty("_menuButton").objectReferenceValue = menuButton;
            hubSo.FindProperty("_removeAdsButton").objectReferenceValue = removeAdsButton;
            hubSo.FindProperty("_energyBar").objectReferenceValue = energyView;
            hubSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
        }

        // ---- helpers --------------------------------------------------------

        private static Button MakeButton(string name, RectTransform parent, string label,
            Vector2 anchor, Vector2 anchoredPos, Vector2 size)
        {
            var go = NewUI(name, parent, out var rt);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(1f, 0.5f);
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
            text.fontSize = 28;
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

        private static void Corner(RectTransform rt, Vector2 anchor, Vector2 anchoredPos)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.sizeDelta = new Vector2(28f, 28f);
            rt.anchoredPosition = anchoredPos;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            var leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);
            foreach (var s in scenes)
                if (s.path == path) return; // already present
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
