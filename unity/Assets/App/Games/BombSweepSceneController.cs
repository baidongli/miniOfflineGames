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
    /// Bomb Sweep, two modes (13x11 arena, 8 Hz):
    ///  - Solo: human (P0) vs a heuristic AI (P1).
    ///  - Same-device: two humans. P0 = baked left D-pad + WASD + a left bomb
    ///    button (Space); P1 = a runtime right D-pad + arrow keys + a right
    ///    bomb button (Enter). Last player alive wins.
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
            new Color(0.35f, 0.60f, 0.98f), // P0 blue
            new Color(0.95f, 0.38f, 0.38f), // P1 red
            new Color(0.45f, 0.85f, 0.50f),
            new Color(0.95f, 0.85f, 0.35f),
        };

        private BombSweepGameState _state;
        private CpuBombSweepController _cpu;
        private Image[,] _cells;
        private int _w, _h;
        private bool _over;
        private bool _twoPlayer;
        private int _winner = -1;
        private readonly bool[] _bombQ = new bool[2];
        private readonly HoldButton[] _p1dir = new HoldButton[4]; // Up,Down,Left,Right

        private void Start()
        {
            _twoPlayer = GameLaunch.SameDevice;
            _state = new BombSweepGameState(playerCount: 2,
                seed: Random.Range(int.MinValue, int.MaxValue));
            if (!_twoPlayer) _cpu = new CpuBombSweepController(_state, 1, new SimpleBombSweepAI());
            _w = _state.Board.Width;
            _h = _state.Board.Height;

            BuildCells();
            if (_backButton != null) _backButton.onClick.AddListener(() => SceneManager.LoadScene("Hub"));
            if (_restartButton != null) _restartButton.onClick.AddListener(() => SceneManager.LoadScene("BombSweep"));
            if (_bombButton != null) _bombButton.onClick.AddListener(() => _bombQ[0] = true);

            if (_twoPlayer) BuildSecondPlayerControls();

            Loc.Label(_backButton, "ui.back");
            InstructionsOverlay.AttachButton((RectTransform)transform, "bomb_sweep");
            Art.StyleButtons((RectTransform)transform);
            Loc.Label(_restartButton, "ui.restart");
            Loc.Label(_bombButton, "ui.bomb");
            if (_upButton != null) Loc.Label(_upButton.GetComponent<Button>(), "ui.up");
            if (_downButton != null) Loc.Label(_downButton.GetComponent<Button>(), "ui.down");
            if (_leftButton != null) Loc.Label(_leftButton.GetComponent<Button>(), "ui.left");
            if (_rightButton != null) Loc.Label(_rightButton.GetComponent<Button>(), "ui.right");

            Render();
            StartCoroutine(TickLoop());
        }

        // ---- two-player controls (runtime) ----

        private void BuildSecondPlayerControls()
        {
            // The baked bomb button sits bottom-right where P1's D-pad goes; hide it
            // and give P0 a bomb button on the left instead.
            if (_bombButton != null) _bombButton.gameObject.SetActive(false);

            MakeTap("P0_Bomb", -90f, 215f, 180f, Loc.T("ui.bomb"), PlayerCol[0], () => _bombQ[0] = true);

            _p1dir[0] = MakeHold("P1_Up", 300f, 320f, "▲");
            _p1dir[1] = MakeHold("P1_Down", 300f, 110f, "▼");
            _p1dir[2] = MakeHold("P1_Left", 170f, 215f, "◀");
            _p1dir[3] = MakeHold("P1_Right", 430f, 215f, "▶");
            MakeTap("P1_Bomb", 90f, 215f, 180f, Loc.T("ui.bomb"), PlayerCol[1], () => _bombQ[1] = true);
        }

        private HoldButton MakeHold(string name, float x, float y, string glyph)
        {
            var btn = MakeTap(name, x, y, 120f, glyph, PlayerCol[1], null);
            return btn.gameObject.AddComponent<HoldButton>();
        }

        private Button MakeTap(string name, float x, float y, float size, string label,
            Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(x, y);
            var img = go.GetComponent<Image>();
            img.color = color;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(onClick);

            var lgo = new GameObject("L", typeof(RectTransform));
            var lrt = (RectTransform)lgo.transform;
            lrt.SetParent(rt, false);
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var t = lgo.AddComponent<TextMeshProUGUI>();
            t.text = label;
            t.fontSize = 34;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            t.raycastTarget = false;
            return btn;
        }

        private void Update()
        {
            if (_over) return;
            if (_twoPlayer)
            {
                if (Input.GetKeyDown(KeyCode.Space)) _bombQ[0] = true;
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                    || Input.GetKeyDown(KeyCode.RightShift)) _bombQ[1] = true;
            }
            else if (Input.GetKeyDown(KeyCode.Space)) _bombQ[0] = true;
        }

        private IEnumerator TickLoop()
        {
            var wait = new WaitForSeconds(1f / TickHz);
            while (!_over)
            {
                yield return wait;
                if (_over) break;

                _state.SetInput(0, HeadingFor(0), _bombQ[0]);
                _bombQ[0] = false;
                if (_twoPlayer) { _state.SetInput(1, HeadingFor(1), _bombQ[1]); _bombQ[1] = false; }
                else _cpu.BeforeTick();

                var res = BombSweepEngine.Step(_state);
                Render();
                if (res.ExplodedThisTick != null && res.ExplodedThisTick.Count > 0)
                {
                    Sfx.Play("hit");
                    foreach (var e in _state.Explosions)
                        foreach (var c in e.Cells)
                            if (In(c.X, c.Y)) UiTween.Pop(_cells[c.X, c.Y].rectTransform, 0.4f, 0.18f);
                }
                if (res.MatchOver) { _winner = res.WinnerIndex ?? -1; _over = true; Render(); }
            }
        }

        private BombDir HeadingFor(int player)
        {
            bool up, down, left, right;
            if (player == 0)
            {
                up = Held(_upButton); down = Held(_downButton); left = Held(_leftButton); right = Held(_rightButton);
                up |= K(KeyCode.W); down |= K(KeyCode.S); left |= K(KeyCode.A); right |= K(KeyCode.D);
                if (!_twoPlayer) // solo: arrows also drive P0
                { up |= K(KeyCode.UpArrow); down |= K(KeyCode.DownArrow); left |= K(KeyCode.LeftArrow); right |= K(KeyCode.RightArrow); }
            }
            else
            {
                up = Held(_p1dir[0]); down = Held(_p1dir[1]); left = Held(_p1dir[2]); right = Held(_p1dir[3]);
                up |= K(KeyCode.UpArrow); down |= K(KeyCode.DownArrow); left |= K(KeyCode.LeftArrow); right |= K(KeyCode.RightArrow);
            }
            if (up) return BombDir.Up;
            if (down) return BombDir.Down;
            if (left) return BombDir.Left;
            if (right) return BombDir.Right;
            return BombDir.None;
        }

        private static bool K(KeyCode k) => Input.GetKey(k);
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
                {
                    var ct = _state.Board.Get(x, y);
                    string n = ArtName(ct);
                    if (n != null && Art.TryApply(_cells[x, y], "bomb_sweep", n)) continue;
                    Shapes.Rounded(_cells[x, y]);
                    _cells[x, y].color = CellColor(ct);
                }

            foreach (var b in _state.Bombs)
                if (In(b.Pos.X, b.Pos.Y) && !Art.TryApply(_cells[b.Pos.X, b.Pos.Y], "bomb_sweep", "bomb"))
                { Shapes.Rounded(_cells[b.Pos.X, b.Pos.Y]); _cells[b.Pos.X, b.Pos.Y].color = BombCol; }

            foreach (var e in _state.Explosions)
                foreach (var c in e.Cells)
                    if (In(c.X, c.Y) && !Art.TryApply(_cells[c.X, c.Y], "bomb_sweep", "explosion"))
                    { Shapes.Rounded(_cells[c.X, c.Y]); _cells[c.X, c.Y].color = Blast; }

            foreach (var p in _state.Players)
                if (p.IsAlive && In(p.Pos.X, p.Pos.Y))
                {
                    var cell = _cells[p.Pos.X, p.Pos.Y];
                    if (!Art.TryApply(cell, "bomb_sweep", p.Index == 0 ? "player" : "cpu"))
                    { Shapes.Rounded(cell); cell.color = PlayerCol[p.Index % PlayerCol.Length]; }
                }

            if (_status != null) _status.text = StatusText();
            if (_over) ShowOver();
        }

        private bool In(int x, int y) => x >= 0 && y >= 0 && x < _w && y < _h;

        private static string ArtName(CellType c) => c switch
        {
            CellType.HardWall => "wall",
            CellType.SoftBlock => "soft",
            CellType.PowerBombs => "power_bombs",
            CellType.PowerRange => "power_range",
            CellType.PowerSpeed => "power_speed",
            _ => null,
        };

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
            bool p0 = _state.Players[0].IsAlive, p1 = _state.Players[1].IsAlive;
            if (_twoPlayer)
            {
                if (_over) return _winner >= 0 ? Loc.T("ig.player_wins", _winner + 1) : Loc.T("result.draw");
                return "P1  vs  P2";
            }
            if (_over)
            {
                if (p0 && !p1) return Loc.T("result.you_win");
                if (!p0) return Loc.T("result.you_lose");
                return Loc.T("result.draw");
            }
            return Loc.T("ig.bomb_vs");
        }

        private void ShowOver()
        {
            if (_twoPlayer)
            {
                GameOverlay.Show(StatusText(),
                    _winner >= 0 ? GameOverlay.Outcome.Win : GameOverlay.Outcome.Neutral);
                return;
            }
            bool p0 = _state.Players[0].IsAlive, p1 = _state.Players[1].IsAlive;
            GameOverlay.Show(StatusText(),
                p0 && !p1 ? GameOverlay.Outcome.Win
                : !p0 ? GameOverlay.Outcome.Lose
                : GameOverlay.Outcome.Neutral);
        }
    }
}
