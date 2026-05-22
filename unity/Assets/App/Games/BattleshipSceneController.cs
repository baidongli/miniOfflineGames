using System.Collections;
using MiniGames.Games.Battleship.AI;
using MiniGames.Games.Battleship.Logic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniGames.App.Games
{
    /// <summary>
    /// Battleship, two modes:
    ///  - Solo: you vs a hunt/target CPU. Fleets auto-placed (Randomize/Start).
    ///  - Same-device (hot-seat): two humans share the device. Fleets are
    ///    auto-placed; turns alternate with a full-screen "pass the device"
    ///    cover between them so neither player sees the other's board.
    /// Two boards are driven directly (the shared BattleshipGame targets the
    /// network protocol). One shot per turn.
    /// </summary>
    public sealed class BattleshipSceneController : MonoBehaviour
    {
        [SerializeField] private RectTransform _enemyGrid;  // tap to fire (current player's tracker)
        [SerializeField] private RectTransform _ownGrid;    // current player's own fleet
        [SerializeField] private TMP_Text _status;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _randomizeButton;
        [SerializeField] private Button _startButton;

        private const int N = BattleshipBoard.Size; // 10

        private static readonly Color Water = new Color(0.15f, 0.30f, 0.50f);
        private static readonly Color Miss = new Color(0.80f, 0.85f, 0.90f);
        private static readonly Color Hit = new Color(0.90f, 0.40f, 0.35f);
        private static readonly Color Sunk = new Color(0.55f, 0.20f, 0.20f);
        private static readonly Color OwnShip = new Color(0.45f, 0.47f, 0.52f);

        private enum Phase { Setup, Playing, Over }

        private readonly System.Random _rng = new System.Random();
        private SimpleBattleshipAI _placer;
        private SimpleBattleshipAI _cpuAI;

        private bool _vsCpu;
        private readonly BattleshipBoard[] _fleet = new BattleshipBoard[2];
        private readonly BattleshipBoard[] _tracker = new BattleshipBoard[2];
        private int _current;        // current shooter (always 0 in solo)
        private int _winner = -1;
        private Image[,] _enemyCells, _ownCells;
        private Phase _phase = Phase.Setup;
        private bool _busy;
        private GameObject _passGate;
        private TMP_Text _passLabel;

        private void Start()
        {
            _vsCpu = !GameLaunch.SameDevice;
            _placer = new SimpleBattleshipAI(_rng.Next());
            _cpuAI = new SimpleBattleshipAI(_rng.Next());

            _fleet[0] = PlacedFleet();
            _fleet[1] = PlacedFleet();
            _tracker[0] = new BattleshipBoard();
            _tracker[1] = new BattleshipBoard();
            _current = 0;

            BuildGrid(_enemyGrid, ref _enemyCells, clickable: true);
            BuildGrid(_ownGrid, ref _ownCells, clickable: false);

            if (_backButton != null) _backButton.onClick.AddListener(() => SceneManager.LoadScene("Hub"));
            if (_restartButton != null) _restartButton.onClick.AddListener(() => SceneManager.LoadScene("Battleship"));
            if (_randomizeButton != null) _randomizeButton.onClick.AddListener(OnRandomize);
            if (_startButton != null) _startButton.onClick.AddListener(OnStartBattle);
            Loc.Label(_backButton, "ui.back");
            InstructionsOverlay.AttachButton((RectTransform)transform, "battleship");
            Art.StyleButtons((RectTransform)transform);
            Loc.Label(_restartButton, "ui.restart");
            Loc.Label(_randomizeButton, "ui.randomize");
            Loc.Label(_startButton, "ui.start");

            BuildPassGate(); // after StyleButtons so the cover isn't skinned (would leak corners)

            if (_vsCpu)
            {
                _phase = Phase.Setup; // let the human reshuffle / start
            }
            else
            {
                // Hot-seat: fleets auto-placed, jump straight to play behind a pass cover.
                _phase = Phase.Playing;
                if (_randomizeButton != null) _randomizeButton.gameObject.SetActive(false);
                if (_startButton != null) _startButton.gameObject.SetActive(false);
            }

            RenderAll();
            if (!_vsCpu) ShowPassGate(); // cover before Player 1's first turn
        }

        private BattleshipBoard PlacedFleet()
        {
            var b = new BattleshipBoard();
            _placer.PlaceFleet(b, _rng);
            return b;
        }

        // Solo setup only.
        private void OnRandomize()
        {
            if (!_vsCpu || _phase != Phase.Setup) return;
            _fleet[0] = PlacedFleet();
            _tracker[0] = new BattleshipBoard();
            RenderAll();
        }

        private void OnStartBattle()
        {
            if (!_vsCpu || _phase != Phase.Setup) return;
            _phase = Phase.Playing;
            if (_randomizeButton != null) _randomizeButton.gameObject.SetActive(false);
            if (_startButton != null) _startButton.gameObject.SetActive(false);
            RenderAll();
        }

        // ---- build ----

        private void BuildGrid(RectTransform parent, ref Image[,] cells, bool clickable)
        {
            cells = new Image[N, N];
            for (int row = 0; row < N; row++)
            {
                int y = row;
                for (int x = 0; x < N; x++)
                {
                    int cx = x, cy = y;
                    var go = new GameObject($"C_{x}_{y}", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(parent, false);
                    var img = go.GetComponent<Image>();
                    img.color = Water;
                    Shapes.Rounded(img);
                    if (clickable)
                    {
                        var btn = go.AddComponent<Button>();
                        btn.targetGraphic = img;
                        btn.onClick.AddListener(() => OnFire(cx, cy));
                    }
                    else img.raycastTarget = false;
                    cells[x, y] = img;
                }
            }
        }

        private void BuildPassGate()
        {
            _passGate = new GameObject("PassGate", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = (RectTransform)_passGate.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            _passGate.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 1f);
            _passGate.GetComponent<Button>().onClick.AddListener(HidePassGate);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.SetParent(rt, false);
            lrt.anchorMin = new Vector2(0.1f, 0.4f); lrt.anchorMax = new Vector2(0.9f, 0.6f);
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            _passLabel = labelGo.AddComponent<TextMeshProUGUI>();
            _passLabel.fontSize = 48;
            _passLabel.alignment = TextAlignmentOptions.Center;
            _passLabel.color = Color.white;
            _passGate.SetActive(false);
        }

        private void ShowPassGate()
        {
            _passLabel.text = Loc.T("ig.player_tap", _current + 1);
            _passGate.transform.SetAsLastSibling();
            _passGate.SetActive(true);
        }

        private void HidePassGate() => _passGate.SetActive(false);

        // ---- play ----

        private void OnFire(int x, int y)
        {
            if (_phase != Phase.Playing || _busy) return;
            if (_passGate.activeSelf) return;
            int shooter = _current, target = 1 - _current;
            if (_tracker[shooter].ShotAt(x, y)) return;

            var r = _fleet[target].RegisterIncomingShot(x, y, out var kind, out var sunkCells);
            _tracker[shooter].RecordShotResult(x, y, r);
            if (r == ShotResult.Sunk) _tracker[shooter].RecordSunkShip(sunkCells, kind);
            Sfx.Play(r == ShotResult.Miss ? "miss" : r == ShotResult.Sunk ? "clear" : "hit");
            UiTween.Pop(_enemyCells[x, y].rectTransform);

            if (_fleet[target].AllShipsSunk())
            {
                _winner = shooter;
                _phase = Phase.Over;
                RenderAll();
                return;
            }

            if (_vsCpu) StartCoroutine(EnemyTurn());
            else StartCoroutine(PassTurn());
        }

        private IEnumerator EnemyTurn()
        {
            _busy = true;
            RenderAll();
            yield return new WaitForSeconds(0.5f);

            var pick = _cpuAI.ChooseShot(_tracker[1]);
            if (pick.HasValue)
            {
                var r = _fleet[0].RegisterIncomingShot(pick.Value.x, pick.Value.y,
                    out var kind, out var sunkCells);
                _tracker[1].RecordShotResult(pick.Value.x, pick.Value.y, r);
                if (r == ShotResult.Sunk) _tracker[1].RecordSunkShip(sunkCells, kind);
                _cpuAI.RecordResult(r);
            }

            if (_fleet[0].AllShipsSunk()) { _winner = 1; _phase = Phase.Over; }
            _busy = false;
            RenderAll();
        }

        private IEnumerator PassTurn()
        {
            _busy = true;
            RenderAll();                  // let the shooter see their result
            yield return new WaitForSeconds(0.9f);
            _current = 1 - _current;
            _busy = false;
            RenderAll();                  // render next player's view (hidden behind the gate)
            ShowPassGate();
        }

        // ---- render ----

        private void RenderAll()
        {
            int view = _vsCpu ? 0 : _current;  // whose perspective to draw
            var tracker = _tracker[view];
            var fleet = _fleet[view];

            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    Paint(_enemyCells[x, y], EnemyArt(tracker, x, y), EnemyColor(tracker, x, y));
                    Paint(_ownCells[x, y], OwnArt(fleet, x, y), OwnColor(fleet, x, y));
                }
            if (_status != null) _status.text = StatusText();
            if (_phase == Phase.Over)
            {
                var outcome = _vsCpu
                    ? (_winner == 0 ? GameOverlay.Outcome.Win : GameOverlay.Outcome.Lose)
                    : GameOverlay.Outcome.Win;
                GameOverlay.Show(StatusText(), outcome);
            }
        }

        private static void Paint(Image img, string artName, Color color)
        {
            if (artName != null && Art.TryApply(img, "battleship", artName)) return;
            Shapes.Rounded(img);
            img.color = color;
        }

        private static Color EnemyColor(BattleshipBoard tracker, int x, int y)
        {
            byte ship = tracker.ShipAt(x, y);
            if (ship >= 1 && ship <= 5) return Sunk;
            if (ship == 255) return Hit;
            if (tracker.ShotAt(x, y)) return Miss;
            return Water;
        }

        private static string EnemyArt(BattleshipBoard tracker, int x, int y)
        {
            byte ship = tracker.ShipAt(x, y);
            if (ship >= 1 && ship <= 5) return "ship";
            if (ship == 255) return "hit";
            if (tracker.ShotAt(x, y)) return "miss";
            return null;
        }

        private static Color OwnColor(BattleshipBoard fleet, int x, int y)
        {
            byte ship = fleet.ShipAt(x, y);
            bool shot = fleet.ShotAt(x, y);
            if (ship > 0 && shot) return Hit;
            if (ship > 0) return OwnShip;
            if (shot) return Miss;
            return Water;
        }

        private static string OwnArt(BattleshipBoard fleet, int x, int y)
        {
            byte ship = fleet.ShipAt(x, y);
            bool shot = fleet.ShotAt(x, y);
            if (ship > 0 && shot) return "hit";
            if (ship > 0) return "ship";
            if (shot) return "miss";
            return null;
        }

        private string StatusText()
        {
            if (_vsCpu)
            {
                switch (_phase)
                {
                    case Phase.Setup: return Loc.T("ig.place_fleet");
                    case Phase.Over: return _winner == 0 ? Loc.T("result.you_win") : Loc.T("result.you_lose");
                    default: return _busy ? Loc.T("ig.enemy_firing") : Loc.T("ig.fire_turn");
                }
            }
            if (_phase == Phase.Over) return Loc.T("ig.player_wins", _winner + 1);
            return Loc.T("ig.player_turn", _current + 1);
        }
    }
}
