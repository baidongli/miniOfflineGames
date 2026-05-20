# Architecture

## Goals

1. Ship a polished, "not-crude" collection of offline mini-games.
2. Each game playable solo or with 1-4 nearby players (no internet, no manual Bluetooth pairing).
3. New games can be added without touching shared code.
4. Keep initial install size lean (< 80 MB); load game assets on demand.

## Module Map

```
┌─────────────────────────────────────────────────────────────┐
│ App (Hub, menus, energy, IAP, ads, shared services)         │
│   asmdef: App                                               │
└──────────────┬──────────────────────────────┬───────────────┘
               │ depends on                   │ depends on
               ▼                              ▼
┌──────────────────────────┐      ┌───────────────────────────┐
│ Networking               │      │ GameModule (interface)    │
│  - Transport abstraction │      │  - IGameModule            │
│  - Nearby impl           │◀─────│  - GameContext            │
│  - Room/Session          │      │  - PlayerInfo             │
│  - Protocol/Messages     │      │ asmdef: GameModule        │
│ asmdef: Networking       │      └─────────────┬─────────────┘
└──────────────────────────┘                    │ implemented by
                                                ▼
                              ┌──────────────────────────────────┐
                              │ Games/ColorBlocks                │
                              │ Games/Snakes                     │
                              │ Games/FruitMerge                 │
                              │ Games/MazePaint                  │
                              │ (each its own asmdef)            │
                              └──────────────────────────────────┘
```

**Dependency rules**

- `Networking` and `GameModule` know nothing about specific games or about `App`.
- Each `Games/X` asmdef depends on `GameModule` and `Networking` only — never on `App`, never on another game.
- `App` discovers games through `IGameModule` implementations registered at startup. Adding a game = adding a folder under `Games/` plus a registration entry.

## Shared Services (under App/)

- `SaveSystem` — JSON to `Application.persistentDataPath`, schema-versioned.
- `Audio` — three buses: BGM, SFX, UI. Independent volume + mute.
- `Ads` — single interface, AdMob impl. Reward, interstitial, banner.
- `IAP` — single interface, Unity IAP impl. Remove-ads, energy packs.
- `Energy` — soft-currency timer system. Refills offline.
- `Analytics` — single interface, swap-in implementation.

## Networking Layer

Three sub-layers, top-down:

1. **Session / Room** (`Networking/Session/`) — Logical concept of a host + N peers, ready states, game selection. Game-agnostic.
2. **Protocol** (`Networking/Protocol/`) — Typed messages, MessagePack serialization, framing.
3. **Transport** (`Networking/Transport/`) — `IGameTransport` interface + `NearbyConnectionsTransport` implementation that calls into native bridges.

Game code talks to Session, not Transport. Transport is swappable (could add LAN, WebSocket relay, etc. later).

See `networking.md` for the wire protocol.

## Native Bridges

Google Nearby Connections has separate SDKs for Android and iOS, both official. Our bridge layer hides those differences behind a single `IGameTransport` C# interface.

**Android** (`native/android/`)
- Kotlin module built as `.aar`.
- Uses `com.google.android.gms:play-services-nearby`.
- Strategy: `P2P_CLUSTER` (1-to-many connections, what we want for 1-4 players).
- Calls into Unity via `UnitySendMessage` or `AndroidJavaProxy` callback interface.

**iOS** (`native/ios/`)
- Swift framework wrapping Google's `NearbyConnections` CocoaPod.
- Same strategy, same callback signatures.
- Bridged to Unity via C# `[DllImport("__Internal")]`.

Both expose:

```
startAdvertising(serviceId, endpointName)
startDiscovery(serviceId)
requestConnection(endpointId)
acceptConnection(endpointId)
rejectConnection(endpointId)
disconnect(endpointId)
sendBytes(endpointId, payload, reliable)

// Callbacks
onEndpointFound(endpointId, name)
onEndpointLost(endpointId)
onConnectionInitiated(endpointId, name, authToken)
onConnectionResult(endpointId, status)
onDisconnected(endpointId)
onPayloadReceived(endpointId, bytes)
```

## Game Module Contract

Every game implements `IGameModule`:

```csharp
public interface IGameModule
{
    string Id { get; }                 // stable id, e.g. "color_blocks"
    string DisplayName { get; }
    GameCapabilities Capabilities { get; }   // soloOnly | multiplayer | both
    int MinPlayers { get; }
    int MaxPlayers { get; }

    // Lifecycle
    UniTask LoadAsync(GameContext ctx);
    void StartSolo(GameContext ctx);
    void StartMultiplayer(GameContext ctx, RoomSnapshot room);
    void Pause();
    void Resume();
    UniTask UnloadAsync();

    // Multiplayer hooks (no-op for solo-only games)
    void OnPeerMessage(PeerId from, ReadOnlySpan<byte> payload);
    void OnPeerJoined(PeerId peer);
    void OnPeerLeft(PeerId peer);
}
```

`GameContext` exposes the shared services (audio, save, analytics, send-message function) so the game code never reaches into `App` directly.

## Build Pipeline

- One Unity project, two build targets (Android, iOS).
- `Addressables` for per-game asset bundles; game scripts stay in main binary for simplicity (revisit if app gets large).
- CI: GitHub Actions builds Android `.aab` and iOS archive on tag pushes. Native bridges built separately and committed under `unity/Assets/Plugins/` (binary, but small enough).

## Open Decisions

- [ ] NGO vs. lightweight custom RPC layer — start with custom, add NGO if needed.
- [ ] MessagePack vs. FlatBuffers — MessagePack first (simpler).
- [ ] Asset Store UI kit — pick before Hub UI work begins.
- [ ] Crash reporting — Sentry vs. Firebase Crashlytics.
