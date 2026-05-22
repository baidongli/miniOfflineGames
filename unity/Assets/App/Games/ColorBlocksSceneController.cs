using MiniGames.App.Bootstrap;
using MiniGames.GameModule;
using MiniGames.Games.ColorBlocks;
using MiniGames.Games.ColorBlocks.Logic;
using MiniGames.Networking.Session;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniGames.App.Games
{
    /// <summary>
    /// Solo Color Blocks (1010!/block-blast style) in its own scene. 10x10
    /// board, a hand of 3 pieces. Drag a piece onto the board; it centers on
    /// the pointer with a green/red validity preview, and drops when valid.
    /// Filling a full row or column clears it.
    /// </summary>
    public sealed class ColorBlocksSceneController : MonoBehaviour
    {
        [SerializeField] private RectTransform _boardGrid;  // GridLayoutGroup, 10 cols
        [SerializeField] private RectTransform _handArea;   // holds the 3 slots
        [SerializeField] private TMP_Text _status;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _restartButton;

        private const int HandSize = 3;
        private const float MiniCell = 38f;

        private static readonly Color SlotEmpty = new Color(0.16f, 0.16f, 0.20f);
        private static readonly Color SlotBg = new Color(0.20f, 0.21f, 0.26f);
        private static readonly Color PreviewOk = new Color(0.35f, 0.80f, 0.45f);
        private static readonly Color PreviewBad = new Color(0.85f, 0.35f, 0.35f);
        private static readonly Color[] PieceColor =
        {
            new Color(0.16f, 0.16f, 0.20f),  // 0 empty
            new Color(0.90f, 0.36f, 0.36f),  // 1 red
            new Color(0.95f, 0.62f, 0.28f),  // 2 orange
            new Color(0.35f, 0.62f, 0.93f),  // 3 blue
            new Color(0.40f, 0.80f, 0.46f),  // 4 green
            new Color(0.70f, 0.45f, 0.90f),  // 5 purple
            new Color(0.30f, 0.78f, 0.74f),  // 6 teal
            new Color(0.93f, 0.80f, 0.32f),  // 7 yellow
        };

        private ColorBlocksModule _module;
        private ColorBlocksGame _game;
        private Image[,] _cells;
        private int _n;
        private RectTransform[] _slots;
        private int _dragging = -1;

        private void Start()
        {
            _module = new ColorBlocksModule();
            _module.StartSolo(BuildContext());
            _game = _module.SoloGame;
            _n = BoardState.Size;

            BuildCells();
            BuildSlots();
            if (_backButton != null) _backButton.onClick.AddListener(() => SceneManager.LoadScene("Hub"));
            if (_restartButton != null) _restartButton.onClick.AddListener(() => SceneManager.LoadScene("ColorBlocks"));
            Loc.Label(_backButton, "ui.back");
            InstructionsOverlay.AttachButton((RectTransform)transform, "color_blocks");
            Loc.Label(_restartButton, "ui.restart");

            RenderBoard();
            RenderHand();
            RenderStatus();
        }

        private static GameContext BuildContext()
        {
            if (AppBootstrap.Services != null)
                return AppBootstrap.BuildContext(NullSendChannel.Instance);
            return new GameContext(null, null, null, null, NullSendChannel.Instance, "local");
        }

        // ---- build ----

        private void BuildCells()
        {
            _cells = new Image[_n, _n];
            for (int row = 0; row < _n; row++)
            {
                int y = row; // top-left origin: row 0 == y 0 == top
                for (int x = 0; x < _n; x++)
                {
                    var go = new GameObject($"C_{x}_{y}", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(_boardGrid, false);
                    var img = go.GetComponent<Image>();
                    img.raycastTarget = false; // pieces are dragged from the hand, not the board
                    img.color = SlotEmpty;
                    Shapes.Rounded(img);
                    _cells[x, y] = img;
                }
            }
        }

        private void BuildSlots()
        {
            _slots = new RectTransform[HandSize];
            for (int i = 0; i < HandSize; i++)
            {
                var go = new GameObject($"Slot_{i}", typeof(RectTransform), typeof(Image));
                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(_handArea, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(220f, 220f);
                rt.anchoredPosition = new Vector2((i - 1) * 250f, 0f);
                go.GetComponent<Image>().color = SlotBg;

                var hp = go.AddComponent<HandPieceView>();
                hp.HandIndex = i;
                hp.BeginDrag = OnBeginDrag;
                hp.Drag = OnDrag;
                hp.EndDrag = OnEndDrag;

                _slots[i] = rt;
            }
        }

        // ---- drag ----

        private void OnBeginDrag(int handIndex)
        {
            if (_game.IsGameOver || _game.Hand[handIndex] == null) { _dragging = -1; return; }
            _dragging = handIndex;
        }

        private void OnDrag(int handIndex, Vector2 screenPos)
        {
            if (_dragging < 0) return;
            RenderBoard();
            if (TryGetOrigin(handIndex, screenPos, out int ox, out int oy))
                ShowPreview(_game.Hand[handIndex], ox, oy);
        }

        private void OnEndDrag(int handIndex, Vector2 screenPos)
        {
            if (_dragging < 0) return;
            _dragging = -1;
            var shape = _game.Hand[handIndex];
            if (TryGetOrigin(handIndex, screenPos, out int ox, out int oy)
                && _game.TryPlay(handIndex, ox, oy, out var r))
            {
                Sfx.Play(r.TotalLinesCleared > 0 ? "clear" : "place");
                if (shape != null)
                    foreach (var c in shape.Cells)
                        UiTween.Pop(_cells[ox + c.X, oy + c.Y].rectTransform);
            }

            RenderBoard();
            RenderHand();
            RenderStatus();
        }

        // Maps the pointer to a board origin so the piece centers on the finger.
        private bool TryGetOrigin(int handIndex, Vector2 screenPos, out int ox, out int oy)
        {
            ox = oy = 0;
            var shape = _game.Hand[handIndex];
            if (shape == null) return false;
            if (!TryGetCellUnderPointer(screenPos, out int px, out int py)) return false;
            ox = px - shape.Width / 2;
            oy = py - shape.Height / 2;
            return true;
        }

        private bool TryGetCellUnderPointer(Vector2 screenPos, out int cx, out int cy)
        {
            for (int y = 0; y < _n; y++)
                for (int x = 0; x < _n; x++)
                    if (RectTransformUtility.RectangleContainsScreenPoint(
                            _cells[x, y].rectTransform, screenPos, null))
                    { cx = x; cy = y; return true; }
            cx = cy = -1;
            return false;
        }

        private void ShowPreview(PieceShape shape, int ox, int oy)
        {
            bool ok = BoardEngine.CanPlace(_game.Board, shape, ox, oy);
            var tint = ok ? PreviewOk : PreviewBad;
            foreach (var c in shape.Cells)
            {
                int x = ox + c.X, y = oy + c.Y;
                if (x >= 0 && y >= 0 && x < _n && y < _n) _cells[x, y].color = tint;
            }
        }

        // ---- render ----

        private void RenderBoard()
        {
            for (int y = 0; y < _n; y++)
                for (int x = 0; x < _n; x++)
                    _cells[x, y].color = ColorFor(_game.Board.Get(x, y));
        }

        private void RenderHand()
        {
            for (int i = 0; i < HandSize; i++)
            {
                var slot = _slots[i];
                for (int c = slot.childCount - 1; c >= 0; c--) Destroy(slot.GetChild(c).gameObject);

                var shape = _game.Hand[i];
                if (shape == null) continue;

                float w = shape.Width * MiniCell, h = shape.Height * MiniCell;
                foreach (var cell in shape.Cells)
                {
                    var go = new GameObject("m", typeof(RectTransform), typeof(Image));
                    var rt = go.GetComponent<RectTransform>();
                    rt.SetParent(slot, false);
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(MiniCell - 4f, MiniCell - 4f);
                    float lx = -w / 2f + cell.X * MiniCell + MiniCell / 2f;
                    float ly = h / 2f - cell.Y * MiniCell - MiniCell / 2f;
                    rt.anchoredPosition = new Vector2(lx, ly);
                    var img = go.GetComponent<Image>();
                    img.raycastTarget = false; // let drags hit the slot
                    img.color = ColorFor(shape.ColorId);
                    Shapes.Rounded(img);
                }
            }
        }

        private void RenderStatus()
        {
            if (_status == null) return;
            string over = $"{Loc.T("result.game_over")}   {Loc.T("ig.score")} {_game.Score}";
            _status.text = _game.IsGameOver ? over : $"{Loc.T("ig.score")} {_game.Score}";
            if (_game.IsGameOver)
            {
                bool rec = BestScores.Report("color_blocks", _game.Score);
                GameOverlay.Show(over + BestScores.Suffix("color_blocks", rec));
            }
        }

        private static Color ColorFor(byte id)
            => id < PieceColor.Length ? PieceColor[id] : Color.gray;
    }
}
