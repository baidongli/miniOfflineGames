using System.Collections;
using MiniGames.App.Bootstrap;
using MiniGames.GameModule;
using MiniGames.Games.MazePaint;
using MiniGames.Games.MazePaint.Logic;
using MiniGames.Networking.Session;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniGames.App.Games
{
    /// <summary>
    /// Solo Maze Paint in its own scene. Single player: leave your territory to
    /// lay a trail, return home to capture the enclosed area. Crossing your own
    /// trail or hitting the boundary ends the run. Goal: paint as much as you
    /// can. Swipe / arrow keys to steer. 24x24, 8 Hz tick.
    /// </summary>
    public sealed class MazePaintSceneController : MonoBehaviour
    {
        [SerializeField] private RectTransform _boardGrid; // GridLayoutGroup
        [SerializeField] private TMP_Text _status;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _restartButton;

        private const float TickHz = 8f;
        private const float SwipeThreshold = 40f;

        private static readonly Color Empty = new Color(0.12f, 0.13f, 0.16f);
        private static readonly Color Owned = new Color(0.25f, 0.45f, 0.85f);
        private static readonly Color Trail = new Color(0.50f, 0.72f, 0.96f);
        private static readonly Color Head = new Color(0.97f, 0.97f, 1.00f);

        private MazePaintModule _module;
        private MazePaintGameState _state;
        private Image[,] _cells;
        private int _n;
        private bool _over;

        private bool _dragging;
        private Vector2 _dragStart;

        private void Start()
        {
            _module = new MazePaintModule();
            _module.StartSolo(BuildContext());
            _state = _module.SoloState;
            _n = _state.Board.Size;

            BuildCells();
            if (_backButton != null) _backButton.onClick.AddListener(() => SceneManager.LoadScene("Hub"));
            if (_restartButton != null) _restartButton.onClick.AddListener(() => SceneManager.LoadScene("MazePaint"));
            Loc.Label(_backButton, "ui.back");
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

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) Steer(MazeDir.Up);
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) Steer(MazeDir.Down);
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Steer(MazeDir.Left);
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Steer(MazeDir.Right);

            if (Input.GetMouseButtonDown(0)) { _dragging = true; _dragStart = Input.mousePosition; }
            else if (Input.GetMouseButtonUp(0) && _dragging)
            {
                _dragging = false;
                Vector2 delta = (Vector2)Input.mousePosition - _dragStart;
                if (delta.magnitude < SwipeThreshold) return;
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    Steer(delta.x > 0 ? MazeDir.Right : MazeDir.Left);
                else
                    Steer(delta.y > 0 ? MazeDir.Up : MazeDir.Down);
            }
        }

        private void Steer(MazeDir d) => _state.SetInput(0, d);

        private IEnumerator TickLoop()
        {
            var wait = new WaitForSeconds(1f / TickHz);
            while (!_over)
            {
                yield return wait;
                if (_over) break;
                var res = MazePaintEngine.Step(_state);
                if (res.CapturedThisTick != null && res.CapturedThisTick.Count > 0) Sfx.Play("clear");
                Render();
                if (res.MatchOver) { _over = true; Render(); }
            }
        }

        private void BuildCells()
        {
            _cells = new Image[_n, _n];
            for (int row = 0; row < _n; row++)
            {
                int y = _n - 1 - row; // top UI row is the highest y
                for (int x = 0; x < _n; x++)
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
            for (int y = 0; y < _n; y++)
                for (int x = 0; x < _n; x++)
                {
                    if (_state.Board.OwnerAt(x, y) == 0) _cells[x, y].color = Owned;
                    else if (_state.Board.TrailAt(x, y) == 0) _cells[x, y].color = Trail;
                    else _cells[x, y].color = Empty;
                }

            var p = _state.Players[0];
            if (p.IsAlive && p.Head.X >= 0 && p.Head.Y >= 0 && p.Head.X < _n && p.Head.Y < _n)
                _cells[p.Head.X, p.Head.Y].color = Head;

            if (_status != null) _status.text = StatusText();
            if (_over)
            {
                bool rec = BestScores.Report("maze_paint", _state.Board.CountOwned(0));
                GameOverlay.Show(StatusText() + BestScores.Suffix("maze_paint", rec));
            }
        }

        private string StatusText()
        {
            int owned = _state.Board.CountOwned(0);
            int pct = Mathf.RoundToInt(100f * owned / (_n * _n));
            string body = $"{Loc.T("ig.territory")} {owned} ({pct}%)";
            return _over ? $"{Loc.T("result.game_over")}   {body}" : body;
        }
    }
}
