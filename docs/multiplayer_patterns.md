# Multiplayer Sync Patterns

Across the 11 games shipped, we use **five** distinct multiplayer
synchronization patterns. Picking the right one is the single most
important decision when adding a new game.

## Decision tree

```
Is the game realtime (player input matters every fraction of a second,
not just on discrete actions)?
│
├── Yes  ─── Snakes / Maze Paint / Bomb Sweep
│           Pattern (3): host-authoritative tick + client prediction.
│
└── No   ─── Discrete actions only.
            │
            ├── Does each player have their own playing field?
            │   │
            │   ├── Yes  ─── Color Blocks / Tetris / Fruit Merge / 2048
            │   │           Patterns (1) or (2):
            │   │             - (1) Event-synced attacks: moves can
            │   │                   affect opponents (junk lines).
            │   │             - (2) Pure score race: same seeded
            │   │                   content; broadcast progress only.
            │   │
            │   └── No   ─── Shared board / state.
            │                │
            │                ├── Is any per-player state SECRET?
            │                │   │
            │                │   ├── Yes  ─── Battleship
            │                │   │           Pattern (5): turn-based +
            │                │   │           hidden state, asymmetric
            │                │   │           shot/result exchange.
            │                │   │
            │                │   └── No   ─── Connect Four / Reversi /
            │                │                Dots and Boxes
            │                │                Pattern (4): turn-based
            │                │                broadcast, deterministic
            │                │                game state.
```

## (1) Event-synced attacks

**Used by**: Color Blocks, Tetris.

- Each peer runs its own game.
- A move (line clear) emits an `Attack` message broadcast to opponents.
- Receivers apply the attack to their local board (e.g. push junk rows).
- Per-player game states diverge naturally.

Pros: simple, no host needed, robust to packet loss for spectator data.

Cons: state can desync if attack messages are dropped; must be reliable.

**Files**:
- `Games/ColorBlocks/Scripts/Multiplayer/MultiplayerColorBlocks.cs`
- `Games/Tetris/Scripts/Multiplayer/MultiplayerTetris.cs`

## (2) Pure score race

**Used by**: Fruit Merge, 2048.

- Each peer runs an identically-seeded game so every player sees the
  same `NextFruit` / spawn sequence.
- Broadcast `Progress(score, ...)` after each move for opponent UI.
- Broadcast `DiedOut(finalScore)` on game-over.
- The only variable is **player skill** - same board, same RNG.

Pros: fairest possible competition for puzzle games. Cheap on bandwidth
(progress messages are tiny).

Cons: doesn't compose with mid-game attacks (would require sync).

**Files**:
- `Games/FruitMerge/Scripts/Multiplayer/MultiplayerFruitMerge.cs`
- `Games/NumberMerge/Scripts/Multiplayer/NumberMergeMultiplayer.cs`

## (3) Host-authoritative tick + client prediction

**Used by**: Snakes, Maze Paint, Bomb Sweep.

- One player is the **host**. The host owns the canonical game state
  and advances it at a fixed tick rate (8-10 Hz).
- Clients send `InputCmd(direction, ...)` whenever input changes.
- Each tick, host:
  1. Applies any queued inputs.
  2. Calls `<Game>Engine.Step(state)`.
  3. Broadcasts a full `Snapshot` to all clients.
- Clients apply input locally for instant response (**prediction**).
- When a snapshot arrives, clients overwrite local state with host
  truth, then **re-apply** their most recent local intent so user-
  requested turns aren't undone by an in-flight snapshot.

Pros: cheat-resistant (host is authoritative), tolerates packet loss
(snapshots are absolute), works for arbitrary number of players.

Cons: requires bandwidth for snapshots; host migration is hard.

**Files**:
- `Games/Snakes/Scripts/Multiplayer/SnakesMultiplayer.cs`
- `Games/MazePaint/Scripts/Multiplayer/MazePaintMultiplayer.cs`
- `Games/BombSweep/Scripts/Multiplayer/MultiplayerBombSweep.cs`

## (4) Turn-based broadcast

**Used by**: Connect Four, Reversi, Dots and Boxes.

- All peers run the same deterministic game.
- The current player makes one move; broadcasts `Move(...)` with a
  monotonically increasing `MoveNumber`.
- Receivers apply the move to their local copy. Out-of-order or
  duplicate messages are dropped via `MoveNumber`.
- No host authority needed - the game state is fully determined by the
  ordered move history alone.
- Whose turn it is afterward is **engine-determined** (e.g. Dots and
  Boxes lets the same player move again if they completed a box).

Seat assignment: **deterministic by sorting PlayerIds lexicographically**.
No extra handshake required.

Pros: minimal bandwidth (one message per move), no host needed.

Cons: must be reliable (lost move = stuck), doesn't extend to realtime.

**Files**:
- `Games/ConnectFour/Scripts/Multiplayer/MultiplayerConnectFour.cs`
- `Games/Reversi/Scripts/Multiplayer/MultiplayerReversi.cs`
- `Games/DotsAndBoxes/Scripts/Multiplayer/MultiplayerDots.cs`

## (5) Turn-based with hidden state

**Used by**: Battleship.

A variant of (4) where each player keeps a piece of **private** state
that's never broadcast in full. Wire protocol is asymmetric:

- Shooter sends `ShotFired(x, y, moveNumber)` toward the target.
- Target resolves the shot against its OwnFleet (private), replies
  with `ShotResult(x, y, hit | miss | sunk, sunkCells?)`.
- Shooter updates an OpponentTracker built only from received
  ShotResults - never learns opponent positions speculatively.

Each peer keeps two boards:
- **OwnFleet** - my ships, private, never serialized to the wire.
- **OpponentTracker** - built from received ShotResults.

Phase machine: `Setup → Playing → GameOver`. Setup ends when both
sides have broadcast `ShipsReady`.

Pros: enables information-asymmetry games that pattern (4) can't.

Cons: asymmetric round-trip per move costs one extra hop of latency;
phase machine adds complexity.

**Files**:
- `Games/Battleship/Scripts/Multiplayer/MultiplayerBattleship.cs`

## Common infrastructure

All five patterns ride on top of:

- **Wire framing**: `[1 byte MessageType][N bytes MessagePack body]`.
- **Type space**: 0x00-0x7F system messages, 0x80-0xFF reserved for
  game-specific subtypes registered per game.
- **Send channel**: `IGameSendChannel` (`Broadcast` / `SendToHost` /
  `SendTo`), implemented by `RoomSendChannel` over `RoomManager`, or
  by `NullSendChannel` for solo / same-device.
- **Transport**: `IGameTransport` over Google Nearby Connections
  (production) or `MockTransport` (tests / editor smoke).

## When to add a new pattern

If a new game needs **hidden state per player** (Battleship-style: ship
positions are secret), that's a 5th pattern. The decision tree above
assumes all visible state is public to all players. Adding hidden state
means:

- Each player keeps a private piece of state (their grid).
- Public moves (e.g. "shoot at (3, 5)") are broadcast.
- The targeted player responds with `(hit, miss, sunk)` - and that
  response is the only authoritative source of truth on whether the
  shot hit.

We have not implemented this pattern. If/when we add Battleship,
sketch it in this doc.
