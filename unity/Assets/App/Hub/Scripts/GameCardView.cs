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

        public void Bind(IGameModule module, Action<IGameModule> onTap)
        {
            _module = module;
            _onTap = onTap;
            _title.text = module.DisplayName;
            _multiplayerBadge?.SetActive((module.Capabilities & GameCapabilities.Multiplayer) != 0);
            _sameDeviceBadge?.SetActive((module.Capabilities & GameCapabilities.SameDevice) != 0);
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => _onTap?.Invoke(_module));
        }
    }
}
