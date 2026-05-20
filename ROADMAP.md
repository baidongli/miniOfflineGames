# Roadmap

Where the project stands today (code complete for 10 games + protocol +
shared services) and what's left to ship a playable build.

## Status

**Done (in repo, ~221 tests covering it):**
- 10 game cores (logic + multiplayer orchestrators + AIs).
- All 4 multiplayer sync patterns (see `docs/multiplayer_patterns.md`).
- Hub state machine, navigation, capability-gated transitions.
- 5 shared services (Save, Energy, Audio, Analytics, Haptics).
- Wire protocol with MessagePack framing.
- Mock transport for editor testing.
- Native plugin source for Nearby Connections (Android Kotlin, iOS Swift).
- 5 CI workflows (Unity test/build × 2, native Android/iOS × 2,
  activation × 1).
- Per-game documentation and player-facing GAMEPLAY.md.

**Not done (requires Unity Editor / hardware / artist time):**
- ProjectSettings/ generation + initial commit.
- Unity scene composition (Boot + Hub + per-game scenes).
- Prefab wiring (game cards, lobby, game boards).
- Art assets (icons, tile sprites, color palettes, fonts).
- Audio assets (SFX + BGM).
- Actual build/sign of Android .aar + iOS .framework.
- Real-device Nearby Connections testing.
- Beta distribution (TestFlight + Google Play internal track).

## Phase 0 — Unity opens

The very first time the project opens in Unity 6 LTS:

0. **Pre-flight** (optional, 1 second): `bash scripts/verify_repo.sh`.
   Checks that every game's asmdef + module + tests + registry entries
   line up. If this fails, fix the structural issue before bothering
   Unity with it.
1. Unity Hub → Open Project → pick `unity/`. Unity generates
   `Library/`, `Temp/`, and the rest of `ProjectSettings/`.
2. Check the Console for any compilation errors. Likely candidates:
   - `com.cysharp.messagepack` package name in `Packages/manifest.json`
     may need adjustment - replace with the actual MessagePack-CSharp
     package source if needed (likely from
     `https://github.com/MessagePack-CSharp/MessagePack-CSharp`).
   - Any C# diagnostics from this codebase (we wrote it without ever
     compiling against Unity 6 LTS, so expect 1-3 minor fixes).
3. Window → General → Test Runner → EditMode → Run All. Expect
   ~221 tests to pass. Fix any failures.
4. Commit the newly-generated `ProjectSettings/*.asset` files and
   `Packages/packages-lock.json`.
5. Now CI can build the project headlessly.

## Phase 1 — Boot scene

Goal: app launches, prints "10 games registered" to log, that's it.

1. Create `Assets/Scenes/Boot.unity`.
2. Add empty GameObject `_AppBootstrap`, attach the `AppBootstrap`
   MonoBehaviour. AppBootstrap will create the NearbyConnections
   transport receiver, save store, etc., on Awake.
3. File → Build Settings → add Boot scene as the first scene.
4. File → Build And Run → Android (with a USB-connected phone).
5. Confirm the app launches and the AppBootstrap log line shows.

## Phase 2 — Hub UI

Goal: the player sees a 2×5 grid of game cards.

1. Create `Assets/Prefabs/UI/GameCard.prefab` from `Hub/Scripts/GameCardView.cs`.
   Wire the title TMP_Text, icon Image, button, and the badge GameObjects.
2. Create `Assets/Scenes/Hub.unity` with a Canvas, scroll view, and
   `HubController` on a root GameObject. Drop the GameCard prefab as
   the card prefab reference.
3. Make `IAppView` concrete: implement a Unity-side view that the
   `AppStateMachine` drives (swap scenes / Canvases).
4. Build, confirm 10 cards appear.

## Phase 3 — Mode select & lobby

Goal: tap a card → choose Solo / Same-device / Nearby → land in lobby
or game.

1. Create `GameModeSelect.prefab` (already has a controller script).
2. Create `Lobby.prefab` for Nearby mode (host/join, peer list).
3. Hook the navigation transitions: `AppStateMachine.SelectGame` →
   `StartSolo` / `StartSameDevice` / `HostNearby` / `JoinNearby`.

## Phase 4 — One playable game

Pick the simplest game (suggest **Connect Four**: pure UI, no realtime,
no scene complexity).

1. Build `ConnectFourBoard.prefab`: 7 columns of clickable cells +
   piece-drop animation.
2. Build a `ConnectFourSceneController` MonoBehaviour that:
   - Reads `GameRegistry.FindById("connect_four")`.
   - Calls `module.StartSolo(ctx)` or `StartMultiplayer(ctx, room, seed, isHost)`.
   - On each `MoveApplied`, updates the visual board.
   - On `IsLocalTurn`, listens for cell taps and calls
     `TryLocalPlay(column)`.
3. Test solo. Then same-device 2-player (touch alternates). Then Nearby
   between two devices.

Once Connect Four works end-to-end, **the rest of the games are
basically copy-paste of the scene layer** since their multiplayer
plumbing is already complete and identical-by-pattern.

## Phase 5 — All 10 games visual

Repeat Phase 4 for each game. Rough difficulty order (visual complexity):

1. Connect Four (already done)
2. Reversi - 8x8 grid, similar to Connect Four
3. Dots and Boxes - edges + boxes, similar grid feel
4. 2048 - 4x4 with tile spawn animations
5. Color Blocks - 10x10 with drag-and-drop for piece placement
6. Fruit Merge - 7x12 with chain-merge animations
7. Tetris - falling tetromino animation, rotation feedback
8. Snakes - realtime grid render, smooth interpolation
9. Maze Paint - territory fill rendering
10. Bomb Sweep - the most visually complex (bombs, explosions,
    soft-block destruction, power-up sparkles)

## Phase 6 — Audio & polish

1. Source / commission SFX (drop, clear, win, click, swipe).
2. Source BGM per game (or one ambient track for the Hub).
3. Wire `_ctx.Audio.PlaySfx` calls that game modules already make to
   actual loaded AudioClips in `Resources/Audio/`.
4. Haptics on touch events that need them (place, clear, win).
5. Camera shake on Tetris-clear, Bomb-Sweep explosion.

## Phase 7 — Native plugins

1. `cd native/android && ./gradlew :nearby-bridge:assembleRelease`.
   Drop the resulting `.aar` into `unity/Assets/Plugins/Android/`.
2. `cd native/ios && pod install && xcodegen generate && xcodebuild ...`.
   Drop the `.framework` into `unity/Assets/Plugins/iOS/`.
3. In Unity Player Settings → iOS → add the required Info.plist keys
   (`NSLocalNetworkUsageDescription`, `NSBonjourServices`, etc.).
4. Add a 30-second smoke test: two real devices, open the same game,
   confirm "1 player found" appears in the lobby and they can connect.

## Phase 8 — Beta

1. Set up CI signing: Android upload keystore, iOS provisioning profile.
2. Configure GitHub Actions to upload `.aab` to Google Play internal
   track and `.ipa` to TestFlight on tag pushes.
3. Recruit ~10 friends to play across 3-4 sessions.
4. Capture telemetry through `DebugAnalytics` for now; swap to a real
   provider before public launch.

## Phase 9 — Public launch

1. Replace `DebugAnalytics` with a real provider (GameAnalytics,
   Firebase, Sentry for crashes).
2. Implement IAP `IPaymentProvider` (Unity IAP).
3. Implement Ad provider (AdMob rewarded for "extra energy" or
   "continue" interstitials).
4. Build store listings (10 game screenshots + 30-second trailer).
5. Submit.

## Open questions

- **Host migration**: when the host leaves a Nearby lobby mid-game,
  what should happen? Currently: game ends. Could promote highest-id
  remaining peer; would require deterministic agreement on who that is.
- **Spectator mode**: 5th+ player joining an in-progress game watches.
  Pure UI work; the protocol already supports it (clients receive but
  don't send).
- **Tournament mode**: round-robin or knockout brackets across games.
  Probably a Phase 10 stretch.
- **Voice / chat**: Nearby supports stream payloads. We've reserved
  `MessageType.Chat` (0x0C) but only have emote presets in mind.

## What I'd build next if I were you

After Phase 5 (all 10 games visually playable), I'd ship a **closed
beta of 3 games only** rather than wait for all 10. Connect Four +
Snakes + Bomb Sweep covers the calm/fast/chaotic emotional range. Get
real-world feedback on Nearby reliability and UI clarity before
investing the other 7 games' visual work.
