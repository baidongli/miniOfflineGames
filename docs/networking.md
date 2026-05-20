# Networking

## Topology

Host-authoritative star. One device is the **host**, all others are **clients**. Hosts run the game simulation; clients send inputs and render snapshots.

```
       ┌─────────┐
       │ Host    │  (runs game logic, owns truth)
       └────┬────┘
            │  Nearby Connections P2P_CLUSTER
   ┌────────┼────────┐
   ▼        ▼        ▼
┌────┐  ┌────┐  ┌────┐
│ C1 │  │ C2 │  │ C3 │  (send inputs, render snapshots)
└────┘  └────┘  └────┘
```

No dedicated server, no relay. Host migration is out of scope for v1: if the host leaves, the room ends.

## Discovery & Connection

Nearby Connections strategy: `P2P_CLUSTER`. Service ID: `app.minigames.<env>` (prod / staging).

1. Host calls `startAdvertising(serviceId, displayName)`.
2. Clients call `startDiscovery(serviceId)` — they get `onEndpointFound` events for each visible host.
3. Client picks a host → `requestConnection(endpointId)`.
4. Both sides receive `onConnectionInitiated`. Auto-accept (no PIN UI for v1; show host name and let user cancel).
5. On `onConnectionResult(OK)`, the client sends a `Hello` message; host replies with current `RoomSnapshot`.

## Wire Format

Every payload:

```
+--------+-----------------------------+
| 1 byte | N bytes                     |
| type   | MessagePack-encoded body    |
+--------+-----------------------------+
```

`type` is from `MessageType` enum (see `Networking/Protocol/MessageType.cs`). Bodies are versioned by adding new optional fields; never reorder or repurpose existing fields.

Two channels:
- **Reliable** — used for room state, game start, scoring, anything that must arrive.
- **Unreliable** — realtime input + state snapshots (Snakes). Nearby supports both via `Payload.fromBytes` (reliable by default); unreliable is approximated via stream payloads with our own per-tick framing.

## Message Catalog

| Type | Direction | Reliable | Purpose |
|---|---|---|---|
| `0x01 Hello` | C → H | ✓ | Client introduces itself (name, version, platform) |
| `0x02 RoomSnapshot` | H → C | ✓ | Full room state: players, ready flags, game id |
| `0x03 PlayerReady` | C → H | ✓ | Toggle ready state |
| `0x04 SelectGame` | H → C | ✓ | Host changes selected game |
| `0x05 StartGame` | H → C | ✓ | Countdown then begin |
| `0x06 InputCommand` | C → H | ✗ | Per-tick input (button, axis, action) |
| `0x07 StateSnapshot` | H → C | ✗ | Per-tick simulation state |
| `0x08 GameEvent` | H → C | ✓ | Discrete event (score, eliminate, levelup) |
| `0x09 EndGame` | H → C | ✓ | Results + return to lobby |
| `0x0A Ping` | both | ✗ | RTT measurement |
| `0x0B Pong` | both | ✗ | Reply to Ping |
| `0x0C Chat` | both | ✓ | Quick emote / preset chat |

## Realtime Sync (Snakes-class games)

- Host tick rate: 20 Hz.
- Clients send `InputCommand` at the same rate, tagged with their local frame.
- Clients run **client-side prediction**: apply own input immediately, store recent inputs.
- When `StateSnapshot` arrives, compare host position for self against predicted; if mismatch > threshold, snap and replay queued inputs.
- Other players are **interpolated** between the last two received snapshots (100ms buffer).

## Reconciliation Budget

- Target end-to-end input latency on same room: < 80ms.
- Acceptable jitter: 30ms peak-to-peak.
- Snapshot bandwidth budget per client: < 8 KB/s.

## Disconnection

- Nearby fires `onDisconnected` after ~5s of silence.
- Host marks peer as `Disconnected` but holds their slot for 10s.
- If peer reconnects within 10s with same `playerId`, restore them mid-game.
- After 10s, the slot frees and game continues (AI takeover for v2, just remove for v1).

## Security & Trust

- v1 assumes everyone in the room is friendly. No anti-cheat, no encryption beyond what Nearby provides natively (BLE/WiFi pairing is authenticated by the OS).
- Host validates clients aren't sending impossible inputs (basic sanity checks, not full anti-cheat).
- App version is included in `Hello`; mismatched majors refuse to connect.

## Protocol Versioning

- Major version bump = incompatible. Refuse connections.
- Minor version bump = additive. Old peers ignore unknown fields.
- Message types `0x80+` reserved for game-specific subtypes (game registers its handler).

## Out of Scope for v1

- Host migration
- Spectators
- Cross-room chat
- Voice
- Persistent ranked stats from local games
