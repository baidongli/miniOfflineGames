# Color Blocks

Block-blast / 1010!-style placement puzzle.

## Rules

- 10x10 grid, initially empty.
- Player gets a "hand" of 3 pieces from a ~25-shape catalog (dots, lines,
  squares, L's, T's).
- Player drags a piece to any valid position on the grid; cells under the
  piece become filled with that piece's color.
- A filled row OR column is cleared and scores points.
- When all three pieces in the hand are placed, a new hand of three is dealt.
- Game over when no piece in the current hand can fit anywhere.

## Scoring

- 1 point per cell placed.
- 10 points per line (row or col) cleared.
- Combo bonus when 2+ lines clear on the same move:
  - 2 lines: +20
  - 3 lines: +50
  - 4 lines: +100
  - 5+: +100 + 60 per extra

## Controls

- Touch and drag a piece from the hand onto the grid.
- Snap to a valid placement; semi-transparent ghost shows where it'll land.
- Drop = commit. Drag off = cancel.

## Multiplayer

- Each player has their own grid.
- A move that clears 2+ lines sends `(linesCleared - 1)` **junk rows** to
  every opponent.
- A junk row pushes everything up; the new bottom row is filled except for
  one randomly placed gap.
- Game over for a player when:
  - their hand has no valid placement, OR
  - a junk row pushes cells off the top of the grid.
- Last player standing wins; tie-breaker is final score.

## Files

- `Games/ColorBlocks/Scripts/Logic/` — pure rules + scoring.
- `Games/ColorBlocks/Scripts/Multiplayer/` — wire messages + orchestrator.
- `Games/ColorBlocks/Scripts/ColorBlocksModule.cs` — IGameModule binding.
