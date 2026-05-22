# Art assets (optional)

Every game ships with a **procedural fallback look** (flat colors, circles,
rounded tiles, a colored emblem + glyph on the Hub). You can replace any of it
with real artwork by dropping PNGs into the right folder — no code changes
needed. Missing files just fall back to the procedural look.

## Where files go

```
unity/Assets/Resources/Art/Games/<gameId>/<name>.png
```

- It **must** be under a `Resources/` folder (that's how it loads at runtime).
- `<gameId>` is the game's id (same string used everywhere): `connect_four`,
  `reversi`, `number_merge`, `dots_and_boxes`, `snakes`, `tetris`,
  `fruit_merge`, `bomb_sweep`, `maze_paint`, `color_blocks`, `battleship`.
- `<name>` is from the table below (no extension in code; the file is `.png`).

Import settings don't matter much: the loader accepts a Sprite or a plain
texture. If you want crisp edges, set **Texture Type = Sprite (2D and UI)** in
the inspector, but it works either way.

## Filenames per game

Wired and working today:

| File | Used by | Notes |
|------|---------|-------|
| `icon.png` | **all games** | Hub card logo. Replaces the color circle + glyph. Square, ~256×256. |
| `tier1.png` … `tier11.png` | `fruit_merge` | One image per fruit tier (1 = smallest). Replaces the colored numbered circle. |

Reserved names (drop the files and tell me to wire them — a few lines each):

| Game | Suggested files |
|------|-----------------|
| connect_four | `disc_a.png` (red/you), `disc_b.png` (yellow/cpu) |
| reversi | `disc_black.png`, `disc_white.png` |
| number_merge (2048) | `tile_2.png`, `tile_4.png`, … `tile_2048.png` (by value) |
| snakes | `head.png`, `body.png`, `food.png` |
| tetris | `block_i.png`, `block_o.png`, `block_t.png`, `block_s.png`, `block_z.png`, `block_j.png`, `block_l.png` |
| bomb_sweep | `player.png`, `cpu.png`, `bomb.png`, `explosion.png`, `wall.png`, `soft.png`, `power_bombs.png`, `power_range.png`, `power_speed.png` |
| color_blocks | `block.png` (white, tinted per color) |
| battleship | `ship.png`, `hit.png`, `miss.png` |
| maze_paint | `tile.png` (optional; colors usually fine) |
| dots_and_boxes | `dot.png` (optional) |

## How to produce them

Any tool works (Midjourney / DALL·E / Stable Diffusion / an artist). Tips:
- Transparent background PNGs.
- Square canvas for icons/discs/tiles; the loader centers and preserves aspect.
- Keep a consistent style across one game's set.

## Example

To give Fruit Merge real fruit and a logo:

```
unity/Assets/Resources/Art/Games/fruit_merge/icon.png
unity/Assets/Resources/Art/Games/fruit_merge/tier1.png   (cherry)
unity/Assets/Resources/Art/Games/fruit_merge/tier2.png   (strawberry)
...
unity/Assets/Resources/Art/Games/fruit_merge/tier11.png  (watermelon)
```

Press Play — the fruit and the Hub logo are used automatically. Delete a file
and it reverts to the procedural circle. No rebuild required.
