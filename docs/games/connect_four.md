# Connect Four

Two-player turn-based classic. First to line up **four discs** in a row,
column, or diagonal wins.

## Rules

- 7 wide x 6 tall grid.
- Two players (yellow and red). PlayerA always moves first.
- On your turn, tap a column; your disc drops to the lowest empty cell.
- A 4-in-a-row in **any** direction (horizontal, vertical, both diagonals)
  ends the game with your win.
- If the board fills with no winner, the game is a draw.

The board / win length is configurable, so the same engine can run
Gomoku-style variants (15x15, 5-in-a-row) without changes.

## Controls

- Tap a column header (or anywhere in the column) to drop.
- Long-press preview shows where it lands before you commit (UI sugar).

## Multiplayer (new pattern: turn-based)

Unlike the realtime games (Snakes / Maze Paint) or the parallel score-race
games (Color Blocks / Tetris / Fruit Merge), Connect Four is **turn-based**:

- Both peers run the same deterministic game.
- One player drops in column X, then broadcasts `MoveMessage(column, moveNumber)`.
- Receivers apply the move to their local copy. Out-of-order messages
  (e.g. on reconnect) are dropped via the moveNumber check.
- No host authority needed - the game state is fully determined by the
  ordered move history.

Seat assignment is **deterministic** by playerId (whoever's id sorts
first is PlayerA), so no extra handshake is required.

Also supported:
- `ResignMessage` - immediate game-over, opponent wins.
- `RematchMessage` - request a fresh game (UI accepts/declines).

## AI

`MinimaxConnectFourAI` is a depth-4 alpha-beta minimax. Heuristic:
- +/- 3 per cell in the central column (center control is decisive).
- +/- 5 per open run of length WinLength - 1 (one move from a win).

Branching factor is at most 7, so depth 4 is fast (a few ms per move).

## Files

- `Games/ConnectFour/Scripts/Logic/` - board, game with Result + Concede.
- `Games/ConnectFour/Scripts/Multiplayer/` - turn-based wire protocol.
- `Games/ConnectFour/Scripts/AI/` - minimax + CpuController.
- `Games/ConnectFour/Scripts/ConnectFourModule.cs` - IGameModule binding.
