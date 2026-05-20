# Bomb Sweep

The 4-player local-multiplayer chaos. Drop bombs, dodge blasts, the
last one alive wins.

## Rules

- 13 x 11 arena. Border is **hard wall**. Every (even, even) interior
  cell is also hard wall, leaving a classic checkerboard arena.
- ~65% of remaining empty cells start as **soft blocks**.
- Each player spawns at a corner with a small safe zone.
- On your turn (which is "always" - this is realtime), you can move in
  4 directions or place a bomb on the cell you're standing on.

## Bombs

- Bombs explode 3 seconds (24 ticks at 8Hz) after placement.
- Explosion is a cross extending **Range** cells in each direction.
- Hard walls stop explosions; soft blocks stop explosions but get
  destroyed in the process.
- A bomb in another bomb's blast detonates **immediately** (chain).
- Any player standing on a lit explosion cell dies.

## Power-ups

When a soft block is destroyed, 30% chance to drop a power-up:

| Pickup | Effect |
|---|---|
| Bombs+ | +1 to your max simultaneous bombs |
| Range+ | +1 to your explosion range |
| Speed+ | -1 to ticks-per-cell (capped at 1) |

## Death / Win

- Touching a lit explosion cell = death.
- Last player standing wins. In a 1-player game, you "win" by surviving
  some target time (TBD).

## Controls

- D-pad / swipe to move (4 directions, no diagonals).
- A button (or dedicated UI) to drop a bomb.

## Multiplayer

Host-authoritative, same pattern as Snakes / Maze Paint:

- Host advances simulation at 8 Hz (`BombSweepEngine.Step`).
- Clients send `InputCmd(heading, placeBomb)` on input change.
- Host broadcasts full `BSSnapshot` each tick.
- Clients apply input locally for responsiveness, snap to host snapshot
  on arrival.

## AI

`SimpleBombSweepAI` prioritizes:
1. Flee if standing on / adjacent to a future blast cell.
2. Place a bomb if a soft block or enemy is adjacent and we have a
   bomb available (then immediately move to a safe cell).
3. Otherwise wander toward the nearest soft block (chasing power-ups).

## Files

- `Games/BombSweep/Scripts/Logic/` - board generation, player, bomb,
  explosion, engine with chain reactions.
- `Games/BombSweep/Scripts/Multiplayer/` - snapshot serialization +
  orchestrator.
- `Games/BombSweep/Scripts/AI/` - heuristic + CpuController.
- `Games/BombSweep/Scripts/BombSweepModule.cs` - IGameModule binding.
