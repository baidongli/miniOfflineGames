using System.Collections;
using MiniGames.App.Bootstrap;
using MiniGames.GameModule;
using MiniGames.Games.Snakes;
using MiniGames.Games.Snakes.Logic;
using MiniGames.Networking.Session;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniGames.App.Games
{
    /// <summary>
    /// Solo Snakes in its own scene. One snake on a 20x20 grid; eat food to
    /// grow, don't hit the wall or yourself. Swipe / arrow keys to steer. The
    /// grid of cells is built once at runtime and repainted every tick.
    /// </summary>
    public sealed class SnakesSceneController : MonoBehaviour
    {
        [SerializeField] private RectTransform _boardGrid; // needs a GridLayoutGroup
        [SerializeField] private TMP_Text _status;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _restartButton;

        private const float TickHz = 8f;
        private const float SwipeThreshold = 40f;

        private static readonly Color Empty = new Color(0.12f, 0.13f, 0.16f);
        private static readonly Color Body = new Color(0.30f, 0.70f, 0.40f);
        private static readonly Color Head = new Color(0.50f, 0.92f, 0.58f);
        private static readonly Color Food = new Color(0.92f, 0.40f, 0.40f);

        private SnakesModule _module;
        private SnakesGameState _state;
        private Image[,] _cells;
        private int _w, _h;
        private bool _over;

        private bool _dragging;
        private Vector2 _dragStart;

        private void Start()
        {
            _module = new SnakesModule();
            _module.StartSolo(BuildContext());
            _state = _module.SoloState;
            _w = _state.Width;
            _h = _state.Height;

            BuildCells();
            if (_backButton != null)
                _backButton.onClick.AddListener(() => SceneManager.LoadScene("Hub"));
            if (_restartButton != null)
                _restartButton.onClick.AddListener(() => SceneManager.LoadScene("Snakes"));

            Render();
            StartCoroutine(TickLoop());
        }

        private static GameContext BuildContext()
        {
            if (AppBootstrap.Services != null)
                return AppBootstrap.BuildContext(NullSendChannel.Instance);
            return new GameContext(null, null, null, null, NullSendChannel.Instance, "local");
        }

        private void Update()
        {
            if (_over) return;

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) Steer(Direction.Up);
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) Steer(Direction.Down);
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Steer(Direction.Left);
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Steer(Direction.Right);

            if (Input.GetMouseButtonDown(0)) { _dragging = true; _dragStart = Input.mousePosition; }
            else if (Input.GetMouseButtonUp(0) && _dragging)
            {
                _dragging = false;
                Vector2 delta = (Vector2)Input.mousePosition - _dragStart;
                if (delta.magnitude < SwipeThreshold) return;
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    Steer(delta.x > 0 ? Direction.Right : Direction.Left);
                else
                    Steer(delta.y > 0 ? Direction.Up : Direction.Down);
            }
        }

        private void Steer(Direction d) => _state.SetInput(0, d);

        private IEnumerator TickLoop()
        {
            var wait = new WaitForSeconds(1f / TickHz);
            while (!_over)
            {
                yield return wait;
                if (_over) break;
                var res = SnakesEngine.Step(_state);
                Render();
                if (res.MatchOver) { _over = true; Render(); }
            }
        }

        private void BuildCells()
        {
            _cells = new Image[_w, _h];
            for (int row = 0; row < _h; row++)
            {
                int y = _h - 1 - row; // top UI row is the highest y (Direction.Up = +y)
                for (int x = 0; x < _w; x++)
                {
                    var go = new GameObject($"C_{x}_{y}", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(_boardGrid, false);
                    var img = go.GetComponent<Image>();
                    img.color = Empty;
                    _cells[x, y] = img;
                }
            }
        }

        private void Render()
        {
            for (int y = 0; y < _h; y++)
                for (int x = 0; x < _w; x++)
                    _cells[x, y].color = Empty;

            foreach (var f in _state.Food)
                if (InBounds(f.X, f.Y)) _cells[f.X, f.Y].color = Food;

            var snake = _state.Snakes[0];
            bool first = true;
            foreach (var p in snake.Body)
            {
                if (InBounds(p.X, p.Y))
                    _cells[p.X, p.Y].color = first ? Head : Body;
                first = false;
            }

            if (_status != null) _status.text = StatusText(snake);
            if (_over) GameOverlay.Show(StatusText(snake));
        }

        private bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < _w && y < _h;

        private string StatusText(SnakeState snake)
        {
            int score = snake.FoodEaten;
            if (_over) return $"Game Over   Score {score}";
            return $"Score {score}";
        }
    }
}
