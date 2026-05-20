# miniOfflineGames

A collection of polished offline mini-games for iOS and Android. Every game supports:

- **Single-player** with progression and challenge modes.
- **Local multiplayer (1-4 players)** via Google Nearby Connections — no internet required, no Bluetooth pairing needed. Works on planes, road trips, anywhere.

## Status

Pre-alpha. Project skeleton only.

## Stack

- Unity 2022.3 LTS
- Unity Netcode for GameObjects (NGO) on top of a custom transport
- Transport: Google Nearby Connections
  - Android: official Google Play Services Nearby SDK
  - iOS: official Nearby Connections iOS SDK (CocoaPods)
- Serialization: MessagePack-CSharp

## Layout

```
miniOfflineGames/
├── unity/                Unity project root (open this folder in Unity Hub)
│   ├── Assets/
│   │   ├── App/          Hub UI + shared services (audio, save, ads, IAP, energy)
│   │   ├── Networking/   Transport abstraction, session/room, wire protocol
│   │   ├── Games/        One folder per game, isolated via Assembly Definitions
│   │   └── Plugins/      Native bridges (Android .aar, iOS .framework)
│   └── Packages/         Unity package manifest
├── native/
│   ├── android/          Kotlin Nearby Connections bridge (built as .aar)
│   └── ios/              Swift Nearby Connections bridge (built as .framework)
└── docs/
    ├── architecture.md   Module boundaries, ownership, build pipeline
    └── networking.md     Wire protocol, host authority, reconnection
```

## First-time setup

1. Install Unity 2022.3 LTS via Unity Hub. Include Android + iOS Build Support modules.
2. Open `unity/` in Unity Hub. Unity will generate `Library/`, `Temp/`, etc. on first open (all gitignored).
3. Build the native bridges (`native/android/README.md`, `native/ios/README.md`) and drop the outputs into `unity/Assets/Plugins/`.

## Roadmap

1. **Phase 1** — Shell: Hub + shared services + transport abstraction + smoke-test demo
2. **Phase 2** — Color Blocks (event-synced, validates room flow)
3. **Phase 3** — Snakes (realtime, validates prediction + reconciliation)
4. **Phase 4** — Maze Paint, Fruit Merge, then more

See `docs/architecture.md` for details.
