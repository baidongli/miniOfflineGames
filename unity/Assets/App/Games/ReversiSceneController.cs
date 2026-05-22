using System.Collections;
using MiniGames.App.Bootstrap;
using MiniGames.GameModule;
using MiniGames.Games.Reversi;
using MiniGames.Games.Reversi.AI;
using MiniGames.Games.Reversi.Logic;
using MiniGames.Networking.Session;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniGames.App.Games
{
    /// <summary>
    /// Solo Reversi in its own scene. Human plays Black (first); a minimax AI
    /// plays White. The 8x8 clickable grid is built at runtime under
    /// _boardGrid. Legal moves for the human are hinted. Passing is automatic
    /// in the rules engine, so there is no pass button - when it's the human's
    /// turn they always have at least one legal move.
    /// </summary>
    public sealed class ReversiSceneController : MonoBehaviour
    {
        [SerializeField] private RectTransform _boardGrid; // needs a GridLayoutGroup, 8 columns
        [SerializeField] private TMP_Text _status;
        [SerializeField] private Button _backButton;

        private const int N = ReversiBoard.Size; // 8
        private static readonly Color Felt = new Color(0.18f, 0.45f, 0.30f);
        private static readonly Color FeltHint = new Color(0.28f, 0.60f, 0.42f);
        private static readonly Color DiscBlack = new Color(0.10f, 0.10f, 0.12f);
        private static readonly Color DiscWhite = new Color(0.95f, 0.95f, 0.95f);

        private ReversiModule _module;
        private ReversiGame _game;
        private IReversiAI _ai;
        private Image[,] _cells;
        private bool _busy;
        private bool _vsCpu;

        private void Start()
        {
            _vsCpu = !GameLaunch.SameDevice;
            _module = new ReversiModule();
            _module.StartSolo(BuildContext());
            _game = _module.SoloGame;
            _ai = new MinimaxReversiAI(3);

            BuildCells();
            if (_backButton != null)
                _backButton.onClick.AddListener(() => SceneManager.LoadScene("Hub"));
            Loc.Label(_backButton, "ui.back");
            InstructionsOverlay.AttachButton((RectTransform)transform, "reversi");

            _game.Moved += r =>
            {
                if (r.Accepted && !r.WasPass)
                {
                    UiTween.Pop(_cells[r.X, r.Y].rectTransform);
                    if (r.Flipped != null)
                        foreach (var (fx, fy) in r.Flipped) UiTween.Pop(_cells[fx, fy].rectTransform);
                }
                Render();
            };
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
            _cells = new Image[N, N];
            for (int row = 0; row < N; row++)
            {
                int y = row;
                for (int x = 0; x < N; x++)
                {
                    int cx = x, cy = y;
                    var go = new GameObject($"Cell_{x}_{y}",
                        typeof(RectTransform), typeof(Image), typeof(Button));
                    go.transform.SetParent(_boardGrid, false);
                    var img = go.GetComponent<Image>();
                    img.color = Felt;
                    Shapes.Circle(img);
                    var btn = go.GetComponent<Button>();
                    btn.targetGraphic = img;
                    btn.onClick.AddListener(() => OnCellTapped(cx, cy));
                    _cells[x, y] = img;
                }
            }
        }

        private void OnCellTapped(int x, int y)
        {
            if (_busy || _game.IsGameOver) return;
            // Solo: only Black (human) taps. Same-device: either colour taps.
            if (_vsCpu && _game.CurrentPlayer != ReversiBoard.Black) return;
            if (!_game.TryPlay(x, y, out _)) return;               // illegal -> ignore
            Sfx.Play("place");
            if (_vsCpu && !_game.IsGameOver && _game.CurrentPlayer == ReversiBoard.White)
                StartCoroutine(AiLoop());
            else
                Render();
        }

        private IEnumerator AiLoop()
        {
            _busy = true;
            Render();
            // White may move several times in a row when Black has no reply.
            while (!_game.IsGameOver && _game.CurrentPlayer == ReversiBoard.White)
            {
                yield return new WaitForSeconds(0.4f);
                var mv = _ai.Choose(_game);
                if (mv == null) _game.Pass(out _);
                else _game.TryPlay(mv.Value.x, mv.Value.y, out _);
            }
            _busy = false;
            Render();
        }

        private void Render()
        {
            // Show legal-move hints whenever a human is on the clock: in solo
            // that's Black only; in same-device it's whoever's turn it is.
            bool showHints = !_game.IsGameOver && !_busy
                && (!_vsCpu || _game.CurrentPlayer == ReversiBoard.Black);

            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    byte v = _game.Board.Get(x, y);
                    if (v == ReversiBoard.Black) _cells[x, y].color = DiscBlack;
                    else if (v == ReversiBoard.White) _cells[x, y].color = DiscWhite;
                    else _cells[x, y].color = Felt;
                }

            if (showHints)
                foreach (var (lx, ly) in _game.Board.LegalMoves(_game.CurrentPlayer))
                    _cells[lx, ly].color = FeltHint;

            if (_status != null) _status.text = StatusText();
            if (_game.IsGameOver) GameOverlay.Show(StatusText(), ResultOutcome());
        }

        private GameOverlay.Outcome ResultOutcome()
        {
            if (_game.Result == ReversiResult.Draw) return GameOverlay.Outcome.Neutral;
            if (!_vsCpu) return GameOverlay.Outcome.Win;
            return _game.Result == ReversiResult.BlackWins
                ? GameOverlay.Outcome.Win : GameOverlay.Outcome.Lose;
        }

        private string StatusText()
        {
            int b = _game.Board.Count(ReversiBoard.Black);
            int w = _game.Board.Count(ReversiBoard.White);
            string score = $"   {Loc.T("ig.black")} {b} : {w} {Loc.T("ig.white")}";
            if (_vsCpu)
            {
                switch (_game.Result)
                {
                    case ReversiResult.BlackWins: return Loc.T("result.you_win") + score;
                    case ReversiResult.WhiteWins: return Loc.T("ig.cpu_wins") + score;
                    case ReversiResult.Draw: return Loc.T("result.draw") + score;
                    default:
                        return (_busy ? Loc.T("ig.thinking") : Loc.T("ig.your_turn")) + score;
                }
            }
            switch (_game.Result)
            {
                case ReversiResult.BlackWins: return Loc.T("ig.wins", Loc.T("ig.black")) + score;
                case ReversiResult.WhiteWins: return Loc.T("ig.wins", Loc.T("ig.white")) + score;
                case ReversiResult.Draw: return Loc.T("result.draw") + score;
                default:
                    return Loc.T("ig.turn_of",
                        _game.CurrentPlayer == ReversiBoard.Black
                            ? Loc.T("ig.black") : Loc.T("ig.white")) + score;
            }
        }
    }
}
