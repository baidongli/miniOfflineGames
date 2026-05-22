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
    /// Snakes, two modes:
    ///  - Solo: one snake, eat to grow, swipe / arrow keys, personal best.
    ///  - Same-device: two snakes on one 20x20 board. Player 1 = WASD + the
    ///    left D-pad, Player 2 = arrow keys + the right D-pad. Last snake
    ///    alive wins. The D-pads are built at runtime (no scene changes).
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
        private static readonly Color Food = new Color(0.92f, 0.40f, 0.40f);
        private static readonly Color[] BodyCol =
        {
            new Color(0.30f, 0.70f, 0.40f), // P1 green
            new Color(0.90f, 0.55f, 0.25f), // P2 orange
        };
        private static readonly Color[] HeadCol =
        {
            new Color(0.50f, 0.92f, 0.58f),
            new Color(0.98f, 0.74f, 0.42f),
        };

        private SnakesGameState _state;
        private Image[,] _cells;
        private int _w, _h;
        private bool _over;
        private bool _twoPlayer;
        private int _winner = -1;

        private bool _dragging;
        private Vector2 _dragStart;

        private void Start()
        {
            _twoPlayer = GameLaunch.SameDevice;
            if (_twoPlayer)
            {
                _state = new SnakesGameState(SnakesModule.BoardSize, SnakesModule.BoardSize,
                    playerCount: 2, seed: Random.Range(int.MinValue, int.MaxValue));
            }
            else
            {
                var module = new SnakesModule();
                module.StartSolo(BuildContext());
                _state = module.SoloState;
            }
            _w = _state.Width;
            _h = _state.Height;

            BuildCells();
            if (_twoPlayer) BuildDpads();
            if (_backButton != null)
                _backButton.onClick.AddListener(() => SceneManager.LoadScene("Hub"));
            if (_restartButton != null)
                _restartButton.onClick.AddListener(() => SceneManager.LoadScene("Snakes"));
            Loc.Label(_backButton, "ui.back");
            InstructionsOverlay.AttachButton((RectTransform)transform, "snakes");
            Art.StyleButtons((RectTransform)transform);
            Loc.Label(_restartButton, "ui.restart");

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

            if (_twoPlayer)
            {
                if (Input.GetKeyDown(KeyCode.W)) Steer(0, Direction.Up);
                else if (Input.GetKeyDown(KeyCode.S)) Steer(0, Direction.Down);
                else if (Input.GetKeyDown(KeyCode.A)) Steer(0, Direction.Left);
                else if (Input.GetKeyDown(KeyCode.D)) Steer(0, Direction.Right);

                if (Input.GetKeyDown(KeyCode.UpArrow)) Steer(1, Direction.Up);
                else if (Input.GetKeyDown(KeyCode.DownArrow)) Steer(1, Direction.Down);
                else if (Input.GetKeyDown(KeyCode.LeftArrow)) Steer(1, Direction.Left);
                else if (Input.GetKeyDown(KeyCode.RightArrow)) Steer(1, Direction.Right);
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) Steer(0, Direction.Up);
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) Steer(0, Direction.Down);
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Steer(0, Direction.Left);
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Steer(0, Direction.Right);

            if (Input.GetMouseButtonDown(0)) { _dragging = true; _dragStart = Input.mousePosition; }
            else if (Input.GetMouseButtonUp(0) && _dragging)
            {
                _dragging = false;
                Vector2 delta = (Vector2)Input.mousePosition - _dragStart;
                if (delta.magnitude < SwipeThreshold) return;
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    Steer(0, delta.x > 0 ? Direction.Right : Direction.Left);
                else
                    Steer(0, delta.y > 0 ? Direction.Up : Direction.Down);
            }
        }

        private void Steer(int player, Direction d) => _state.SetInput(player, d);

        private IEnumerator TickLoop()
        {
            var wait = new WaitForSeconds(1f / TickHz);
            while (!_over)
            {
                yield return wait;
                if (_over) break;
                var res = SnakesEngine.Step(_state);
                if (res.AteThisTick != null && res.AteThisTick.Count > 0) Sfx.Play("place");
                Render();
                if (res.MatchOver) { _winner = res.WinnerIndex ?? -1; _over = true; Render(); }
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
                    Shapes.Rounded(img);
                    _cells[x, y] = img;
                }
            }
        }

        // ---- two-player D-pads (runtime) ----

        private void BuildDpads()
        {
            BuildDpad(0, 300f, HeadCol[0]);   // left side, player 1
            BuildDpad(1, -300f, HeadCol[1]);  // right side, player 2
        }

        private void BuildDpad(int player, float baseX, Color tint)
        {
            float anchorX = baseX < 0 ? 1f : 0f;       // left dpad hugs left edge, right hugs right
            float ox = baseX < 0 ? -180f : 180f;
            MakeDir(player, Direction.Up, anchorX, ox, 250f, "▲", tint);
            MakeDir(player, Direction.Down, anchorX, ox, 90f, "▼", tint);
            MakeDir(player, Direction.Left, anchorX, ox - 130f, 170f, "◀", tint);
            MakeDir(player, Direction.Right, anchorX, ox + 130f, 170f, "▶", tint);
        }

        private void MakeDir(int player, Direction d, float anchorX, float x, float y, string glyph, Color tint)
        {
            var go = new GameObject($"P{player}_{d}", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(anchorX, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(120f, 120f);
            rt.anchoredPosition = new Vector2(x, y);
            var img = go.GetComponent<Image>();
            img.color = tint;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => Steer(player, d));

            var lgo = new GameObject("L", typeof(RectTransform));
            var lrt = (RectTransform)lgo.transform;
            lrt.SetParent(rt, false);
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var t = lgo.AddComponent<TextMeshProUGUI>();
            t.text = glyph;
            t.fontSize = 44;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            t.raycastTarget = false;
        }

        private void Render()
        {
            for (int y = 0; y < _h; y++)
                for (int x = 0; x < _w; x++)
                {
                    Shapes.Rounded(_cells[x, y]);
                    _cells[x, y].color = Empty;
                }

            foreach (var f in _state.Food)
                if (InBounds(f.X, f.Y) && !Art.TryApply(_cells[f.X, f.Y], "snakes", "food"))
                    _cells[f.X, f.Y].color = Food;

            for (int i = 0; i < _state.Snakes.Count; i++)
            {
                var snake = _state.Snakes[i];
                bool first = true;
                foreach (var p in snake.Body)
                {
                    if (InBounds(p.X, p.Y)) PaintSnake(_cells[p.X, p.Y], i, first);
                    first = false;
                }
            }

            if (_status != null) _status.text = StatusText();
            if (_over) ShowOver();
        }

        private void PaintSnake(Image cell, int player, bool head)
        {
            string name = head ? "head" : "body";
            if (_twoPlayer)
            {
                // Tint the art (or a rounded cell) so the two snakes are distinct.
                if (!Art.TryApply(cell, "snakes", name, keepColor: true)) Shapes.Rounded(cell);
                cell.color = head ? HeadCol[player] : BodyCol[player];
            }
            else if (!Art.TryApply(cell, "snakes", name))
            {
                Shapes.Rounded(cell);
                cell.color = head ? HeadCol[0] : BodyCol[0];
            }
        }

        private bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < _w && y < _h;

        private string StatusText()
        {
            if (_twoPlayer)
            {
                if (_over)
                    return _winner >= 0 ? Loc.T("ig.player_wins", _winner + 1) : Loc.T("result.draw");
                return $"P1  vs  P2";
            }
            int score = _state.Snakes[0].FoodEaten;
            if (_over) return $"{Loc.T("result.game_over")}   {Loc.T("ig.score")} {score}";
            return $"{Loc.T("ig.score")} {score}";
        }

        private void ShowOver()
        {
            if (_twoPlayer)
            {
                GameOverlay.Show(StatusText(),
                    _winner >= 0 ? GameOverlay.Outcome.Win : GameOverlay.Outcome.Neutral);
                return;
            }
            int score = _state.Snakes[0].FoodEaten;
            bool rec = BestScores.Report("snakes", score);
            GameOverlay.Show(StatusText() + BestScores.Suffix("snakes", rec));
        }
    }
}
