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

All of these are **wired** — drop the PNG in and it shows up next Play, no
code changes. `icon.png` works for every game.

| File | Used by | Notes |
|------|---------|-------|
| `icon.png` | **all games** | Hub card logo. Replaces the color circle + glyph. Square, ~256×256. |
| `disc_a.png`, `disc_b.png` | connect_four | Your (red) and CPU (yellow) discs. |
| `disc_black.png`, `disc_white.png` | reversi | The two disc colors. |
| `tile_2.png`, `tile_4.png`, … `tile_2048.png` | number_merge (2048) | One per tile **value**. Replaces the numbered tile. |
| `tier1.png` … `tier11.png` | fruit_merge | One per fruit tier (1 = smallest). |
| `head.png`, `body.png`, `food.png` | snakes | Snake head, body segment, food. |
| `block_i/o/t/s/z/j/l.png` | tetris | One per tetromino. |
| `block.png` | color_blocks | One white block, tinted per color. |
| `player.png`, `cpu.png`, `bomb.png`, `explosion.png`, `wall.png`, `soft.png`, `power_bombs.png`, `power_range.png`, `power_speed.png` | bomb_sweep | Arena elements. |
| `ship.png`, `hit.png`, `miss.png` | battleship | Cell states (water stays procedural). |

Maze Paint and Dots and Boxes use flat colors and have no per-element art
hooks yet (ask if you want some).

Any file you omit simply keeps the current procedural look, so you can add art
one piece at a time.

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

---

# Shared UI art (buttons & popups)

The same drop-in system covers the common UI chrome. Files go in:

```
unity/Assets/Resources/Art/UI/<name>.png
```

| File | Used for | Notes |
|------|----------|-------|
| `button.png` | every button (Back, Restart, Rotate, Bomb, D-pad, Play Again, Home, Close, Solo/Same-device, ?, …) | A **white/neutral** rounded button shape. It's **tinted** by each button's color, so color coding survives. Make it 9-sliceable (see below). |
| `panel.png` | every popup window (game-over, settings, instructions, mode-select) | Shown as-is (its own colors). The window frame. |

If you don't add these, buttons fall back to rounded rectangles and panels to
rounded dark cards (what you see now).

### 9-slice (important for buttons & panels)

Buttons and panels stretch to many sizes. To keep corners crisp:

1. Select the imported PNG in Unity → Inspector.
2. **Texture Type = Sprite (2D and UI)** → **Sprite Editor** → drag the green
   **border** lines in from each edge (e.g. 24 px) so the corners don't stretch.
3. Apply.

If you skip this the image still works, it just stretches uniformly — fine for
flat or gradient designs, not for ones with rounded corners or borders.

---

# AI image-generation workflow

A repeatable way to produce every asset with an AI image tool (Midjourney /
DALL·E / Stable Diffusion / Firefly).

### 1. Lock a style first

Generate ONE reference (e.g. the Fruit Merge icon) until you like it, then reuse
the **same style sentence** in every prompt so the whole app matches. A good
generic style block:

> *flat vector game art, soft rounded shapes, bright saturated palette, subtle
> top-down gradient, thick clean outline, centered, on a transparent
> background, mobile game asset, no text, no shadow on the ground*

### 2. Output rules (apply to every image)

- **Format:** PNG with **transparent background** (alpha).
- **Shape:** square canvas (1:1). Icons/discs/tiles ~**512×512**; UI button/panel
  ~**512×512** too (they get 9-sliced).
- **Framing:** subject centered, small margin, nothing cropped at edges.
- **No text** baked in (the app draws numbers/labels itself).
- One subject per image.

### 3. Prompt template

```
<subject>, <style block from step 1>, square, transparent background, no text
```

Examples:

| Asset | `<subject>` |
|-------|-------------|
| fruit_merge/tier1 | a small red cherry |
| fruit_merge/tier6 | a green kiwi slice |
| fruit_merge/tier11 | a watermelon |
| fruit_merge/icon | app icon: a stack of merging fruit |
| snakes/head | a cute green snake head facing right |
| snakes/food | a shiny red apple |
| connect_four/disc_a | a glossy red game disc, top-down |
| reversi/disc_black | a glossy black othello disc, top-down |
| number_merge/tile_2048 | a rounded golden "2048" game tile **(no number text)** |
| tetris/block_i | a glossy cyan square game block |
| bomb_sweep/bomb | a classic round black cartoon bomb with a lit fuse |
| bomb_sweep/explosion | a bright orange cartoon explosion burst |
| battleship/ship | a top-down grey warship segment |
| UI/button | a blank rounded rectangle button, white, soft bevel |
| UI/panel | a rounded popup window panel, dark blue, soft border |

### 4. Drop in & check

1. Save each file with the **exact name** from the tables above, into the right
   folder (`Resources/Art/Games/<id>/` or `Resources/Art/UI/`).
2. (Recommended) set Texture Type = Sprite; for `button`/`panel` set 9-slice
   borders.
3. Press Play. Art appears automatically — no code, no rebuild.
4. Commit: `git add unity/Assets/Resources/Art` (includes the `.meta` files).

### 5. Batch tips

- Do one game's full set in a single session so the style stays consistent.
- For `number_merge` tiles and `tetris` blocks, generate one and recolor copies
  if your tool supports it — they're the same shape, different color.
- Keep the source/large versions somewhere outside the project; only the final
  PNGs need to live under `Resources/`.
