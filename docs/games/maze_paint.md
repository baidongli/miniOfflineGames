# Maze Paint

Paper.io-style territory grabber.

## Rules

- 24x24 grid.
- Each player has a small starting "home" territory at a board corner
  (3x3 painted cells in their color).
- Player controls a head that moves one cell per tick (~8Hz).
- When the head is on the player's own territory, no trail is laid.
- When the head leaves home, each subsequent cell becomes part of the
  player's **active trail** (still not "owned" yet).
- When the head returns to owned territory, the active trail closes:
  - Trail cells become owned.
  - Any cells **enclosed** by (territory + trail) become owned too
    (flood-fill from the board edges treats territory and trail as walls;
    unreachable cells = enclosed).

## Death

- Head goes off the board.
- Head steps onto its own active trail.
- Another player's head steps onto your active trail → **you** die,
  their trail wiped.

## Win Condition

- Last player alive wins. If a timer is used, highest percentage of
  owned cells wins.

## Multiplayer Sync

Host-authoritative with simple snapshot model (no prediction yet because
8Hz is forgiving):

- Each tick: host applies queued inputs, runs `MazePaintEngine.Step`,
  broadcasts board state to clients.
- Client input is sent to host as a direction change.

(Orchestrator delivered in milestone P; see `Games/MazePaint/Scripts/Multiplayer/`.)

## Files

- `Games/MazePaint/Scripts/Logic/` — board (owner + trail layers),
  player, engine with flood-fill capture.
- `Games/MazePaint/Scripts/MazePaintModule.cs` — IGameModule binding.
