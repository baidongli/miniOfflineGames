using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGames.App.Games
{
    /// <summary>
    /// Per-game how-to-play popup. AttachButton() drops a small "?" button onto
    /// a game's canvas; tapping it shows a localized panel with the game's title
    /// and instructions. Built at runtime, so no prefab/scene wiring is needed.
    /// </summary>
    public sealed class InstructionsOverlay : MonoBehaviour
    {
        private static bool _active;

        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.72f);
        private static readonly Color Panel = new Color(0.16f, 0.18f, 0.26f);
        private static readonly Color Accent = new Color(0.30f, 0.55f, 0.95f);

        /// <summary>Adds a "?" button (top-left, next to Back) that opens the instructions.</summary>
        public static void AttachButton(RectTransform canvas, string gameId)
        {
            if (canvas == null) return;
            var go = new GameObject("HelpButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvas, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(90f, 90f);
            rt.anchoredPosition = new Vector2(230f, -32f);
            var img = go.GetComponent<Image>();
            img.color = Accent;
            Shapes.Circle(img);
            go.GetComponent<Button>().onClick.AddListener(() => Show(gameId));

            var labelGo = new GameObject("Label", typeof(RectTransform));
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.SetParent(rt, false);
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var t = labelGo.AddComponent<TextMeshProUGUI>();
            t.text = "?";
            t.fontSize = 48;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
        }

        public static void Show(string gameId)
        {
            if (_active) return;
            _active = true;
            new GameObject("InstructionsOverlay").AddComponent<InstructionsOverlay>().Build(gameId);
        }

        private void OnDestroy() => _active = false;

        private void Build(string gameId)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            var root = (RectTransform)transform;

            var dim = NewUI("Dim", root, out var dimRt);
            Stretch(dimRt);
            var dimImg = dim.AddComponent<Image>();
            dimImg.color = Dim;
            dim.AddComponent<Button>().onClick.AddListener(() => Destroy(gameObject)); // tap outside closes

            var panelGo = NewUI("Panel", root, out var panelRt);
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(860f, 720f);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = Panel;
            if (!Art.ApplyPanel(panelImg)) Shapes.Rounded(panelImg);

            var titleGo = NewUI("Title", panelRt, out var titleRt);
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(-60f, 130f);
            titleRt.anchoredPosition = new Vector2(0f, -30f);
            var title = titleGo.AddComponent<TextMeshProUGUI>();
            title.text = Loc.T($"game.{gameId}.title");
            title.fontSize = 50;
            title.alignment = TextAlignmentOptions.Center;
            title.color = Color.white;

            var bodyGo = NewUI("Body", panelRt, out var bodyRt);
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(50f, 170f);
            bodyRt.offsetMax = new Vector2(-50f, -170f);
            var body = bodyGo.AddComponent<TextMeshProUGUI>();
            body.text = Loc.T($"howto.{gameId}");
            body.fontSize = 36;
            body.alignment = TextAlignmentOptions.TopLeft;
            body.color = new Color(0.9f, 0.92f, 0.96f);

            var closeGo = NewUI("Close", panelRt, out var closeRt);
            closeRt.anchorMin = closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.sizeDelta = new Vector2(360f, 110f);
            closeRt.anchoredPosition = new Vector2(0f, 36f);
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = Accent;
            if (!Art.ApplyButton(closeImg)) Shapes.Rounded(closeImg);
            closeGo.AddComponent<Button>().onClick.AddListener(() => Destroy(gameObject));
            var closeLabelGo = NewUI("Label", closeRt, out var clrt);
            Stretch(clrt);
            var cl = closeLabelGo.AddComponent<TextMeshProUGUI>();
            cl.text = Loc.T("ui.close");
            cl.fontSize = 38;
            cl.alignment = TextAlignmentOptions.Center;
            cl.color = Color.white;
        }

        private static GameObject NewUI(string name, RectTransform parent, out RectTransform rt)
        {
            var go = new GameObject(name, typeof(RectTransform));
            rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return go;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
