using MiniGames.App.Games;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGames.App.Hub
{
    /// <summary>
    /// Lightweight settings popup opened from the Hub menu button. Built at
    /// runtime on its own top-most canvas (no prefab wiring). Currently exposes
    /// a sound on/off toggle (persisted via Sfx.Muted) and a Close button.
    /// </summary>
    public sealed class SettingsOverlay : MonoBehaviour
    {
        private static bool _active;

        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color Panel = new Color(0.16f, 0.18f, 0.26f);
        private static readonly Color On = new Color(0.35f, 0.75f, 0.45f);
        private static readonly Color Off = new Color(0.55f, 0.40f, 0.40f);
        private static readonly Color Close = new Color(0.45f, 0.48f, 0.55f);

        private TMP_Text _titleLabel;
        private TMP_Text _soundLabel;
        private Image _soundBg;
        private TMP_Text _langLabel;

        public static void Show()
        {
            if (_active) return;
            _active = true;
            new GameObject("SettingsOverlay").AddComponent<SettingsOverlay>().Build();
        }

        private void OnDestroy() => _active = false;

        private void Build()
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
            dim.AddComponent<Image>().color = Dim;

            var panelGo = NewUI("Panel", root, out var panelRt);
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(740f, 640f);
            panelGo.AddComponent<Image>().color = Panel;

            _titleLabel = MakeTitle(panelRt);

            var soundBtn = MakeButton("Sound", panelRt, -190f, On, ToggleSound);
            _soundBg = soundBtn.GetComponent<Image>();
            _soundLabel = soundBtn.GetComponentInChildren<TMP_Text>();

            var langBtn = MakeButton("Language", panelRt, -340f, Close, ToggleLanguage);
            _langLabel = langBtn.GetComponentInChildren<TMP_Text>();

            MakeButton("Close", panelRt, -490f, Close, () => Destroy(gameObject))
                .GetComponentInChildren<TMP_Text>().name = "CloseLabel";

            RefreshTexts();
        }

        private void ToggleSound()
        {
            Sfx.Muted = !Sfx.Muted;
            RefreshTexts();
            Sfx.Play("place"); // audible confirmation when turning sound back on
        }

        private void ToggleLanguage()
        {
            Loc.Toggle();
            RefreshTexts();
        }

        private void RefreshTexts()
        {
            _titleLabel.text = Loc.T("ui.settings");
            bool on = !Sfx.Muted;
            _soundLabel.text = Loc.T(on ? "ui.sound_on" : "ui.sound_off");
            _soundBg.color = on ? On : Off;
            _langLabel.text = Loc.T("settings.language") + ": " + (Loc.Language == "zh" ? "中文" : "English");
            // Close button is the last child's label.
            foreach (var t in GetComponentsInChildren<TMP_Text>())
                if (t.name == "CloseLabel") t.text = Loc.T("ui.close");
        }

        private static TMP_Text MakeTitle(RectTransform parent)
        {
            var go = NewUI("Title", parent, out var rt);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(-60f, 160f);
            rt.anchoredPosition = new Vector2(0f, -40f);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = 52;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            return t;
        }

        private static Button MakeButton(string name, RectTransform parent,
            float y, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = NewUI(name, parent, out var rt);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(520f, 120f);
            rt.anchoredPosition = new Vector2(0f, y);
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var labelGo = NewUI("Label", rt, out var labelRt);
            Stretch(labelRt);
            var text = labelGo.AddComponent<TextMeshProUGUI>();
            text.fontSize = 38;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            return btn;
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
