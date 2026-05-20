using System;
using System.Collections.Generic;

namespace MiniGames.Games.Tetris.Logic
{
    public struct LockResult
    {
        public bool Locked;
        public int LinesCleared;
        public int ScoreAwarded;
        public int AttackLines;
        public bool GameOver;
    }

    /// <summary>
    /// One Tetris session: board + active piece + bag + score / level.
    /// Pure C#, no Unity. The driver (UI or AI) calls TryMove*/TryRotate
    /// for input, and Tick / SoftDrop / HardDrop for falling. LockPiece
    /// is called automatically when a downward move fails.
    /// </summary>
    public sealed class TetrisGame
    {
        public readonly TetrisBoard Board = new TetrisBoard();
        private readonly TetrisBag _bag;

        public TetrominoType Current { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Rotation { get; private set; }

        public TetrominoType Next { get; private set; }
        public TetrominoType Held { get; private set; }
        public bool HasUsedHoldThisTurn { get; private set; }

        public int Score { get; private set; }
        public int Lines { get; private set; }
        public int Level => 1 + Lines / 10;
        public bool IsGameOver { get; private set; }

        public event Action<LockResult> Locked;
        public event Action GameOver;

        public TetrisGame(int seed)
        {
            _bag = new TetrisBag(seed);
            Next = _bag.Next();
            SpawnNext();
        }

        // --- input ---

        public bool TryMoveLeft() => TryShift(-1, 0);
        public bool TryMoveRight() => TryShift(1, 0);

        public bool TrySoftDrop()
        {
            bool ok = TryShift(0, -1);
            if (ok) Score += ScoringRules.SoftDropPointsPerCell;
            return ok;
        }

        public LockResult HardDrop()
        {
            int dropped = 0;
            while (TryShift(0, -1)) dropped++;
            Score += dropped * ScoringRules.HardDropPointsPerCell;
            return Lock();
        }

        public bool TryRotate(int dir)
        {
            // Basic wall-kick: try the rotation in place, then nudge L/R by 1, then 2.
            int newRot = ((Rotation + dir) % 4 + 4) % 4;
            int[] kicks = { 0, -1, 1, -2, 2 };
            foreach (var kx in kicks)
            {
                if (CanPlace(Current, X + kx, Y, newRot))
                {
                    X += kx;
                    Rotation = newRot;
                    return true;
                }
            }
            return false;
        }

        public void Hold()
        {
            if (HasUsedHoldThisTurn) return;
            HasUsedHoldThisTurn = true;
            if (Held == TetrominoType.None)
            {
                Held = Current;
                SpawnNext();
            }
            else
            {
                var swap = Held;
                Held = Current;
                SpawnSpecific(swap);
            }
        }

        /// <summary>Gravity tick: try to fall by 1; lock if blocked.</summary>
        public LockResult Tick()
        {
            if (IsGameOver) return new LockResult { Locked = false, GameOver = true };
            if (TryShift(0, -1)) return default;
            return Lock();
        }

        /// <summary>Apply incoming junk rows from an opponent (host-side or in solo modes, used by the MP orchestrator).</summary>
        public bool ReceiveJunk(int rows, byte junkColor, int rngSeed)
        {
            var rng = new System.Random(rngSeed);
            bool overflow = Board.PushJunkRows(rows, junkColor, rng);
            // Make sure the active piece isn't now intersecting blocks. If it is,
            // shift it up; if that fails, it's game over.
            while (!CanPlace(Current, X, Y, Rotation))
            {
                Y++;
                if (Y + 3 >= TetrisBoard.TotalHeight) { GameOverNow(); return true; }
            }
            if (overflow) GameOverNow();
            return overflow;
        }

        // --- internals ---

        private bool TryShift(int dx, int dy)
        {
            if (CanPlace(Current, X + dx, Y + dy, Rotation))
            {
                X += dx; Y += dy;
                return true;
            }
            return false;
        }

        private bool CanPlace(TetrominoType type, int x, int y, int rotation)
        {
            var cells = TetrominoShapes.Cells(type, rotation);
            foreach (var (cx, cy) in cells)
            {
                int bx = x + cx, by = y + cy;
                if (bx < 0 || bx >= TetrisBoard.Width) return false;
                if (by < 0) return false;
                if (by < TetrisBoard.TotalHeight && Board.Get(bx, by) != 0) return false;
            }
            return true;
        }

        private LockResult Lock()
        {
            // Stamp current piece onto the board.
            var cells = TetrominoShapes.Cells(Current, Rotation);
            foreach (var (cx, cy) in cells) Board.Set(X + cx, Y + cy, (byte)Current);

            // Detect and clear full rows.
            var cleared = new List<int>();
            // Only need to check rows the piece touches.
            var touched = new HashSet<int>();
            foreach (var (_, cy) in cells) touched.Add(Y + cy);
            foreach (var row in touched)
                if (row >= 0 && row < TetrisBoard.TotalHeight && Board.IsRowFull(row))
                    cleared.Add(row);
            cleared.Sort();
            if (cleared.Count > 0) Board.RemoveRows(cleared);

            int gained = ScoringRules.LineClearScore(cleared.Count, Level);
            Score += gained;
            Lines += cleared.Count;
            HasUsedHoldThisTurn = false;

            var result = new LockResult
            {
                Locked = true,
                LinesCleared = cleared.Count,
                ScoreAwarded = gained,
                AttackLines = ScoringRules.AttackLines(cleared.Count)
            };

            // Spawn next; if blocked, game over.
            SpawnNext();
            if (!CanPlace(Current, X, Y, Rotation))
            {
                result.GameOver = true;
                GameOverNow();
            }
            Locked?.Invoke(result);
            return result;
        }

        private void SpawnNext()
        {
            SpawnSpecific(Next);
            Next = _bag.Next();
        }

        private void SpawnSpecific(TetrominoType t)
        {
            Current = t;
            Rotation = 0;
            X = 3;   // centered for a 4-wide piece on a 10-wide board
            Y = TetrisBoard.VisibleHeight - 2; // top of visible
        }

        private void GameOverNow()
        {
            if (IsGameOver) return;
            IsGameOver = true;
            GameOver?.Invoke();
        }
    }
}
