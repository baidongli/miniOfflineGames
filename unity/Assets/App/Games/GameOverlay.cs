using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniGames.App.Games
{
    /// <summary>
    /// Reusable end-of-game overlay. Any scene controller calls
    /// GameOverlay.Show("You win!") once the game ends; this builds a
    /// full-screen dimmed panel (its own top-most Canvas) with the result and
    /// Play Again / Home buttons, fading + popping in. Calls are de-duplicated
    /// so controllers can invoke it every frame without stacking overlays.
    /// </summary>
    public sealed class GameOverlay : MonoBehaviour
    {
        private static bool _active;

        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color Panel = new Color(0.16f, 0.18f, 0.26f);
        private static readonly Color Accent = new Color(0.30f, 0.55f, 0.95f);
        private static readonly Color Home = new Color(0.45f, 0.48f, 0.55f);

        private CanvasGroup _cg;
        private RectTransform _panel;

        public static void Show(string message)
        {
            if (_active) return;
            _active = true;
            var go = new GameObject("GameOverlay");
            go.AddComponent<GameOverlay>().Build(message);
        }

        private void OnDestroy() => _active = false;

        private void Build(string message)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            _cg = gameObject.AddComponent<CanvasGroup>();
            _cg.alpha = 0f;

            var root = (RectTransform)transform;

            var dim = NewUI("Dim", root, out var dimRt);
            Stretch(dimRt);
            dim.AddComponent<Image>().color = Dim;

            var panelGo = NewUI("Panel", root, out _panel);
            _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0.5f, 0.5f);
            _panel.sizeDelta = new Vector2(740f, 560f);
            panelGo.AddComponent<Image>().color = Panel;

            var titleGo = NewUI("Title", _panel, out var titleRt);
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(-60f, 220f);
            titleRt.anchoredPosition = new Vector2(0f, -40f);
            var title = titleGo.AddComponent<TextMeshProUGUI>();
            title.text = message;
            title.fontSize = 56;
            title.enableAutoSizing = true;
            title.fontSizeMin = 24;
            title.fontSizeMax = 64;
            title.alignment = TextAlignmentOptions.Center;
            title.color = Color.white;

            MakeButton("PlayAgain", _panel, "Play Again", -200f, Accent,
                () => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
            MakeButton("Home", _panel, "Home", -360f, Home,
                () => SceneManager.LoadScene("Hub"));

            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            const float dur = 0.28f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                _cg.alpha = k;
                float s = Mathf.Lerp(0.82f, 1f, EaseOut(k));
                _panel.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            _cg.alpha = 1f;
            _panel.localScale = Vector3.one;
        }

        private static float EaseOut(float x) => 1f - (1f - x) * (1f - x);

        private static void MakeButton(string name, RectTransform parent, string label,
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
            text.text = label;
            text.fontSize = 38;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
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
