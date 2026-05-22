using System.Collections;
using MiniGames.Games.BombSweep.AI;
using MiniGames.Games.BombSweep.Logic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniGames.App.Games
{
    /// <summary>
    /// Solo Bomb Sweep in its own scene. The module's StartSolo spawns a
    /// single player (no opponent), so for an actual game the scene builds a
    /// 2-player state directly: human is player 0, a heuristic AI is player 1.
    /// 13x11 arena, 8 Hz tick. D-pad (hold) to move, Bomb button to drop.
    /// </summary>
    public sealed class BombSweepSceneController : MonoBehaviour
    {
        [SerializeField] private RectTransform _boardGrid; // GridLayoutGroup
        [SerializeField] private TMP_Text _status;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private HoldButton _upButton;
        [SerializeField] private HoldButton _downButton;
        [SerializeField] private HoldButton _leftButton;
        [SerializeField] private HoldButton _rightButton;
        [SerializeField] private Button _bombButton;

        private const float TickHz = 8f;

        private static readonly Color Floor = new Color(0.42f, 0.46f, 0.40f);
        private static readonly Color HardWall = new Color(0.22f, 0.23f, 0.27f);
        private static readonly Color SoftBlock = new Color(0.55f, 0.40f, 0.28f);
        private static readonly Color PUBombs = new Color(0.40f, 0.60f, 0.95f);
        private static readonly Color PURange = new Color(0.95f, 0.60f, 0.30f);
        private static readonly Color PUSpeed = new Color(0.40f, 0.85f, 0.50f);
        private static readonly Color BombCol = new Color(0.10f, 0.10f, 0.12f);
        private static readonly Color Blast = new Color(1.00f, 0.62f, 0.20f);
        private static readonly Color[] PlayerCol =
        {
            new Color(0.35f, 0.60f, 0.98f), // P0 human blue
            new Color(0.95f, 0.38f, 0.38f), // P1 cpu red
            new Color(0.45f, 0.85f, 0.50f),
            new Color(0.95f, 0.85f, 0.35f),
        };

        private BombSweepGameState _state;
        private CpuBombSweepController _cpu;
        private Image[,] _cells;
        private int _w, _h;
        private bool _over;
        private bool _bombQueued;

        private void Start()
        {
            _state = new BombSweepGameState(playerCount: 2,
                seed: Random.Range(int.MinValue, int.MaxValue));
            _cpu = new CpuBombSweepController(_state, 1, new SimpleBombSweepAI());
            _w = _state.Board.Width;
            _h = _state.Board.Height;

            BuildCells();
            if (_backButton != null) _backButton.onClick.AddListener(() => SceneManager.LoadScene("Hub"));
            if (_restartButton != null) _restartButton.onClick.AddListener(() => SceneManager.LoadScene("BombSweep"));
            if (_bombButton != null) _bombButton.onClick.AddListener(() => _bombQueued = true);
            Loc.Label(_backButton, "ui.back");
            InstructionsOverlay.AttachButton((RectTransform)transform, "bomb_sweep");
            Loc.Label(_restartButton, "ui.restart");
            Loc.Label(_bombButton, "ui.bomb");
            if (_upButton != null) Loc.Label(_upButton.GetComponent<Button>(), "ui.up");
            if (_downButton != null) Loc.Label(_downButton.GetComponent<Button>(), "ui.down");
            if (_leftButton != null) Loc.Label(_leftButton.GetComponent<Button>(), "ui.left");
            if (_rightButton != null) Loc.Label(_rightButton.GetComponent<Button>(), "ui.right");

            Render();
            StartCoroutine(TickLoop());
        }

        private void Update()
        {
            if (_over) return;
            if (Input.GetKeyDown(KeyCode.Space)) _bombQueued = true;
        }

        private IEnumerator TickLoop()
        {
            var wait = new WaitForSeconds(1f / TickHz);
            while (!_over)
            {
                yield return wait;
                if (_over) break;

                _state.SetInput(0, CurrentHeading(), _bombQueued);
                _bombQueued = false;
                _cpu.BeforeTick();

                var res = BombSweepEngine.Step(_state);
                Render();
                if (res.MatchOver) { _over = true; Render(); }
            }
        }

        private BombDir CurrentHeading()
        {
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W) || Held(_upButton)) return BombDir.Up;
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S) || Held(_downButton)) return BombDir.Down;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) || Held(_leftButton)) return BombDir.Left;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) || Held(_rightButton)) return BombDir.Right;
            return BombDir.None;
        }

        private static bool Held(HoldButton b) => b != null && b.IsHeld;

        private void BuildCells()
        {
            _cells = new Image[_w, _h];
            for (int row = 0; row < _h; row++)
            {
                int y = _h - 1 - row; // top UI row is the highest y
                for (int x = 0; x < _w; x++)
                {
                    var go = new GameObject($"C_{x}_{y}", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(_boardGrid, false);
                    var img = go.GetComponent<Image>();
                    img.color = Floor;
                    Shapes.Rounded(img);
                    _cells[x, y] = img;
                }
            }
        }

        private void Render()
        {
            for (int y = 0; y < _h; y++)
                for (int x = 0; x < _w; x++)
                    _cells[x, y].color = CellColor(_state.Board.Get(x, y));

            foreach (var b in _state.Bombs)
                if (In(b.Pos.X, b.Pos.Y)) _cells[b.Pos.X, b.Pos.Y].color = BombCol;

            foreach (var e in _state.Explosions)
                foreach (var c in e.Cells)
                    if (In(c.X, c.Y)) _cells[c.X, c.Y].color = Blast;

            foreach (var p in _state.Players)
                if (p.IsAlive && In(p.Pos.X, p.Pos.Y))
                    _cells[p.Pos.X, p.Pos.Y].color = PlayerCol[p.Index % PlayerCol.Length];

            if (_status != null) _status.text = StatusText();
            if (_over)
            {
                bool you = _state.Players[0].IsAlive, cpu = _state.Players[1].IsAlive;
                GameOverlay.Show(StatusText(),
                    you && !cpu ? GameOverlay.Outcome.Win
                    : !you ? GameOverlay.Outcome.Lose
                    : GameOverlay.Outcome.Neutral);
            }
        }

        private bool In(int x, int y) => x >= 0 && y >= 0 && x < _w && y < _h;

        private static Color CellColor(CellType c) => c switch
        {
            CellType.HardWall => HardWall,
            CellType.SoftBlock => SoftBlock,
            CellType.PowerBombs => PUBombs,
            CellType.PowerRange => PURange,
            CellType.PowerSpeed => PUSpeed,
            _ => Floor,
        };

        private string StatusText()
        {
            bool you = _state.Players[0].IsAlive;
            bool cpu = _state.Players[1].IsAlive;
            if (_over)
            {
                if (you && !cpu) return Loc.T("result.you_win");
                if (!you && cpu) return Loc.T("result.you_lose");
                return Loc.T("result.draw");
            }
            return Loc.T("ig.bomb_vs");
        }
    }
}
