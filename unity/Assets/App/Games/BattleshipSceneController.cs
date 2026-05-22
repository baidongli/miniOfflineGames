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
    /// Solo Battleship vs the CPU. The shared BattleshipGame is built for the
    /// asymmetric multiplayer protocol, so for solo this drives two boards
    /// directly: the human's fleet, the CPU's fleet (hidden), plus a tracker
    /// each. Fleets are auto-placed (Randomize to reshuffle, Start to begin).
    /// One shot per turn: tap an enemy cell to fire, then the CPU fires back.
    /// </summary>
    public sealed class BattleshipSceneController : MonoBehaviour
    {
        [SerializeField] private RectTransform _enemyGrid;  // GridLayoutGroup, 10 cols (tap to fire)
        [SerializeField] private RectTransform _ownGrid;    // GridLayoutGroup, 10 cols (your fleet)
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

        private BattleshipBoard _humanFleet, _cpuFleet, _humanTracker, _cpuTracker;
        private Image[,] _enemyCells, _ownCells;
        private Phase _phase = Phase.Setup;
        private bool _busy;

        private void Start()
        {
            _placer = new SimpleBattleshipAI(_rng.Next());
            _cpuAI = new SimpleBattleshipAI(_rng.Next());

            _cpuFleet = new BattleshipBoard();
            _cpuAI.PlaceFleet(_cpuFleet, _rng);
            _cpuTracker = new BattleshipBoard();
            RandomizeHumanFleet();

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

            RenderAll();
        }

        private void RandomizeHumanFleet()
        {
            _humanFleet = new BattleshipBoard();
            _placer.PlaceFleet(_humanFleet, _rng);
            _humanTracker = new BattleshipBoard();
        }

        private void OnRandomize()
        {
            if (_phase != Phase.Setup) return;
            RandomizeHumanFleet();
            RenderAll();
        }

        private void OnStartBattle()
        {
            if (_phase != Phase.Setup) return;
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

        // ---- play ----

        private void OnFire(int x, int y)
        {
            if (_phase != Phase.Playing || _busy) return;
            if (_humanTracker.ShotAt(x, y)) return;

            var r = _cpuFleet.RegisterIncomingShot(x, y, out var kind, out var sunkCells);
            _humanTracker.RecordShotResult(x, y, r);
            if (r == ShotResult.Sunk) _humanTracker.RecordSunkShip(sunkCells, kind);
            Sfx.Play(r == ShotResult.Miss ? "miss" : r == ShotResult.Sunk ? "clear" : "hit");
            UiTween.Pop(_enemyCells[x, y].rectTransform);

            if (_cpuFleet.AllShipsSunk()) { _phase = Phase.Over; RenderAll(); return; }

            StartCoroutine(EnemyTurn());
        }

        private IEnumerator EnemyTurn()
        {
            _busy = true;
            RenderAll();
            yield return new WaitForSeconds(0.5f);

            var pick = _cpuAI.ChooseShot(_cpuTracker);
            if (pick.HasValue)
            {
                var r = _humanFleet.RegisterIncomingShot(pick.Value.x, pick.Value.y,
                    out var kind, out var sunkCells);
                _cpuTracker.RecordShotResult(pick.Value.x, pick.Value.y, r);
                if (r == ShotResult.Sunk) _cpuTracker.RecordSunkShip(sunkCells, kind);
                _cpuAI.RecordResult(r);
            }

            if (_humanFleet.AllShipsSunk()) _phase = Phase.Over;
            _busy = false;
            RenderAll();
        }

        // ---- render ----

        private void RenderAll()
        {
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    Paint(_enemyCells[x, y], EnemyArt(x, y), EnemyColor(x, y));
                    Paint(_ownCells[x, y], OwnArt(x, y), OwnColor(x, y));
                }
            if (_status != null) _status.text = StatusText();
            if (_phase == Phase.Over)
                GameOverlay.Show(StatusText(),
                    _cpuFleet.AllShipsSunk() ? GameOverlay.Outcome.Win : GameOverlay.Outcome.Lose);
        }

        private static void Paint(Image img, string artName, Color color)
        {
            if (artName != null && Art.TryApply(img, "battleship", artName)) return;
            Shapes.Rounded(img);
            img.color = color;
        }

        private Color EnemyColor(int x, int y)
        {
            byte ship = _humanTracker.ShipAt(x, y);
            if (ship >= 1 && ship <= 5) return Sunk;     // confirmed sunk ship cell
            if (ship == 255) return Hit;                 // hit, ship unknown
            if (_humanTracker.ShotAt(x, y)) return Miss;
            return Water;
        }

        private string EnemyArt(int x, int y)
        {
            byte ship = _humanTracker.ShipAt(x, y);
            if (ship >= 1 && ship <= 5) return "ship";   // sunk ship revealed
            if (ship == 255) return "hit";
            if (_humanTracker.ShotAt(x, y)) return "miss";
            return null;                                  // water -> procedural
        }

        private Color OwnColor(int x, int y)
        {
            byte ship = _humanFleet.ShipAt(x, y);
            bool shot = _humanFleet.ShotAt(x, y);
            if (ship > 0 && shot) return Hit;
            if (ship > 0) return OwnShip;
            if (shot) return Miss;
            return Water;
        }

        private string OwnArt(int x, int y)
        {
            byte ship = _humanFleet.ShipAt(x, y);
            bool shot = _humanFleet.ShotAt(x, y);
            if (ship > 0 && shot) return "hit";
            if (ship > 0) return "ship";
            if (shot) return "miss";
            return null;
        }

        private string StatusText()
        {
            switch (_phase)
            {
                case Phase.Setup: return Loc.T("ig.place_fleet");
                case Phase.Over:
                    return _cpuFleet.AllShipsSunk() ? Loc.T("result.you_win") : Loc.T("result.you_lose");
                default:
                    return _busy ? Loc.T("ig.enemy_firing") : Loc.T("ig.fire_turn");
            }
        }
    }
}
