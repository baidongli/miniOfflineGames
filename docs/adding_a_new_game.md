# Adding a New Game

Step-by-step recipe for adding game #N+1 to the collection. Use this in
combination with `multiplayer_patterns.md` to pick your sync model.

## 0. Decide first

Three decisions before you write any code:

1. **Genre** — what kind of game? Look at `GAMEPLAY.md` to avoid overlap
   with the existing 11 games. A new game should fill a distinct slot.

2. **Player count** — 2 only? 1-4? Solo possible?
   - 2-player only: e.g. Connect Four, Reversi, Battleship.
   - 1-4 with same-device option: most realtime games.
   - 1-4 with same-seed score race: puzzle games.

3. **Sync pattern** — see `docs/multiplayer_patterns.md`:
   - (1) Event-synced attacks
   - (2) Pure score race
   - (3) Host tick + prediction
   - (4) Turn-based broadcast
   - (5) Turn-based with hidden state

Write these down. The rest of this doc assumes you've picked.

## 1. Create the folder structure

```bash
mkdir -p unity/Assets/Games/<Name>/Scripts/{Logic,Multiplayer,AI}
mkdir -p unity/Assets/Tests/EditMode/Games/<Name>
```

Conventionally `<Name>` is **PascalCase** (e.g. `ConnectFour`,
`BombSweep`). The `Logic/` subfolder holds pure-C# rules that are
fully unit-testable without Unity.

## 2. asmdef

Create `unity/Assets/Games/<Name>/MiniGames.Games.<Name>.asmdef`:

```json
{
    "name": "MiniGames.Games.<Name>",
    "rootNamespace": "MiniGames.Games.<Name>",
    "references": [
        "MiniGames.GameModule",
        "MiniGames.Networking"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

**Rules**:
- Games NEVER reference `MiniGames.App` (App depends on games, not the
  other way).
- Games NEVER reference each other.
- Always reference `MiniGames.GameModule` (for `IGameModule`, `PeerId`,
  `MessageType`) and `MiniGames.Networking` (for protocol types).

## 3. Pure-logic core (`Scripts/Logic/`)

This is your game's mathematics. **No Unity references** (no
`UnityEngine.*`, no `MonoBehaviour`, no `Debug.Log`). Pure C# only -
that's why we can unit-test it without an Editor.

Minimum surface:

- A board / state type (e.g. `MyBoard`).
- A game session type (e.g. `MyGame`) with:
  - Mutable state mirroring the board.
  - Events fired on state changes (`Moved`, `GameOver`, etc.).
  - `bool TryPlay(...args)` or similar - validates input + applies +
    returns success.
  - `bool IsGameOver` and a result type.
- An engine helper if logic is procedural (e.g. `MyEngine.Step(state)`
  for tick-based games).
- An RNG hook: if your game uses randomness (food spawn, tile spawn,
  piece bag), thread a `System.Random` seeded at construction. Don't
  use `UnityEngine.Random` here.

Look at `Games/ConnectFour/Scripts/Logic/` for a small clean example.

## 4. Tests (`Tests/EditMode/Games/<Name>/`)

Write tests against the pure logic. Minimum coverage:

- Happy path: a legal action mutates state as expected.
- Boundaries: out-of-bounds, full board, etc. return false / errors.
- Win condition: build a board where the next move wins; assert result.
- Loss / draw conditions if applicable.
- Determinism: same seed produces the same RNG-dependent sequence.

You'll likely have 5-15 tests for the logic alone. See
`Tests/EditMode/Games/ConnectFour/ConnectFourLogicTests.cs`.

## 5. Multiplayer (`Scripts/Multiplayer/`)

Pick the matching pattern from `docs/multiplayer_patterns.md`. Each
pattern has 1-3 reference implementations.

For all patterns:

- Wire messages live in `<Name>Messages.cs`. Each message class has
  `[MessagePackObject(keyAsPropertyName: true)]`.
- An enum `<Name>MessageType` lists message bytes starting at
  `MessageType.GameSpecificBase` (0x80).
- The orchestrator class (`Multiplayer<Name>.cs`) has:
  - Constructor takes the local + remote player ids (or full id list).
  - `<event> ...Outgoing` for each outbound message type.
  - `void On<X>Received(<X>Message m)` for each inbound message.
  - Public methods players call (`TryLocalPlay`, `TryShoot`, etc).
  - Internal state mirror updated from both local actions and remote
    messages.
- For deterministic seat assignment in turn-based games, sort the
  player ids alphabetically:

```csharp
bool localIsFirst = string.CompareOrdinal(localPlayerId, remotePlayerId) < 0;
LocalSeat = localIsFirst ? (byte)0 : (byte)1;
```

## 6. AI (`Scripts/AI/`)

Each game should ship at least a simple AI so solo mode is playable.
The AI is pure C# - call site decides when to invoke it.

Common interface shape:

```csharp
public interface IMyGameAI
{
    MoveType Choose(MyGame game);
}
```

Plus a `Cpu<Game>Controller` wrapper that drives the engine:

```csharp
public sealed class CpuMyGameController
{
    public readonly MyGame Game;
    public readonly IMyGameAI Ai;
    public bool TakeTurn();   // returns false on game-over
}
```

Difficulty range:
- **Minimal**: random valid move + simple heuristic (Snakes AI: safe-step preference).
- **Greedy**: 1-ply lookahead (NumberMerge AI: try every direction, score the result).
- **Minimax**: alpha-beta with positional heuristic (Connect Four, Reversi).
- **Specialty**: domain-aware planning (Battleship: hunt + target mode).

## 7. Module (`Scripts/<Name>Module.cs`)

The bridge between Unity and your pure-C# game. Implement `IGameModule`:

```csharp
public sealed class MyGameModule : IGameModule
{
    public string Id => "my_game";              // stable; used in messages
    public string DisplayName => "My Game";     // shown in UI
    public GameCapabilities Capabilities =>
        GameCapabilities.Solo | GameCapabilities.Multiplayer | GameCapabilities.SameDevice;
    public int MinPlayers => 2;
    public int MaxPlayers => 4;

    public Task LoadAsync(GameContext ctx) => Task.CompletedTask;
    public void StartSolo(GameContext ctx) { /* construct local solo game */ }
    public void StartMultiplayer(GameContext ctx, RoomSnapshot room, int seed, bool isHost)
    {
        /* construct MultiplayerXxx orchestrator, wire outgoing events to ctx.Net.Broadcast */
    }
    public void Pause() { }
    public void Resume() { }
    public Task UnloadAsync() => Task.CompletedTask;

    public void OnPeerMessage(PeerId from, MessageType type, ArraySegment<byte> payload)
    {
        /* deserialize via MessagePackSerializer + dispatch to orchestrator */
    }

    public void OnPeerJoined(PeerId peer) { }
    public void OnPeerLeft(PeerId peer) { }
}
```

The module is where Unity-specific code is allowed (`UnityEngine.Random`,
`Debug.Log`, etc.) but keep it minimal - all real logic should be in
`Logic/`.

## 8. Register in `GameRegistry`

Open `unity/Assets/App/Bootstrap/GameRegistry.cs` and add your module
to both the `using` block and the `All` array:

```csharp
using MiniGames.Games.MyGame;
// ...
public static IReadOnlyList<IGameModule> All { get; } = new IGameModule[]
{
    new ColorBlocksModule(),
    // ... existing ...
    new MyGameModule()
};
```

Then update `MiniGames.App.asmdef` to reference
`MiniGames.Games.MyGame`, and likewise update
`Tests/EditMode/MiniGames.Tests.EditMode.asmdef`.

## 9. Documentation

Add `docs/games/<your_game>.md` following the structure of
`docs/games/connect_four.md`:

- Rules
- Controls
- Multiplayer mechanic (cite the sync pattern by number)
- AI summary
- Files-of-interest

Update `README.md`'s games table with your game. Update `GAMEPLAY.md`
with a player-facing section.

## 10. Optional: integration test

For non-trivial multiplayer, write a full-stack test under
`Tests/EditMode/Networking/<Name>MultiplayerIntegrationTests.cs`. Use
`MockNetwork` + `MockTransport` to wire two peers together and verify
the protocol round-trips. See
`Tests/EditMode/Networking/SnakesMultiplayerIntegrationTests.cs` for
the canonical example.

## 11. Smoke check before committing

- Tests pass locally (Window → General → Test Runner → EditMode → Run All).
- New asmdef references compile without "missing assembly" errors.
- GameRegistry loads without exception.
- `git diff` shows your changes are scoped to your game's folder +
  the registry/asmdef updates +/- docs.

## Common gotchas

- **Don't put MonoBehaviours in `Logic/`**. They'll prevent
  edit-mode testing.
- **Don't use `UnityEngine.Random` in `Logic/`**. Thread a seeded
  `System.Random` instead so multiplayer parity works.
- **Don't reference other games' asmdefs**. If two games share a type
  (e.g. `GridPos`), each defines its own copy in its own namespace.
- **Don't forget the asmdef update**. The most common "my game won't
  appear" cause is forgetting to add the reference in
  `MiniGames.App.asmdef`.
- **MessagePack attributes are critical**. Wire types without
  `[MessagePackObject(keyAsPropertyName: true)]` will silently fail to
  serialize.
- **Stable Id**. Once a game's `Id` ships, don't change it - saved
  scores / leaderboards reference it.

## Reference for each pattern

| Sync pattern | Smallest example to copy from |
|---|---|
| (1) Event-synced attacks | `Games/ColorBlocks/Scripts/Multiplayer/` |
| (2) Pure score race | `Games/NumberMerge/Scripts/Multiplayer/` |
| (3) Host tick + prediction | `Games/Snakes/Scripts/Multiplayer/` |
| (4) Turn-based broadcast | `Games/ConnectFour/Scripts/Multiplayer/` |
| (5) Hidden state | `Games/Battleship/Scripts/Multiplayer/` |
