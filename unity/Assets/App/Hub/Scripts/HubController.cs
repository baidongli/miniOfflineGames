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
        }

        private void BuildGameGrid()
        {
            foreach (var module in GameRegistry.All)
            {
                var card = Instantiate(_cardPrefab, _gameGrid);
                card.Bind(module, OnGameCardTapped);
                _cards.Add(card);
            }
        }

        private void OnGameCardTapped(IGameModule module)
        {
            if (_modeSelect != null) _modeSelect.Show(module);
            else Debug.Log($"[Hub] selected {module.Id} (no mode-select wired)");
        }

        private void OnModeChosen(IGameModule module, GameMode mode)
        {
            _modeSelect.gameObject.SetActive(false);

            // Games wired into a solo scene so far. Everything else logs until
            // its scene exists.
            if (mode == GameMode.Solo)
            {
                switch (module.Id)
                {
                    case "connect_four": SceneManager.LoadScene("ConnectFour"); return;
                    case "reversi":      SceneManager.LoadScene("Reversi");     return;
                    case "number_merge":   SceneManager.LoadScene("NumberMerge");   return;
                    case "dots_and_boxes": SceneManager.LoadScene("DotsAndBoxes"); return;
                    case "snakes":         SceneManager.LoadScene("Snakes");       return;
                    case "tetris":         SceneManager.LoadScene("Tetris");       return;
                    case "fruit_merge":    SceneManager.LoadScene("FruitMerge");   return;
                    case "bomb_sweep":     SceneManager.LoadScene("BombSweep");     return;
                }
            }

            Debug.Log($"[Hub] {module.DisplayName} / {mode} not implemented yet");
        }

        private void OnMenu()
        {
            // TODO: settings / about / restore purchases
        }

        private void OnRemoveAds()
        {
            // TODO: trigger IAP flow
        }
    }
}
