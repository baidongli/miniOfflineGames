# Battleship

The classic guess-and-sink, 2 players, 10×10 grid. Each player secretly
places a fleet, then they take turns calling shots. First to sink all
of opponent's ships wins.

This is the only game in the collection with **hidden per-player
state** — the 5th multiplayer sync pattern. See
`docs/multiplayer_patterns.md` for the cross-game model.

## Rules

### Fleet (standard)

| Ship | Length |
|---|---|
| Carrier | 5 |
| Battleship | 4 |
| Cruiser | 3 |
| Submarine | 3 |
| Destroyer | 2 |

Total occupied cells: **17**.

### Setup phase

- Each player privately places all 5 ships on their own 10×10 grid.
- Ships are horizontal or vertical, no overlap, no diagonal.
- A player taps **Ready** when they're done.
- The game enters Playing phase when **both** players are ready.

### Playing phase

- Players alternate shots. Lex-smaller PlayerId goes first.
- On your turn, tap a cell on the **opponent's grid**.
- The opponent's device computes the result and replies:
  - **Miss**: no ship in that cell.
  - **Hit**: a ship was there.
  - **Sunk**: that hit completed a ship; the response includes the
    full set of cells so the shooter can render the dead ship.
- One shot per turn (classic rules — no bonus shot on hit).
- Game ends when one player's entire fleet is sunk.

## Wire protocol (hidden-state pattern)

Asymmetric, two messages per shot:

1. Shooter → target: `ShotFired(x, y, moveNumber)`.
2. Target's device resolves the shot against its **OwnFleet** (private).
3. Target → shooter: `ShotResult(x, y, result, sunkShipCells?)`.
4. Shooter updates its **OpponentTracker** (built up from received
   ShotResults).

The shooter never knows opponent ship positions until they get a Sunk
response — and even then, only for the just-sunk ship.

## Controls

- Setup: drag ships onto the grid; rotate with a tap; **Ready** button
  when all 5 are placed.
- Playing: tap any unshot cell on the opponent's panel.

## AI

`SimpleBattleshipAI`:

- **Placement**: random valid orientation per ship.
- **Shooting**: two modes:
  - **Hunt** — scan a checkerboard pattern (every other cell, since
    the smallest ship covers 2 cells, no ship can hide on the "off"
    pattern entirely).
  - **Target** — once a Hit lands, queue the 4 cardinal neighbors and
    shoot those before returning to hunt mode. Sunk clears the queue.

Lacks parity-tracking (after a hit, AI doesn't deduce the ship's
orientation) — that's room for a v2 AI.

## Files

- `Games/Battleship/Scripts/Logic/`:
  - `ShipKind.cs` — fleet definitions + phase enum.
  - `BattleshipBoard.cs` — 10×10 grid with placement + hit tracking.
  - `BattleshipGame.cs` — per-peer session with OwnFleet (private) +
    OpponentTracker (learned from responses).
- `Games/Battleship/Scripts/Multiplayer/`:
  - `BattleshipMessages.cs` — 0x80 ShipsReady, 0x81 ShotFired,
    0x82 ShotResult, 0x83 Resign.
  - `MultiplayerBattleship.cs` — asymmetric orchestrator.
- `Games/Battleship/Scripts/AI/SimpleBattleshipAI.cs` — placement +
  hunt/target.
- `Games/Battleship/Scripts/BattleshipModule.cs` — IGameModule binding.
