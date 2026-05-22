using MiniGames.App.Bootstrap;
using MiniGames.GameModule;
using MiniGames.Games.NumberMerge;
using MiniGames.Games.NumberMerge.Logic;
using MiniGames.Networking.Session;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniGames.App.Games
{
    /// <summary>
    /// Solo 2048 in its own scene. Single player vs the board: swipe (or use
    /// arrow keys / mouse drag) to slide and merge tiles. The 4x4 grid is
    /// built at runtime under _boardGrid. No AI opponent.
    /// </summary>
    public sealed class NumberMergeSceneController : MonoBehaviour
    {
        [SerializeField] private RectTransform _boardGrid; // needs a GridLayoutGroup, 4 columns
        [SerializeField] private TMP_Text _status;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _newGameButton;

        private const int N = NumberMergeBoard.Size; // 4
        private const float SwipeThreshold = 40f;

        private static readonly Color SlotEmpty = new Color(0.20f, 0.19f, 0.18f);

        private NumberMergeModule _module;
        private NumberMergeGame _game;
        private Image[,] _cells;
        private TMP_Text[,] _labels;

        private bool _dragging;
        private Vector2 _dragStart;

        private void Start()
        {
            _module = new NumberMergeModule();
            _module.StartSolo(BuildContext());
            _game = _module.SoloGame;

            BuildCells();
            if (_backButton != null)
                _backButton.onClick.AddListener(() => SceneManager.LoadScene("Hub"));
            if (_newGameButton != null)
                _newGameButton.onClick.AddListener(() => SceneManager.LoadScene("NumberMerge"));

            _game.Swiped += _ => Render();
            Render();
        }

        private static GameContext BuildContext()
        {
            if (AppBootstrap.Services != null)
                return AppBootstrap.BuildContext(NullSendChannel.Instance);
            return new GameContext(null, null, null, null, NullSendChannel.Instance, "local");
        }

        private void Update()
        {
            if (_game == null || _game.IsGameOver) return;

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) DoSwipe(SwipeDir.Up);
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) DoSwipe(SwipeDir.Down);
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) DoSwipe(SwipeDir.Left);
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) DoSwipe(SwipeDir.Right);

            if (Input.GetMouseButtonDown(0)) { _dragging = true; _dragStart = Input.mousePosition; }
            else if (Input.GetMouseButtonUp(0) && _dragging)
            {
                _dragging = false;
                Vector2 delta = (Vector2)Input.mousePosition - _dragStart;
                if (delta.magnitude < SwipeThreshold) return;
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    DoSwipe(delta.x > 0 ? SwipeDir.Right : SwipeDir.Left);
                else
                    DoSwipe(delta.y > 0 ? SwipeDir.Up : SwipeDir.Down);
            }
        }

        private void DoSwipe(SwipeDir dir)
        {
            _game.TrySwipe(dir, out _);
            Render();
        }

        private void BuildCells()
        {
            _cells = new Image[N, N];
            _labels = new TMP_Text[N, N];
            // Top UI row is the highest y so swipe-up moves tiles toward the top.
            for (int row = 0; row < N; row++)
            {
                int y = N - 1 - row;
                for (int x = 0; x < N; x++)
                {
                    var cellGo = new GameObject($"Cell_{x}_{y}", typeof(RectTransform), typeof(Image));
                    cellGo.transform.SetParent(_boardGrid, false);
                    var img = cellGo.GetComponent<Image>();
                    img.color = SlotEmpty;

                    var labelGo = new GameObject("Label", typeof(RectTransform));
                    labelGo.transform.SetParent(cellGo.transform, false);
                    var lrt = labelGo.GetComponent<RectTransform>();
                    lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                    lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
                    var tmp = labelGo.AddComponent<TextMeshProUGUI>();
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMin = 16; tmp.fontSizeMax = 90;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.text = "";

                    _cells[x, y] = img;
                    _labels[x, y] = tmp;
                }
            }
        }

        private void Render()
        {
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    byte exp = _game.Board.Get(x, y);
                    _cells[x, y].color = TileColor(exp);
                    _labels[x, y].text = exp == 0 ? "" : _game.Board.Value(x, y).ToString();
                    _labels[x, y].color = exp <= 2
                        ? new Color(0.30f, 0.28f, 0.25f)
                        : Color.white;
                }
            if (_status != null) _status.text = StatusText();
        }

        private string StatusText()
        {
            int best = _game.MaxExponent == 0 ? 0 : 1 << _game.MaxExponent;
            if (_game.IsGameOver) return $"Game Over   Score {_game.Score}   Best {best}";
            if (_game.ReachedGoal) return $"You reached 2048!   Score {_game.Score}";
            return $"Score {_game.Score}   Best {best}";
        }

        private static Color TileColor(byte exp)
        {
            switch (exp)
            {
                case 0:  return SlotEmpty;
                case 1:  return new Color(0.93f, 0.89f, 0.85f);
                case 2:  return new Color(0.93f, 0.88f, 0.78f);
                case 3:  return new Color(0.95f, 0.69f, 0.47f);
                case 4:  return new Color(0.96f, 0.58f, 0.39f);
                case 5:  return new Color(0.96f, 0.49f, 0.37f);
                case 6:  return new Color(0.96f, 0.37f, 0.23f);
                case 7:  return new Color(0.93f, 0.81f, 0.45f);
                case 8:  return new Color(0.93f, 0.80f, 0.38f);
                case 9:  return new Color(0.93f, 0.78f, 0.31f);
                case 10: return new Color(0.93f, 0.77f, 0.25f);
                default: return new Color(0.93f, 0.76f, 0.18f);
            }
        }
    }
}
