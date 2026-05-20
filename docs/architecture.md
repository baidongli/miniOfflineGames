# Architecture

## Goals

1. Ship a polished, "not-crude" collection of offline mini-games.
2. Each game playable solo or with 1-4 nearby players (no internet, no
   manual Bluetooth pairing).
3. New games can be added without touching shared code.
4. Keep initial install size lean (< 80 MB); load game assets on demand.

## Module Map

```
┌─────────────────────────────────────────────────────────────────┐
│ App                                                             │
│  - Bootstrap (singleton transport, services, persistent ids)    │
│  - Navigation (AppStateMachine + IAppView)                      │
│  - Hub UI controllers (game grid, lobby, mode select)           │
│  - Shared services (Save / Audio / Energy / Analytics / Haptics)│
│  asmdef: MiniGames.App                                          │
└──────┬──────────────────────────────────────┬───────────────────┘
       │                                      │
       ▼                                      ▼
┌──────────────────────────┐      ┌──────────────────────────────┐
│ Networking               │      │ GameModule                   │
│  - Transport abstraction │      │  - IGameModule interface     │
│  - NearbyConnections     │      │  - GameContext               │
│  - MockTransport (test)  │◀─────│  - IGameSendChannel          │
│  - Room/Session          │      │  - Input abstraction         │
│  - Wire protocol         │      │  - ITicker abstraction       │
│ asmdef: MiniGames.Networking    │ asmdef: MiniGames.GameModule │
└──────────────────────────┘      └──────────────┬───────────────┘
                                                 │ implemented by
                                                 ▼
                            ┌──────────────────────────────────────┐
                            │ Games/ColorBlocks                    │
                            │ Games/Snakes                         │
                            │ Games/FruitMerge                     │
                            │ Games/MazePaint                      │
                            │   each: Logic/ + Multiplayer/ +      │
                            │         AI/ + Module + own asmdef    │
                            └──────────────────────────────────────┘
```

**Dependency rules (enforced by Assembly Definitions):**

- `Networking` knows nothing about specific games or about `App`.
- `GameModule` depends on `Networking` (for `PeerId` / `MessageType`) but
  not on `App` or any specific game.
- Each `Games/X` asmdef depends on `GameModule` + `Networking` only —
  never on `App`, never on another game.
- `App` references all of the above. Game discovery happens through
  `IGameModule` implementations registered in `App/Bootstrap/GameRegistry.cs`.

## App Layer

**Bootstrap** (`App/Bootstrap/`)
- `AppBootstrap` — single MonoBehaviour entry point. Constructs every
  shared service exactly once, exposes them via `AppServices`.
- `GameRegistry` — static list of `IGameModule` implementations shipped
  with the app. Adding a game = appending one line here.
- `UnityTicker` — production `ITicker` for fixed-rate hosting loops.

**Navigation** (`App/Navigation/`)
- `AppState` enum + `PlayMode` enum.
- `IAppView` — screen presentation contract. Concrete UI is a thin
  MonoBehaviour swapping prefabs/canvases.
- `AppStateMachine` — pure-C# screen-flow controller, fully unit-testable
  with a fake `IAppView`. Validates every transition (capability gates,
  state preconditions). Emits `nav_transition` analytics events.

**Hub UI** (`App/Hub/Scripts/`)
- View controllers (`HubController`, `GameCardView`, `GameModeSelectController`,
  `RoomLobbyController`, `PeerRowView`, `EnergyBarView`). Pure C#;
  Inspector wiring of prefab references happens in the Unity Editor.

**Shared services** (`App/Shared/`)
- `JsonSaveStore` — JSON-on-disk via atomic `.tmp` + rename.
- `EnergyTimer` — soft-currency timer, time source injected so tests
  use virtual clock.
- `AudioBus` — three-source pool (BGM crossfade pair + SFX one-shot pool).
- `DebugAnalytics` — log-only stand-in; swap out for a real provider
  without touching call sites.
- `UnityHaptics` / `NullHaptics` — platform-conditional buzz.

## Networking Layer

Three sub-layers, top-down:

1. **Session / Room** (`Networking/Session/`)
   - `RoomManager` — game-agnostic host+peers lifecycle: Hello / Snapshot
     / Ready / Start state machine, broadcasts room snapshots, tracks
     connected players and host peer id.
   - `GameSession` — glue that subscribes to `RoomManager.GameMessageReceived`
     and routes payloads into `IGameModule.OnPeerMessage`.
   - `RoomSendChannel` — `IGameSendChannel` adapter so game modules can
     send game-specific (0x80+) messages without knowing about framing.
   - `NullSendChannel` — no-op for solo/same-device modes.

2. **Protocol** (`Networking/Protocol/`)
   - `MessageType` enum: 0x01-0x7F system messages, 0x80-0xFF reserved
     for game-specific subtypes.
   - POCOs (`Hello`, `RoomSnapshot`, `StartGame`, `Ping`, etc.) with
     `[MessagePackObject(keyAsPropertyName: true)]` for additive evolution.
   - `MessagePackMessageSerializer` — `[1 byte type][body bytes]` framing.

3. **Transport** (`Networking/Transport/`)
   - `IGameTransport` — abstract: advertise/discover, connect/accept,
     send/broadcast, callbacks.
   - `NearbyConnectionsTransport` — Unity-side MonoBehaviour using
     `AndroidJavaObject` (Android) and `[DllImport("__Internal")]` (iOS)
     to talk to the native bridges. Receives callbacks via
     `UnitySendMessage` to a dedicated `_NearbyTransportReceiver` GameObject.
   - `MockTransport` + `MockNetwork` — in-memory test/editor implementation
     pairing multiple peers synchronously.

## Game Module Contract

```csharp
public interface IGameModule
{
    string Id { get; }
    string DisplayName { get; }
    GameCapabilities Capabilities { get; }   // Solo | Multiplayer | SameDevice
    int MinPlayers { get; }                  // >= 2 for any multiplayer mode
    int MaxPlayers { get; }

    Task LoadAsync(GameContext ctx);
    void StartSolo(GameContext ctx);
    void StartMultiplayer(GameContext ctx, RoomSnapshot room, int seed, bool isHost);
    void Pause(); void Resume();
    Task UnloadAsync();

    void OnPeerMessage(PeerId from, MessageType type, ArraySegment<byte> payload);
    void OnPeerJoined(PeerId peer);
    void OnPeerLeft(PeerId peer);
}
```

`GameContext` exposes shared services + `IGameSendChannel`, so game code
never reaches into App globals.

**Per-game folder layout**:
```
Games/<Name>/
├── Scripts/
│   ├── Logic/         Pure-C# game rules, fully unit-testable
│   ├── Multiplayer/   Game-specific wire messages + orchestrator
│   ├── AI/            One-step heuristic opponents (also pure-C#)
│   └── <Name>Module.cs   Implements IGameModule
└── <Name>.asmdef
```

## Multiplayer Protocols

**Snakes / Maze Paint**: host-authoritative snapshot + client prediction.
Host advances simulation at fixed Hz, broadcasts state. Clients apply
local input immediately, then snap to host snapshots and re-apply their
most-recent intent so user-requested turns aren't undone.

**Color Blocks**: event-synced. Each player runs their own game; on a
2+ line clear, broadcast `Attack(junkRows = lines - 1)`. Receivers
push that many junk rows into their own board (one gap per row determined
by a deterministic RNG from the attacker's seed).

**Fruit Merge**: each peer runs an identically-seeded game (identical
`NextFruit` sequence). Drops are broadcast as events so opponents can
render the other side's grid; winning is a score race + last-alive.

All four games use `MessageType.GameSpecificBase + N` (0x80+) for their
private subtypes.

## Tick / Time

`ITicker` (`GameModule/Tick/`) is the abstract fixed-rate driver.
- `UnityTicker` — production, driven by `MonoBehaviour.Update`.
- `VirtualTicker` — test driver, advance manually via `Advance(seconds)`
  or `AdvanceTicks(n)`.

Hosts wire up an `ITicker` per active multiplayer game so `HostTick()`
runs at the game's target Hz (10 for Snakes, 8 for Maze Paint).

## AI Opponents

Each game ships a simple heuristic AI usable for solo "vs CPU" and as
a placeholder in same-device modes when fewer than 4 humans play.

- `GreedyColorBlocksAI` — exhaustive (piece, position) scan; prefers
  line clears, then placement low on the board.
- `SimpleSnakesAI` — one-step lookahead; prefers current heading,
  rotates toward food when safe.
- `SimpleMazePaintAI` — like Snakes' AI but steers back home when
  carrying a trail.
- `GreedyFruitMergeAI` — picks the column whose landing cell has the
  most same-tier neighbors; prefers lower landings to avoid tall stacks.

All AIs are pure-C# with no Unity dependency; tested under
`Tests/EditMode/Games/<Name>/*AITests.cs`.

## Build Pipeline

- One Unity project, two targets (Android, iOS).
- `Addressables` for per-game asset bundles (configured but unused
  while resources fit in the main bundle).
- CI: `.github/workflows/unity-ci.yml` runs EditMode + PlayMode tests
  on every push and builds Android `.aab` + iOS Xcode project on PRs.
  Native bridges have their own workflows (`native-android.yml`,
  `native-ios.yml`).

## Native Bridges

Google Nearby Connections has separate SDKs for Android and iOS. The
bridge layer hides this behind a single `IGameTransport` C# interface.

**Android** (`native/android/nearby-bridge/`)
- Kotlin AAR module via Gradle.
- Strategy: `P2P_CLUSTER` (1-to-many for 1-4 players).
- Callbacks reach Unity via `UnityPlayer.UnitySendMessage`.
- AndroidManifest declares all required runtime permissions
  (BT_ADVERTISE / CONNECT / SCAN on API 31+, NEARBY_WIFI_DEVICES on API 33+).

**iOS** (`native/ios/`)
- Swift framework with `@_cdecl` C ABI, ObjC shim that calls Unity's
  `UnitySendMessage` (resolved at link time when the framework is
  loaded by the host app).
- Generated via `xcodegen` from `project.yml`; pods from `Podfile`.

## Test Coverage Map

| Module | Tests |
|---|---|
| Protocol serialization | 7 |
| Color Blocks (logic + MP + integration + AI) | 30 |
| Tetris (logic + MP + AI) | 15 |
| Snakes (logic + MP + integration + AI) | 20 |
| Maze Paint (logic + MP + integration + AI) | 13 |
| Fruit Merge (logic + MP + integration + AI) | 22 |
| Bomb Sweep (logic + AI) | 8 |
| Connect Four (logic + MP + AI) | 15 |
| Reversi (logic + MP + AI) | 11 |
| 2048 / Number Merge (logic + MP + AI) | 11 |
| Dots and Boxes (logic + MP + AI) | 9 |
| Input dispatcher | 4 |
| MockTransport | 5 |
| RoomManager + game-msg routing + reconnect | 12 |
| App state machine | 8 |
| EnergyTimer | 9 |
| JsonSaveStore | 5 |
| VirtualTicker | 4 |
| IRng | 4 |
| CPU controllers (cross-game smoke) | 5 |
| **Total** | **~221** |

## Open Decisions / Backlog

- [ ] Whether to adopt NGO (Netcode for GameObjects) for the realtime
      games or stay with the custom orchestrator pattern (currently:
      custom is simpler and we control reconciliation).
- [ ] Host migration on disconnect (out of scope for v1).
- [ ] Spectator mode.
- [ ] Voice chat / quick chat extension beyond preset emotes.
- [ ] Crash reporting integration (Sentry vs Firebase Crashlytics).
- [ ] Asset Store UI kit selection before final art pass.
