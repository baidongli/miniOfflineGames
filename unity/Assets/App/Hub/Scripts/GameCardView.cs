using System;
using MiniGames.App.Games;
using MiniGames.GameModule;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGames.App.Hub
{
    /// <summary>
    /// One tile in the game grid. Pure view: receives an IGameModule, renders
    /// title/icon, calls back when tapped. Visual polish (animations, hover
    /// states) belongs here.
    /// </summary>
    public sealed class GameCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Image _icon;
        [SerializeField] private Button _button;
        [SerializeField] private GameObject _multiplayerBadge;
        [SerializeField] private GameObject _sameDeviceBadge;

        private IGameModule _module;
        private Action<IGameModule> _onTap;
        private TMP_Text _glyphLabel;
        private TMP_Text _bestLabel;

        public void Bind(IGameModule module, Action<IGameModule> onTap)
        {
            _module = module;
            _onTap = onTap;
            _title.text = module.DisplayName;

            var visual = GameVisuals.For(module.Id);
            if (_icon != null)
            {
                _icon.color = visual.Color;
                EnsureGlyph().text = visual.Glyph;
            }

            // Personal best (score games only; turn-based games never store one).
            int best = BestScores.Get(module.Id);
            EnsureBest().text = best > 0 ? $"Best {best}" : "";

            _multiplayerBadge?.SetActive((module.Capabilities & GameCapabilities.Multiplayer) != 0);
            _sameDeviceBadge?.SetActive((module.Capabilities & GameCapabilities.SameDevice) != 0);
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => _onTap?.Invoke(_module));
        }

        // Creates (once) a centered glyph label on top of the icon so cards
        // read at a glance. Runtime-built so the existing prefab needs no edit.
        private TMP_Text EnsureGlyph()
        {
            if (_glyphLabel != null) return _glyphLabel;
            var go = new GameObject("Glyph", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(_icon.transform, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            _glyphLabel = go.AddComponent<TextMeshProUGUI>();
            _glyphLabel.alignment = TextAlignmentOptions.Center;
            _glyphLabel.enableAutoSizing = true;
            _glyphLabel.fontSizeMin = 18;
            _glyphLabel.fontSizeMax = 90;
            _glyphLabel.fontStyle = FontStyles.Bold;
            _glyphLabel.color = new Color(1f, 1f, 1f, 0.92f);
            _glyphLabel.raycastTarget = false;
            return _glyphLabel;
        }

        // Small "Best N" strip across the top of the card, created once.
        private TMP_Text EnsureBest()
        {
            if (_bestLabel != null) return _bestLabel;
            var go = new GameObject("Best", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.anchorMin = new Vector2(0f, 0.90f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            _bestLabel = go.AddComponent<TextMeshProUGUI>();
            _bestLabel.alignment = TextAlignmentOptions.Center;
            _bestLabel.fontSize = 22;
            _bestLabel.enableAutoSizing = true;
            _bestLabel.fontSizeMin = 12;
            _bestLabel.fontSizeMax = 26;
            _bestLabel.color = new Color(1f, 1f, 1f, 0.85f);
            _bestLabel.raycastTarget = false;
            return _bestLabel;
        }
    }
}
