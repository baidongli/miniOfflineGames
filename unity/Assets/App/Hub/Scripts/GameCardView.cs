using System;
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
    }
}
