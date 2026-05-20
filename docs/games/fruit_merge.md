# Fruit Merge

Drop-and-merge puzzle, deterministic grid variant (no 2D physics).

## Rules

- 7-wide x 12-tall grid.
- The game shows a `NextFruit` (a low tier, 1-4) and an optional `HoldFruit`.
- Player picks a column; fruit falls to the lowest empty cell of that column.
- After landing, **merge passes**:
  - Find connected components of same-tier orthogonally-adjacent fruits.
  - Components of size 2+ collapse into a single (tier+1) fruit at the
    lowest cell of the component.
  - Apply gravity (cells above the cleared cells fall).
  - Repeat until no more merges (chain reactions).
- Game over when the chosen column is full after the drop.

## Scoring

- Each merge component awards `tier * count` points.
- Chains add up; an 11-tier fruit (the maximum) is the goal.

## Controls

- Tap a column to drop. The next fruit indicator shows what's coming.
- Hold button stashes the current fruit and brings up the previously held
  one (one slot, no queue).

## Multiplayer

- Each player runs their own grid, seeded identically so they see the same
  `NextFruit` sequence (parity).
- Score race: highest cumulative score after a timer wins.
- Future: high-tier merges send junk rows to opponents, similar to Color
  Blocks. (Multiplayer orchestrator delivered in milestone P.)

## Files

- `Games/FruitMerge/Scripts/Logic/` — grid, bag, engine, game session.
- `Games/FruitMerge/Scripts/FruitMergeModule.cs` — IGameModule binding.
