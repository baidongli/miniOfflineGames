using System.Collections.Generic;
using MiniGames.App.Bootstrap;
using MiniGames.GameModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniGames.App.Hub
{
    /// <summary>
    /// Top-level Hub screen: shows the game grid, energy bar, remove-ads CTA.
    /// View references are wired in the Inspector. Tapping a card opens the
    /// mode-select modal; choosing a mode routes into the matching game scene.
    /// </summary>
    public sealed class HubController : MonoBehaviour
    {
        [Header("View refs")]
        [SerializeField] private RectTransform _gameGrid;
        [SerializeField] private GameCardView _cardPrefab;
        [SerializeField] private Button _menuButton;
        [SerializeField] private Button _removeAdsButton;
        [SerializeField] private EnergyBarView _energyBar;
        [SerializeField] private GameModeSelectController _modeSelect;

        private readonly List<GameCardView> _cards = new List<GameCardView>();

        private void Start()
        {
            BuildGameGrid();
            _menuButton.onClick.AddListener(OnMenu);
            _removeAdsButton.onClick.AddListener(OnRemoveAds);
            if (_modeSelect != null)
            {
                _modeSelect.gameObject.SetActive(false);
                _modeSelect.ModeChosen += OnModeChosen;
            }
            RenderEnergy();
            RelocalizeChrome();
            MiniGames.App.Games.Loc.LanguageChanged += RelocalizeChrome;
        }

        private void OnDestroy() => MiniGames.App.Games.Loc.LanguageChanged -= RelocalizeChrome;

        private void RelocalizeChrome()
        {
            SetButtonLabel(_menuButton, "ui.menu");
            SetButtonLabel(_removeAdsButton, "ui.remove_ads");
        }

        private static void SetButtonLabel(Button button, string key)
        {
            if (button == null) return;
            var label = button.GetComponentInChildren<TMPro.TMP_Text>();
            if (label != null) label.text = MiniGames.App.Games.Loc.T(key);
        }

        private void RenderEnergy()
        {
            if (_energyBar == null) return;
            var energy = AppBootstrap.Services?.Energy;
            if (energy != null)
                _energyBar.Render(energy.Current(System.DateTimeOffset.UtcNow), energy.Max);
            else
                _energyBar.Render(5, 5); // Boot didn't run (e.g. playing Hub directly)
        }

        private void BuildGameGrid()
        {
            foreach (var module in GameRegistry.All)
            {
                var card = Instantiate(_cardPrefab, _gameGrid);
                card.Bind(module, OnGameCardTapped);
                MiniGames.App.Games.UiTween.Pop(
                    (RectTransform)card.transform, 0.6f, 0.26f, _cards.Count * 0.04f);
                _cards.Add(card);
            }
        }

        private void OnGameCardTapped(IGameModule module)
        {
            if (_modeSelect != null) _modeSelect.Show(module);
            else Debug.Log($"[Hub] selected {module.Id} (no mode-select wired)");
        }

        // Games with a same-device (hot-seat) implementation in their scene.
        private static readonly HashSet<string> SameDeviceGames =
            new HashSet<string> { "connect_four", "reversi", "dots_and_boxes" };

        private void OnModeChosen(IGameModule module, GameMode mode)
        {
            _modeSelect.gameObject.SetActive(false);
            var scene = SceneFor(module.Id);

            if (mode == GameMode.Solo && scene != null)
            {
                MiniGames.App.Games.GameLaunch.SameDevice = false;
                SceneManager.LoadScene(scene);
                return;
            }
            if (mode == GameMode.SameDevice && scene != null && SameDeviceGames.Contains(module.Id))
            {
                MiniGames.App.Games.GameLaunch.SameDevice = true;
                SceneManager.LoadScene(scene);
                return;
            }

            Debug.Log($"[Hub] {module.DisplayName} / {mode} not implemented yet");
        }

        private static string SceneFor(string id) => id switch
        {
            "connect_four"   => "ConnectFour",
            "reversi"        => "Reversi",
            "number_merge"   => "NumberMerge",
            "dots_and_boxes" => "DotsAndBoxes",
            "snakes"         => "Snakes",
            "tetris"         => "Tetris",
            "fruit_merge"    => "FruitMerge",
            "bomb_sweep"     => "BombSweep",
            "color_blocks"   => "ColorBlocks",
            "maze_paint"     => "MazePaint",
            "battleship"     => "Battleship",
            _ => null
        };

        private void OnMenu() => SettingsOverlay.Show();

        private void OnRemoveAds()
        {
            // TODO: trigger IAP flow
        }
    }
}
