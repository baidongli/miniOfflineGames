# miniOfflineGames

[![Unity CI](https://github.com/baidongli/miniOfflineGames/actions/workflows/unity-ci.yml/badge.svg)](https://github.com/baidongli/miniOfflineGames/actions/workflows/unity-ci.yml)
[![Android bridge](https://github.com/baidongli/miniOfflineGames/actions/workflows/native-android.yml/badge.svg)](https://github.com/baidongli/miniOfflineGames/actions/workflows/native-android.yml)
[![iOS bridge](https://github.com/baidongli/miniOfflineGames/actions/workflows/native-ios.yml/badge.svg)](https://github.com/baidongli/miniOfflineGames/actions/workflows/native-ios.yml)

A collection of polished offline mini-games for iOS and Android. Every game supports:

- **Single-player** with progression and challenge modes.
- **Local multiplayer (1-4 players)** via Google Nearby Connections — no internet required, no Bluetooth pairing needed. Works on planes, road trips, anywhere.

## Games

| Game | Solo | Same-device | Nearby | Mechanic |
|---|---|---|---|---|
| **Color Blocks** | ✓ | ✓ | ✓ | Drag pieces, clear rows/cols; combos send junk to opponents |
| **Tetris** | ✓ | ✓ | ✓ | Classic falling-tetromino with 7-bag RNG; 2+ line clears send garbage |
| **Snakes** | ✓ | ✓ | ✓ | Realtime grid snake; 10Hz host-authoritative + client prediction |
| **Maze Paint** | ✓ | ✓ | ✓ | Paper.io-style territory grab with flood-fill capture |
| **Fruit Merge** | ✓ |   | ✓ | Grid drop-and-collapse with chain merges |
| **Connect Four** | ✓ | ✓ | ✓ | Turn-based 2-player classic, 4-in-a-row to win |
| **Bomb Sweep** | ✓ | ✓ | ✓ | 4-player realtime arena: drop bombs, dodge blasts, last alive wins |
| **Reversi** | ✓ | ✓ | ✓ | 2-player Othello with positional minimax AI |
| **2048** | ✓ |   | ✓ | Slide-and-merge tile puzzle; race to 2048 |
| **Dots and Boxes** | ✓ | ✓ | ✓ | Turn-based 2-4 players, complete a box = bonus turn |
| **Battleship** | ✓ | ✓ | ✓ | Hidden-fleet 2-player guess-and-sink classic |

## Project status

Pre-alpha. Code complete; awaiting first Unity Editor open to generate
`ProjectSettings/` and wire UI prefabs/scenes.

| Pillar | State |
|---|---|
| Game logic (4 games) | Done |
| Multiplayer orchestrators (4 games) | Done |
| Wire protocol + framing | Done |
| Nearby Connections transport (Android Kotlin + iOS Swift) | Source written; awaiting first .aar / .framework build |
| Shared services (Save, Energy, Audio, Analytics, Haptics) | Done |
| App state machine (Boot → Hub → Lobby → InGame → Results) | Done |
| Hub UI controllers (C# layer) | Done; prefabs pending |
| CI (Unity test + builds, native bridges) | Workflows defined; needs `UNITY_LICENSE` secret |
| Unit & integration tests | 120+ tests |

## Stack

- Unity 6 LTS (6000.0.75f1)
- MessagePack-CSharp for wire serialization
- Google Nearby Connections SDK (Android Play Services / iOS Pod)
- UniTask for async

## Layout

```
miniOfflineGames/
├── unity/                Unity project root (open in Unity Hub)
│   ├── Assets/
│   │   ├── App/          Bootstrap, Hub UI, navigation, shared services
│   │   ├── Networking/   Transport / Protocol / Session
│   │   ├── GameModule/   IGameModule + GameContext + Input
│   │   ├── Games/        One folder + asmdef per game
│   │   ├── Plugins/      Native .aar / .framework drop locations
│   │   └── Tests/EditMode/  NUnit edit-mode test suite
│   └── Packages/         Unity package manifest
├── native/
│   ├── android/          Kotlin Nearby bridge (Gradle)
│   └── ios/              Swift Nearby bridge (xcodegen + CocoaPods)
└── docs/
    ├── architecture.md           Module boundaries
    ├── networking.md             Wire protocol, host authority, reconciliation
    ├── multiplayer_patterns.md   Decision tree for the 4 sync patterns
    ├── first_time_setup.md       One-time Unity bootstrap
    └── games/                    Per-game rules (10 .md files)

Top-level reading order for a new contributor:

1. `README.md` (you are here) — what + status.
2. `GAMEPLAY.md` — what the player experiences.
3. `ROADMAP.md` — what's done, what's next, in what order.
4. `CLAUDE.md` — how to add a new game + asmdef rules.
5. `docs/architecture.md` — module dependencies + service inventory.
6. `docs/multiplayer_patterns.md` — pick the right sync model for new games.
```

## Quick sanity check

Before opening Unity, you can validate the repo's structure:

```bash
bash scripts/verify_repo.sh
```

Runs ~83 static checks (every game has Logic/Multiplayer/AI folders +
asmdef + module, every game is referenced in App.asmdef + Tests.asmdef
+ GameRegistry, no game depends on another game or on App, CI
workflows present, etc.). Exit 0 = good to go.

## First-time setup

See `docs/first_time_setup.md`. Short version:

1. Install Unity 6 LTS (6000.0.75f1) via Hub with Android + iOS modules.
2. Open `unity/` in Unity Hub; Unity generates `ProjectSettings/` + `Library/`.
3. Commit the generated `ProjectSettings/` files.
4. Add CI secrets (`UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`).
5. Build native plugins (see each `native/*/README.md`) and drop into `unity/Assets/Plugins/`.

## Running tests

In Unity Editor: `Window → General → Test Runner → EditMode → Run All`.

On CI: tests run automatically on every push that touches `unity/**`.

## Architecture overview

Strict module boundaries enforced by Unity Assembly Definitions:

```
App ──depends on──▶ Networking, GameModule, Games/*
Games/X ──depends on──▶ Networking, GameModule       (never on App or other games)
Networking ──depends on──▶ —
GameModule ──depends on──▶ Networking (for PeerId / MessageType)
```

Adding a game is a folder + an asmdef + one line in `GameRegistry.cs`.
See `CLAUDE.md` and `docs/architecture.md` for details.
