using MiniGames.App.Bootstrap;
using MiniGames.GameModule;
using MiniGames.Games.FruitMerge;
using MiniGames.Games.FruitMerge.Logic;
using MiniGames.Networking.Session;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniGames.App.Games
{
    /// <summary>
    /// Solo Fruit Merge in its own scene. Single player vs the board: tap a
    /// column to drop the next fruit; equal tiers merge and chain. The 7x12
    /// grid is built at runtime; tap any cell in a column to drop there.
    /// </summary>
    public sealed class FruitMergeSceneController : MonoBehaviour
    {
        [SerializeField] private RectTransform _boardGrid; // GridLayoutGroup, 7 columns
        [SerializeField] private TMP_Text _status;
        [SerializeField] private Image _nextSwatch;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _restartButton;

        private static readonly Color SlotEmpty = new Color(0.16f, 0.15f, 0.18f);
        private static readonly Color[] TierColor =
        {
            new Color(0.16f, 0.15f, 0.18f),  // 0 empty
            new Color(0.90f, 0.27f, 0.30f),  // 1 cherry
            new Color(0.95f, 0.55f, 0.22f),  // 2 orange
            new Color(0.96f, 0.83f, 0.27f),  // 3 lemon
            new Color(0.62f, 0.84f, 0.30f),  // 4 lime
            new Color(0.33f, 0.78f, 0.42f),  // 5 green
            new Color(0.28f, 0.80f, 0.72f),  // 6 teal
            new Color(0.33f, 0.62f, 0.93f),  // 7 blue
            new Color(0.45f, 0.45f, 0.92f),  // 8 indigo
            new Color(0.65f, 0.40f, 0.90f),  // 9 purple
            new Color(0.90f, 0.40f, 0.80f),  // 10 magenta
            new Color(0.95f, 0.80f, 0.35f),  // 11 gold (max)
        };

        private FruitMergeModule _module;
        private FruitMergeGame _game;
        private Image[,] _cells;
        private TMP_Text[,] _labels;
        private int _w, _h;

        private void Start()
        {
            _module = new FruitMergeModule();
            _module.StartSolo(BuildContext());
            _game = _module.SoloGame;
            _w = _game.Grid.Width;
            _h = _game.Grid.Height;

            BuildCells();
            if (_backButton != null) _backButton.onClick.AddListener(() => SceneManager.LoadScene("Hub"));
            if (_restartButton != null) _restartButton.onClick.AddListener(() => SceneManager.LoadScene("FruitMerge"));

            _game.Dropped += _ => Render();
            Render();
        }

        private static GameContext BuildContext()
        {
            if (AppBootstrap.Services != null)
                return AppBootstrap.BuildContext(NullSendChannel.Instance);
            return new GameContext(null, null, null, null, NullSendChannel.Instance, "local");
        }

        private void BuildCells()
        {
            _cells = new Image[_w, _h];
            _labels = new TMP_Text[_w, _h];
            for (int row = 0; row < _h; row++)
            {
                int y = _h - 1 - row; // top UI row is the highest y
                for (int x = 0; x < _w; x++)
                {
                    int col = x;
                    var cellGo = new GameObject($"C_{x}_{y}", typeof(RectTransform), typeof(Image), typeof(Button));
                    cellGo.transform.SetParent(_boardGrid, false);
                    var img = cellGo.GetComponent<Image>();
                    img.color = SlotEmpty;
                    var btn = cellGo.GetComponent<Button>();
                    btn.targetGraphic = img;
                    btn.onClick.AddListener(() => OnColumnTapped(col));

                    var labelGo = new GameObject("L", typeof(RectTransform));
                    labelGo.transform.SetParent(cellGo.transform, false);
                    var lrt = labelGo.GetComponent<RectTransform>();
                    lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                    lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
                    var tmp = labelGo.AddComponent<TextMeshProUGUI>();
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMin = 12; tmp.fontSizeMax = 48;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.raycastTarget = false;
                    tmp.text = "";

                    _cells[x, y] = img;
                    _labels[x, y] = tmp;
                }
            }
        }

        private void OnColumnTapped(int column)
        {
            if (_game.IsGameOver) return;
            _game.TryDrop(column);
            Render();
        }

        private void Render()
        {
            for (int y = 0; y < _h; y++)
                for (int x = 0; x < _w; x++)
                {
                    byte t = _game.Grid.Get(x, y);
                    _cells[x, y].color = ColorFor(t);
                    _labels[x, y].text = t == 0 ? "" : t.ToString();
                    _labels[x, y].color = new Color(0.1f, 0.1f, 0.12f);
                }

            if (_nextSwatch != null) _nextSwatch.color = ColorFor(_game.NextFruit);
            if (_status != null) _status.text = StatusText();
        }

        private static Color ColorFor(byte tier)
            => tier < TierColor.Length ? TierColor[tier] : TierColor[TierColor.Length - 1];

        private string StatusText()
        {
            if (_game.IsGameOver) return $"Game Over   Score {_game.Score}";
            return $"Score {_game.Score}   Best {_game.HighestTier}";
        }
    }
}
