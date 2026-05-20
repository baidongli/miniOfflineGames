# Tetris

Classic falling-tetrominoes with modern guideline-adjacent rules.

## Rules

- 10-wide x 20-tall visible board (+ 4-row hidden spawn buffer).
- Pieces drawn from a **7-bag**: every 7 pieces = one of each tetromino,
  in shuffled order. Same seed produces the same sequence on every device.
- The active piece falls one row per gravity tick; the tick rate scales
  with level.
- Player can move left/right, soft-drop (1 row, +1 point each), rotate
  CW/CCW with basic wall-kick, or hard-drop (slam to bottom, +2 points
  per row, instant lock).
- Hold: stash the current piece, swap with held one. One hold per piece.
- A row that's completely filled clears; rows above fall down. Multi-
  line clears score more per line.
- Game over when a new piece can't spawn (top-out).

## Scoring

| Lines cleared | Score (× level) |
|---|---|
| 1 (Single) | 100 |
| 2 (Double) | 300 |
| 3 (Triple) | 500 |
| 4 (Tetris) | 800 |

Soft drop: +1 per row. Hard drop: +2 per row.

Level = 1 + (totalLinesCleared / 10).

## Controls

- Tap left/right halves or swipe to move.
- Tap or swipe up to rotate.
- Swipe down for soft drop; flick down for hard drop.
- Tap "Hold" to swap with held piece.

## Multiplayer attack

| Lines cleared | Junk rows sent |
|---|---|
| 1 | 0 |
| 2 | 1 |
| 3 | 2 |
| 4 (Tetris) | 4 |

A junk row is full except for one randomly-placed gap. The gap column is
derived from a seed in the attack message so all receivers compute the
same row (multiplayer parity).

Game over for you when:
- a new piece can't spawn, OR
- incoming junk overflows out the top.

Last player standing wins; ties broken by score.

## Files

- `Games/Tetris/Scripts/Logic/` — Board, Bag, Game, Shapes, ScoringRules.
- `Games/Tetris/Scripts/Multiplayer/` — wire messages + orchestrator.
- `Games/Tetris/Scripts/AI/` — Dellacherie-style heuristic + CpuController.
- `Games/Tetris/Scripts/TetrisModule.cs` — IGameModule binding.
