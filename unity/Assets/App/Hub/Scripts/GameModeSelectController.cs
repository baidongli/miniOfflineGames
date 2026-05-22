using System;
using MiniGames.App.Games;
using MiniGames.GameModule;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGames.App.Hub
{
    /// <summary>
    /// Modal shown after tapping a game tile. Shows up to three buttons —
    /// Solo, Same device (1-4), Nearby (1-4) — depending on Capabilities.
    /// </summary>
    public sealed class GameModeSelectController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Button _soloButton;
        [SerializeField] private Button _sameDeviceButton;
        [SerializeField] private Button _nearbyButton;
        [SerializeField] private Button _closeButton;

        private IGameModule _module;
        public event Action<IGameModule, GameMode> ModeChosen;
        public event Action Closed;

        public void Show(IGameModule module)
        {
            _module = module;
            _title.text = Loc.T($"game.{module.Id}.title");
            SetLabel(_soloButton, "mode.solo");
            SetLabel(_sameDeviceButton, "mode.same_device");
            SetLabel(_nearbyButton, "mode.nearby_host");
            SetLabel(_closeButton, "ui.close");
            _soloButton.gameObject.SetActive((module.Capabilities & GameCapabilities.Solo) != 0);
            _sameDeviceButton.gameObject.SetActive((module.Capabilities & GameCapabilities.SameDevice) != 0);
            _nearbyButton.gameObject.SetActive((module.Capabilities & GameCapabilities.Multiplayer) != 0);
            gameObject.SetActive(true);
        }

        private static void SetLabel(Button button, string key)
        {
            if (button == null) return;
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = Loc.T(key);
        }

        private void Awake()
        {
            _soloButton.onClick.AddListener(() => ModeChosen?.Invoke(_module, GameMode.Solo));
            _sameDeviceButton.onClick.AddListener(() => ModeChosen?.Invoke(_module, GameMode.SameDevice));
            _nearbyButton.onClick.AddListener(() => ModeChosen?.Invoke(_module, GameMode.Nearby));
            _closeButton.onClick.AddListener(() => { gameObject.SetActive(false); Closed?.Invoke(); });
        }
    }

    public enum GameMode
    {
        Solo,
        SameDevice,
        Nearby
    }
}
