# Snakes

Classic snake with real-time multiplayer.

## Rules

- 20x20 grid.
- Each player controls a snake. Snakes start 3 segments long, spawn at
  board corners heading inward.
- Snake moves one cell per tick (10Hz) in its heading direction.
- Player input changes the heading; 180-degree U-turns are rejected.
- Food cells (3 at a time) appear at random empty positions. Eating a food
  grows the snake by one segment and respawns a new food.
- A snake dies when its head:
  - leaves the board, OR
  - lands on its own body, OR
  - lands on any other snake's body (including head).
- Last snake alive wins. Solo: survive as long as possible / eat the most.

## Controls

- Swipe in a direction OR press a directional button.
- Hold-to-turn not used; input applies at the next tick boundary.

## Multiplayer Sync

- Host-authoritative simulation.
- Each tick (10Hz):
  - Host applies any inputs received from clients since the last tick.
  - Host calls `SnakesEngine.Step` on the canonical state.
  - Host broadcasts a `SnakeSnapshot` to all clients.
- Client-side prediction: the local player's direction change applies
  immediately to the client's own copy of the state, so input feel is
  not gated on round-trip.
- When a snapshot arrives, client replaces its state with the host's, then
  re-applies the most recent local intent so the user's most recent turn
  isn't undone by an in-flight snapshot.
- Bandwidth budget per client: < 8 KB/s for snapshots at 10Hz.
- Input latency target: < 80ms (predicted) / < 200ms (corrected).

## Files

- `Games/Snakes/Scripts/Logic/` — engine, state, direction, food.
- `Games/Snakes/Scripts/Multiplayer/` — wire messages, orchestrator,
  serialization.
- `Games/Snakes/Scripts/SnakesModule.cs` — IGameModule binding.
