using System.Collections;
using MiniGames.App.Bootstrap;
using MiniGames.GameModule;
using MiniGames.Games.ConnectFour;
using MiniGames.Games.ConnectFour.AI;
using MiniGames.Games.ConnectFour.Logic;
using MiniGames.Networking.Session;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniGames.App.Games
{
    /// <summary>
    /// Drives a solo Connect Four game in its own scene. Human plays PlayerA
    /// (red, first); a minimax AI plays PlayerB (yellow). The 7x6 grid of
    /// clickable cells is built at runtime under _boardGrid so the only
    /// Inspector wiring is three references.
    /// </summary>
    public sealed class ConnectFourSceneController : MonoBehaviour
    {
        [SerializeField] private RectTransform _boardGrid; // needs a GridLayoutGroup, 7 columns
        [SerializeField] private TMP_Text _status;
        [SerializeField] private Button _backButton;

        private const int W = 7, H = 6;
        private static readonly Color SlotEmpty = new Color(0.12f, 0.14f, 0.22f);
        private static readonly Color DiscA = new Color(0.90f, 0.30f, 0.30f); // human, red
        private static readonly Color DiscB = new Color(0.95f, 0.80f, 0.25f); // cpu, yellow

        private ConnectFourModule _module;
        private ConnectFourGame _game;
        private IConnectFourAI _ai;
        private Image[,] _cells;
        private bool _busy;

        private void Start()
        {
            _module = new ConnectFourModule();
            _module.StartSolo(BuildContext());
            _game = _module.SoloGame;
            _ai = new MinimaxConnectFourAI(4);

            BuildCells();
            if (_backButton != null)
                _backButton.onClick.AddListener(() => SceneManager.LoadScene("Hub"));

            _game.Moved += _ => Render();
            Render();
        }

        private static GameContext BuildContext()
        {
            // Normal flow: Boot scene populated AppBootstrap.Services. When this
            // scene is played directly (Boot never ran), fall back to a minimal
            // context - Connect Four solo only touches ctx.Audio, null-safely.
            if (AppBootstrap.Services != null)
                return AppBootstrap.BuildContext(NullSendChannel.Instance);
            return new GameContext(null, null, null, null, NullSendChannel.Instance, "local");
        }

        private void BuildCells()
        {
            _cells = new Image[W, H];
            // Grid fills left-to-right, top-to-bottom; top UI row is the highest y.
            for (int row = 0; row < H; row++)
            {
                int y = H - 1 - row;
                for (int x = 0; x < W; x++)
                {
                    int col = x;
                    var go = new GameObject($"Cell_{x}_{y}",
                        typeof(RectTransform), typeof(Image), typeof(Button));
                    go.transform.SetParent(_boardGrid, false);
                    var img = go.GetComponent<Image>();
                    img.color = SlotEmpty;
                    var btn = go.GetComponent<Button>();
                    btn.targetGraphic = img;
                    btn.onClick.AddListener(() => OnColumnTapped(col));
                    _cells[x, y] = img;
                }
            }
        }

        private void OnColumnTapped(int column)
        {
            if (_busy || _game.IsGameOver) return;
            if (_game.CurrentPlayer != ConnectFourBoard.PlayerA) return; // human is A
            if (!_game.TryPlay(column, out _)) return;
            if (!_game.IsGameOver && _game.CurrentPlayer == ConnectFourBoard.PlayerB)
                StartCoroutine(AiMove());
        }

        private IEnumerator AiMove()
        {
            _busy = true;
            Render();
            yield return new WaitForSeconds(0.4f);
            if (!_game.IsGameOver)
                _game.TryPlay(_ai.ChooseColumn(_game), out _);
            _busy = false;
            Render();
        }

        private void Render()
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    byte v = _game.Board.Get(x, y);
                    _cells[x, y].color = v == ConnectFourBoard.PlayerA ? DiscA
                                       : v == ConnectFourBoard.PlayerB ? DiscB
                                       : SlotEmpty;
                }
            if (_status != null) _status.text = StatusText();
            if (_game.IsGameOver) GameOverlay.Show(StatusText());
        }

        private string StatusText()
        {
            switch (_game.Result)
            {
                case GameResult.PlayerAWins: return "You win!";
                case GameResult.PlayerBWins: return "Computer wins";
                case GameResult.Draw: return "Draw";
                default:
                    return _busy ? "Computer thinking..."
                         : _game.CurrentPlayer == ConnectFourBoard.PlayerA
                            ? "Your turn (red)"
                            : "Computer's turn";
            }
        }
    }
}
