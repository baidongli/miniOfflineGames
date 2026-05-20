# How to Play

Quick-reference rules for everyone in the room.

## Setup (multiplayer)

Same-device or Nearby — both work without internet.

**Same device (1-4 players, one phone/tablet)**
1. Pick a game from the Hub.
2. Tap **Same device**.
3. Choose number of players (2-4). The screen splits into that many
   regions; each player taps in their own region.

**Nearby (1-4 players, separate phones in the same room)**
1. One player picks a game and taps **Nearby → Host**.
2. The other players open the app, pick the same game, tap **Nearby → Join**.
3. Joiners see the host's name in a list; tap to connect.
4. When everyone is ready, the host taps **Start**.

No internet required, no Bluetooth pairing screen, no manual IPs. The
app uses Google Nearby Connections (Wi-Fi peer-to-peer with a Bluetooth
fallback). Make sure Wi-Fi and Bluetooth are turned on; location
permission may be requested on older Android phones.

---

## Color Blocks

**Goal**: clear rows and columns on a 10×10 grid by placing 3 pieces at a
time. Don't get stuck with no legal placement.

**Controls**
- Drag a piece from the tray to a grid square.
- Snap preview shows where it'll land. Release to commit.
- Drag back out to cancel.

**Scoring**
- 1 point per cell placed.
- 10 points per cleared line (row OR column).
- Combo bonuses for clearing multiple lines at once (+20 / +50 / +100).

**Multiplayer attack**
- Clear 2+ lines in one move → opponents get junk rows pushed up.
- Junk rows are almost-full bottom rows with a single gap each.
- You lose when you can't fit any piece, OR junk pushes cells off the top.

---

## Snakes

**Goal**: stay alive longer than everyone else. Eat food to grow.

**Controls**
- Swipe in a direction OR tap directional arrows in your screen region.
- You can't reverse direction (180° turn is rejected).

**Death**
- Run into a wall, your own body, or another snake's body.

**Multiplayer**
- 2-4 snakes spawn at board corners, heading toward the middle.
- Last snake alive wins. Head-on collisions kill both.

---

## Maze Paint

**Goal**: own more cells than anyone else. Paint by drawing closed loops.

**Controls**
- Swipe to steer; your head moves one cell per tick.
- When you're on your own territory, no trail is laid.
- When you step off, you start a trail (visible faint stripe).
- Return to your territory → the trail and **any cells it encloses**
  become yours.

**Death**
- Walk off the board.
- Step on your own trail.
- Get **hit by another player while you have a trail out** — they cut
  your loop and your trail evaporates.

**Multiplayer**
- 2-4 players, each at a corner with a small starter territory.
- Last alive wins. (With a timer: highest % territory wins.)

---

## Fruit Merge

**Goal**: grow the biggest fruit by merging smaller ones.

**Controls**
- Tap a column to drop the **Next Fruit** there. It falls to the bottom
  of that column.
- Tap **Hold** to stash the current fruit; the previously held one
  becomes Next.

**Merging**
- Two or more same-tier fruits that touch (up/down/left/right) collapse
  into one fruit of the next tier.
- Merges chain: a new fruit can immediately match its neighbors, and
  so on.

**Game over**
- A column fills past the top → you're out.

**Multiplayer**
- Everyone gets the same `Next Fruit` sequence (deterministic seed).
- Highest score after a timer wins. Watch the others' grids on the side
  of the screen for pressure.

---

## Quick reference: which game suits the moment

| Vibe | Pick |
|---|---|
| "I want a long, calm puzzle" | Color Blocks |
| "We want a chaotic 2-minute brawl" | Snakes |
| "We want a strategic territory fight" | Maze Paint |
| "Solo airplane flight" | Fruit Merge |

Have fun. Don't drop the host's phone.
