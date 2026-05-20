using System.Collections.Generic;
using MiniGames.App.Bootstrap;
using MiniGames.GameModule;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGames.App.Hub
{
    /// <summary>
    /// Top-level Hub screen: shows the game grid, energy bar, remove-ads CTA.
    /// View references are wired in the Inspector. This script handles input
    /// and delegates navigation to AppNavigator (TBD).
    /// </summary>
    public sealed class HubController : MonoBehaviour
    {
        [Header("View refs")]
        [SerializeField] private RectTransform _gameGrid;
        [SerializeField] private GameCardView _cardPrefab;
        [SerializeField] private Button _menuButton;
        [SerializeField] private Button _removeAdsButton;
        [SerializeField] private EnergyBarView _energyBar;

        private readonly List<GameCardView> _cards = new List<GameCardView>();

        private void Start()
        {
            BuildGameGrid();
            _menuButton.onClick.AddListener(OnMenu);
            _removeAdsButton.onClick.AddListener(OnRemoveAds);
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
            Debug.Log($"[Hub] selected {module.Id}");
            // TODO: route to GameModeSelectScreen (solo / multiplayer / same-device)
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
