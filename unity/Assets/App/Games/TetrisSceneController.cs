using MiniGames.App.Bootstrap;
using MiniGames.GameModule;
using MiniGames.Games.Tetris;
using MiniGames.Games.Tetris.Logic;
using MiniGames.Networking.Session;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniGames.App.Games
{
    /// <summary>
    /// Solo Tetris in its own scene. Renders the 10x20 visible board plus the
    /// falling piece, advanced by a gravity timer. Left/Right/Soft-drop use
    /// hold-to-repeat buttons (or arrow keys); Rotate and Hard-drop are taps
    /// (Up / Space).
    /// </summary>
    public sealed class TetrisSceneController : MonoBehaviour
    {
        [SerializeField] private RectTransform _boardGrid; // GridLayoutGroup, 10 columns
        [SerializeField] private TMP_Text _status;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private HoldButton _leftButton;
        [SerializeField] private HoldButton _rightButton;
        [SerializeField] private HoldButton _softButton;
        [SerializeField] private Button _rotateButton;
        [SerializeField] private Button _hardButton;

        private const int W = TetrisBoard.Width;          // 10
        private const int H = TetrisBoard.VisibleHeight;  // 20
        private const float RepeatDelay = 0.16f;
        private const float RepeatRate = 0.05f;

        private static readonly Color Empty = new Color(0.12f, 0.13f, 0.17f);
        private static readonly Color[] PieceColor =
        {
            new Color(0.12f, 0.13f, 0.17f),  // 0 none
            new Color(0.30f, 0.80f, 0.85f),  // I cyan
            new Color(0.92f, 0.84f, 0.30f),  // O yellow
            new Color(0.70f, 0.40f, 0.85f),  // T purple
            new Color(0.40f, 0.78f, 0.42f),  // S green
            new Color(0.90f, 0.36f, 0.36f),  // Z red
            new Color(0.35f, 0.52f, 0.92f),  // J blue
            new Color(0.93f, 0.60f, 0.27f),  // L orange
        };

        private TetrisModule _module;
        private TetrisGame _game;
        private Image[,] _cells;
        private bool _over;

        private float _gravityTimer;
        private float _leftTimer, _rightTimer, _softTimer;

        private void Start()
        {
            _module = new TetrisModule();
            _module.StartSolo(BuildContext());
            _game = _module.SoloGame;

            BuildCells();
            if (_backButton != null) _backButton.onClick.AddListener(() => SceneManager.LoadScene("Hub"));
            if (_restartButton != null) _restartButton.onClick.AddListener(() => SceneManager.LoadScene("Tetris"));
            if (_rotateButton != null) _rotateButton.onClick.AddListener(() => Act(() => _game.TryRotate(1)));
            if (_hardButton != null) _hardButton.onClick.AddListener(() => Act(() => { _game.HardDrop(); return true; }));

            _game.Locked += OnLocked;
            _gravityTimer = ScoringRules.GravitySeconds(_game.Level);
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
            if (_over) return;

            // Edge-triggered taps.
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) Act(() => _game.TryRotate(1));
            if (Input.GetKeyDown(KeyCode.Space)) Act(() => { _game.HardDrop(); return true; });

            // Held / repeatable.
            bool left = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) || Held(_leftButton);
            bool right = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) || Held(_rightButton);
            bool soft = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S) || Held(_softButton);

            if (Repeat(left, ref _leftTimer)) Act(() => _game.TryMoveLeft());
            if (Repeat(right, ref _rightTimer)) Act(() => _game.TryMoveRight());
            if (Repeat(soft, ref _softTimer)) Act(() => _game.TrySoftDrop());

            // Gravity.
            _gravityTimer -= Time.deltaTime;
            if (_gravityTimer <= 0f)
            {
                _game.Tick();
                _gravityTimer = ScoringRules.GravitySeconds(_game.Level);
                AfterStateChange();
            }
        }

        private void OnLocked(LockResult lr)
        {
            if (lr.LinesCleared > 0)
            {
                Sfx.Play("clear");
                UiTween.Pop(_boardGrid, 0.94f, 0.14f); // whole-board "thump" on a clear
            }
            else
            {
                Sfx.Play("place");
            }
        }

        private static bool Held(HoldButton b) => b != null && b.IsHeld;

        private static bool Repeat(bool active, ref float timer)
        {
            if (!active) { timer = 0f; return false; }
            if (timer == 0f) { timer = RepeatDelay; return true; } // fresh press fires now
            timer -= Time.deltaTime;
            if (timer <= 0f) { timer = RepeatRate; return true; }
            return false;
        }

        private void Act(System.Func<bool> move)
        {
            if (_over) return;
            move();
            AfterStateChange();
        }

        private void AfterStateChange()
        {
            if (_game.IsGameOver) _over = true;
            Render();
        }

        private void BuildCells()
        {
            _cells = new Image[W, H];
            for (int row = 0; row < H; row++)
            {
                int y = H - 1 - row; // top UI row is the highest y
                for (int x = 0; x < W; x++)
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
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    byte v = _game.Board.Get(x, y);
                    _cells[x, y].color = ColorFor(v);
                }

            // Overlay the active piece (cells in the visible range).
            if (!_over)
            {
                var cells = TetrominoShapes.Cells(_game.Current, _game.Rotation);
                var col = ColorFor((byte)_game.Current);
                foreach (var (cx, cy) in cells)
                {
                    int bx = _game.X + cx, by = _game.Y + cy;
                    if (bx >= 0 && bx < W && by >= 0 && by < H) _cells[bx, by].color = col;
                }
            }

            if (_status != null) _status.text = StatusText();
            if (_over) GameOverlay.Show(StatusText());
        }

        private static Color ColorFor(byte v)
            => v < PieceColor.Length ? PieceColor[v] : Color.gray;

        private string StatusText()
        {
            if (_over) return $"Game Over   Score {_game.Score}   Lines {_game.Lines}";
            return $"Score {_game.Score}   Lines {_game.Lines}   Next {_game.Next}";
        }
    }
}
