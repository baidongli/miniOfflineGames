# Reversi (Othello)

2-player turn-based classic. Sandwich opponent discs to flip them.

## Rules

- 8 x 8 board, starts with B/W/W/B in the four center cells.
- Black moves first.
- A legal placement: an empty cell where you'd flank at least one
  opponent disc between your new disc and another of your own. Flank
  works in **all 8 directions** (4 cardinal + 4 diagonal).
- All flanked opponent discs flip to your color.
- If you have no legal move, you **pass**. If both players pass in
  succession, the game ends.
- Game also ends when the board is full.
- Winner: most discs of own color. Ties = draw.

## Controls

- Tap a highlighted legal cell to play.
- "Pass" button activates only when no legal move exists.

## Multiplayer (turn-based)

Identical pattern to Connect Four:
- Deterministic seat assignment by sorting PlayerIds; smaller id = Black
  (gets the first move).
- One `RVMoveMessage` per turn, plus `RVPassMessage` for forced passes.
- MoveNumber-tracked de-duplication on reconnect.
- Resign / Rematch messages also supported.

## AI

`MinimaxReversiAI` is a depth-3 alpha-beta minimax. Evaluation function:
- Positional weight matrix: corners +100, X-squares (diagonal to corners)
  -50, edges ~10, interior ~-1. Corners are decisive in Reversi.
- Mobility differential (legal-move count) +/-5 per move.
- Terminal positions overwhelm positional score with disc differential.

## Files

- `Games/Reversi/Scripts/Logic/` - board, game with FlipsFor / Pass /
  Concede.
- `Games/Reversi/Scripts/Multiplayer/` - turn-based wire protocol with
  pass support.
- `Games/Reversi/Scripts/AI/` - minimax with positional heuristic.
- `Games/Reversi/Scripts/ReversiModule.cs` - IGameModule binding.
