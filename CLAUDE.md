# Developer Guide

A collection of offline mini-games for iOS and Android, supporting solo,
same-device (1-4 player split screen), and Nearby Connections (1-4 player
local wireless) modes. Built in Unity 6 LTS (6000.0.75f1).

## Repository Layout

```
miniOfflineGames/
├── docs/
│   ├── architecture.md         Module map, dependencies, design rules
│   ├── networking.md           Wire protocol, host authority, prediction
│   └── games/                  Per-game design notes
├── unity/                      Unity project root (open in Unity Hub)
│   ├── Assets/
│   │   ├── App/                Bootstrap, Hub UI, shared services
│   │   │   └── Shared/         Audio / Save / Energy / Analytics / Haptics
│   │   ├── Networking/         Transport / Protocol / Session
│   │   ├── GameModule/         IGameModule + GameContext + Input abstraction
│   │   ├── Games/              One folder per game (ColorBlocks, Snakes, ...)
│   │   ├── Plugins/            Native .aar / .framework drop locations
│   │   └── Tests/EditMode/     NUnit edit-mode tests
│   └── Packages/manifest.json  UPM package list
├── native/
│   ├── android/                Kotlin Nearby Connections bridge (Gradle)
│   └── ios/                    Swift Nearby Connections bridge (xcodegen)
└── .github/workflows/          CI (Unity test + build, native plugin builds)
```

## Module Boundaries

```
   App  ─────────────────── depends on every module below
    │
    ▼
   Networking ◀──── GameModule (interface only)
                          ▲
                          │
                Games/{Name}  ──── one asmdef per game,
                                   may only reference Networking + GameModule
```

Rules enforced by Assembly Definitions:

- Games cannot reference each other.
- Games cannot reference App.
- Networking and GameModule never reach down into games or App.
- Adding a game = new folder under `Games/` + new asmdef + register in
  `App/Bootstrap/GameRegistry.cs`.

## Adding a New Game

1. Create `unity/Assets/Games/<Name>/` with a `MiniGames.Games.<Name>.asmdef`
   referencing `MiniGames.GameModule` and `MiniGames.Networking`.
2. Implement `IGameModule` in `<Name>Module.cs`.
3. Put pure game logic under `Scripts/Logic/` so it can be unit tested
   without Unity.
4. For multiplayer, add a `Scripts/Multiplayer/` folder with:
   - Game-specific MessagePack messages typed at `MessageType.GameSpecificBase + N`
   - An orchestrator that talks to the local logic and to `ctx.Net`.
5. Add a line in `GameRegistry.cs`.
6. Add edit-mode tests under `unity/Assets/Tests/EditMode/Games/<Name>/`.

## Running Tests

In Unity Editor: Window → General → Test Runner → EditMode → Run All.

In CI: tests run automatically on every push that touches `unity/**`.
See `.github/workflows/unity-ci.yml`.

Requires repo secrets `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`
to be set (one-time setup; see `unity-activation.yml` workflow).

## Building Native Plugins

**Android** (`native/android/`)
```bash
cd native/android
./gradlew :nearby-bridge:assembleRelease
# Drop the resulting .aar into unity/Assets/Plugins/Android/
```

**iOS** (`native/ios/`)
```bash
cd native/ios
brew install xcodegen && gem install cocoapods
xcodegen generate && pod install
xcodebuild -workspace NearbyBridge.xcworkspace -scheme NearbyBridge \
  -configuration Release -sdk iphoneos build
# Drop the resulting .framework into unity/Assets/Plugins/iOS/
```

Both also build on CI; see `native-android.yml` and `native-ios.yml`.

## Key Design Documents

- `docs/architecture.md` — module dependency rules, build pipeline, open decisions.
- `docs/networking.md` — wire protocol, message types, host authority,
  client prediction strategy, reconciliation budget.
- `docs/games/*.md` — per-game rules, mechanics, multiplayer twist.
