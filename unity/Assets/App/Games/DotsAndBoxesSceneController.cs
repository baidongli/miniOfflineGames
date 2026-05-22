using System.Collections;
using System.Collections.Generic;
using MiniGames.App.Bootstrap;
using MiniGames.GameModule;
using MiniGames.Games.DotsAndBoxes;
using MiniGames.Games.DotsAndBoxes.AI;
using MiniGames.Games.DotsAndBoxes.Logic;
using MiniGames.Networking.Session;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniGames.App.Games
{
    /// <summary>
    /// Solo Dots and Boxes in its own scene. Human is player 0 (blue), a
    /// greedy AI is player 1 (red). Dots, clickable edges, and box fills are
    /// positioned at runtime inside _boardArea. Completing a box grants
    /// another turn (engine rule), so the AI may play several edges in a row.
    /// </summary>
    public sealed class DotsAndBoxesSceneController : MonoBehaviour
    {
        [SerializeField] private RectTransform _boardArea; // a fixed-size square container
        [SerializeField] private TMP_Text _status;
        [SerializeField] private Button _backButton;

        private static readonly Color EdgeUndrawn = new Color(0.30f, 0.31f, 0.36f);
        private static readonly Color DotColor = new Color(0.85f, 0.85f, 0.90f);
        private static readonly Color[] PlayerColor =
        {
            new Color(0.30f, 0.55f, 0.95f), // P0 human - blue
            new Color(0.92f, 0.38f, 0.38f), // P1 cpu   - red
        };
        private static readonly Color[] BoxFill =
        {
            new Color(0.30f, 0.55f, 0.95f, 0.35f),
            new Color(0.92f, 0.38f, 0.38f, 0.35f),
        };

        private DotsAndBoxesModule _module;
        private DotsGame _game;
        private IDotsAI _ai;

        private int _bw, _bh;
        private Image[,] _hImg;   // [bw, bh+1]
        private Image[,] _vImg;   // [bw+1, bh]
        private Image[,] _boxImg; // [bw, bh]
        private readonly Dictionary<(EdgeKind, int, int), int> _edgeOwner =
            new Dictionary<(EdgeKind, int, int), int>();

        private bool _busy;

        private void Start()
        {
            _module = new DotsAndBoxesModule();
            _module.StartSolo(BuildContext());
            _game = _module.SoloGame;
            _ai = new SimpleDotsAI();
            _bw = _game.Board.BoxWidth;
            _bh = _game.Board.BoxHeight;

            BuildBoard();
            if (_backButton != null)
                _backButton.onClick.AddListener(() => SceneManager.LoadScene("Hub"));

            _game.Moved += OnMoved;
            Render();
        }

        private static GameContext BuildContext()
        {
            if (AppBootstrap.Services != null)
                return AppBootstrap.BuildContext(NullSendChannel.Instance);
            return new GameContext(null, null, null, null, NullSendChannel.Instance, "local");
        }

        private void OnMoved(DotsMoveResult r)
        {
            if (r.Accepted)
                _edgeOwner[(r.Edge.Kind, r.Edge.X, r.Edge.Y)] = r.Player;
            Render();
        }

        private void BuildBoard()
        {
            float w = _boardArea.rect.width;
            float h = _boardArea.rect.height;
            float margin = 60f;
            float cellW = (w - 2f * margin) / _bw;
            float cellH = (h - 2f * margin) / _bh;
            float edgeThick = Mathf.Min(cellW, cellH) * 0.16f;

            Vector2 Dot(int x, int y) => new Vector2(margin + x * cellW, margin + y * cellH);

            _boxImg = new Image[_bw, _bh];
            _hImg = new Image[_bw, _bh + 1];
            _vImg = new Image[_bw + 1, _bh];

            // Box fills (bottom layer).
            for (int by = 0; by < _bh; by++)
                for (int bx = 0; bx < _bw; bx++)
                {
                    var c = Dot(bx, by) + new Vector2(cellW, cellH) * 0.5f;
                    var img = NewCell($"Box_{bx}_{by}", c,
                        new Vector2(cellW * 0.78f, cellH * 0.78f), out _);
                    img.color = new Color(0, 0, 0, 0);
                    _boxImg[bx, by] = img;
                }

            // Horizontal edges.
            for (int y = 0; y <= _bh; y++)
                for (int x = 0; x < _bw; x++)
                {
                    var c = Dot(x, y) + new Vector2(cellW * 0.5f, 0f);
                    int cx = x, cy = y;
                    var img = NewCell($"H_{x}_{y}", c,
                        new Vector2(cellW * 0.82f, edgeThick), out var btn);
                    img.color = EdgeUndrawn;
                    btn.onClick.AddListener(() => OnEdgeTapped(new EdgeId(EdgeKind.Horizontal, cx, cy)));
                    _hImg[x, y] = img;
                }

            // Vertical edges.
            for (int y = 0; y < _bh; y++)
                for (int x = 0; x <= _bw; x++)
                {
                    var c = Dot(x, y) + new Vector2(0f, cellH * 0.5f);
                    int cx = x, cy = y;
                    var img = NewCell($"V_{x}_{y}", c,
                        new Vector2(edgeThick, cellH * 0.82f), out var btn);
                    img.color = EdgeUndrawn;
                    btn.onClick.AddListener(() => OnEdgeTapped(new EdgeId(EdgeKind.Vertical, cx, cy)));
                    _vImg[x, y] = img;
                }

            // Dots (top layer).
            float dotSize = edgeThick * 1.4f;
            for (int y = 0; y <= _bh; y++)
                for (int x = 0; x <= _bw; x++)
                {
                    var img = NewCell($"Dot_{x}_{y}", Dot(x, y),
                        new Vector2(dotSize, dotSize), out _);
                    img.color = DotColor;
                }
        }

        private Image NewCell(string name, Vector2 anchoredPos, Vector2 size, out Button button)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(_boardArea, false);
            rt.anchorMin = rt.anchorMax = Vector2.zero; // measure from board bottom-left
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            var img = go.GetComponent<Image>();
            button = go.GetComponent<Button>();
            button.targetGraphic = img;
            return img;
        }

        private void OnEdgeTapped(EdgeId edge)
        {
            if (_busy || _game.IsGameOver) return;
            if (_game.CurrentPlayer != 0) return;            // human is player 0
            if (!_game.TryPlay(edge, out _)) return;         // illegal/taken -> ignore
            if (!_game.IsGameOver && _game.CurrentPlayer == 1)
                StartCoroutine(AiLoop());
        }

        private IEnumerator AiLoop()
        {
            _busy = true;
            Render();
            while (!_game.IsGameOver && _game.CurrentPlayer == 1)
            {
                yield return new WaitForSeconds(0.35f);
                var mv = _ai.Choose(_game);
                if (mv == null) break;
                _game.TryPlay(mv.Value, out _);
            }
            _busy = false;
            Render();
        }

        private void Render()
        {
            for (int y = 0; y <= _bh; y++)
                for (int x = 0; x < _bw; x++)
                    _hImg[x, y].color = EdgeColor(EdgeKind.Horizontal, x, y, _game.Board.HasHEdge(x, y));
            for (int y = 0; y < _bh; y++)
                for (int x = 0; x <= _bw; x++)
                    _vImg[x, y].color = EdgeColor(EdgeKind.Vertical, x, y, _game.Board.HasVEdge(x, y));

            for (int by = 0; by < _bh; by++)
                for (int bx = 0; bx < _bw; bx++)
                {
                    int owner = _game.Board.BoxOwner(bx, by);
                    _boxImg[bx, by].color = owner >= 0 && owner < BoxFill.Length
                        ? BoxFill[owner] : new Color(0, 0, 0, 0);
                }

            if (_status != null) _status.text = StatusText();
        }

        private Color EdgeColor(EdgeKind kind, int x, int y, bool drawn)
        {
            if (!drawn) return EdgeUndrawn;
            return _edgeOwner.TryGetValue((kind, x, y), out var p) && p < PlayerColor.Length
                ? PlayerColor[p] : Color.white;
        }

        private string StatusText()
        {
            int you = _game.Board.CountOwned(0);
            int cpu = _game.Board.CountOwned(1);
            string score = $"   You {you} : {cpu} CPU";
            if (_game.IsGameOver)
            {
                int w = _game.WinnerOrDraw();
                string verdict = w < 0 ? "Draw" : w == 0 ? "You win!" : "CPU wins";
                return verdict + score;
            }
            return (_busy ? "CPU thinking..." : "Your turn") + score;
        }
    }
}
