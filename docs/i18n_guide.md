# i18n Guide

How to add a new string, add a new language, or change an existing
translation.

## Where strings live

```
unity/Assets/App/Shared/Localization/
├── ILocalizationProvider.cs       Contract (Get + Language)
├── LocalizationService.cs         Active table + fallback + persist
└── Tables/
    ├── EnTable.cs                 Canonical English source
    └── ZhTable.cs                 Simplified Chinese
```

UI code calls `L10n.T(key, args...)` (the static shortcut after
`AppBootstrap` sets `L10n.Active`) or pulls the active service from
`AppBootstrap.Services.Localization`.

## Key naming convention

Keys are **stable, dotted, lowercase paths**. Established prefixes:

| Prefix | Meaning | Example |
|---|---|---|
| `game.<id>.title` | Game name | `game.color_blocks.title` |
| `game.<id>.tagline` | One-line hub-card description | `game.color_blocks.tagline` |
| `game.<id>.howto` | Full rules text (reserved) | `game.color_blocks.howto` |
| `ui.*` | Generic UI verbs | `ui.play`, `ui.cancel` |
| `mode.*` | Play modes | `mode.solo`, `mode.nearby_host` |
| `lobby.*` | Lobby strings | `lobby.searching` |
| `result.*` | Result-screen strings | `result.you_win` |
| `settings.*` | Settings labels | `settings.audio` |
| `energy.*` | Energy-related toasts | `energy.refills_in` |
| `error.*` | Error / permission strings | `error.network_off` |
| `ach.<game>.<id>.title` | Achievement title | `ach.tetris.first_tetris.title` |

**Rules**:
- Once an English key ships, **don't rename it** — older device installs
  may have persisted choices keyed by old names.
- Use `{0}`, `{1}` placeholders for runtime values
  (e.g. `"Score: {0}"`). `L10n.T("result.final_score", 42)` returns
  `"Score: 42"`.
- Prefer **two short labels** over one long sentence — translators have
  more flexibility, and shorter strings look better in tight UI.

## Adding a new string

1. Open `EnTable.cs`. Add the new entry in the appropriate section.
2. Open `ZhTable.cs`. Add the **same key** with the Chinese translation.
3. Run `bash scripts/verify_repo.sh` (it doesn't check this, but Unity
   will once you open it).
4. Run the EditMode tests. The `Both_tables_have_the_same_keys` test
   will catch mismatches.
5. Use the new key in UI code: `L10n.T("your.new.key")`.

If a key is missing from `ZhTable`, the service falls back to the
English value automatically — so the app stays usable even with
partial translations. Untranslated keys are visible as the key
verbatim (e.g. `your.new.key`), so missing translations are
**obvious during development**.

## Adding a new language

To add e.g. French (`fr`):

1. Create `Tables/FrTable.cs` mirroring `EnTable.cs` exactly.
2. In `LocalizationService.cs`, add the new table to `_tables`:

   ```csharp
   _tables = new Dictionary<string, Dictionary<string, string>>
   {
       { "en", EnTable.Build() },
       { "zh", ZhTable.Build() },
       { "fr", FrTable.Build() },   // <-- new
   };
   ```

3. Update the language picker UI to list the new option.
4. Add a test (mirroring `Both_tables_have_the_same_keys`) that the
   new table covers all canonical keys.

## Hooking strings to dynamic content

For server-side / dynamic content, use parameterized strings:

```csharp
// Show "Best: 1500" or "最佳：1500"
label.text = L10n.T("result.best_score", playerBest);

// Show "2 players, 1 ready"
label.text = L10n.T("lobby.player_count", players, ready);
```

For pluralization (English "1 player" vs "2 players"), we don't yet
have plural support. Workaround: pre-pluralize on the C# side and use
two keys (`lobby.one_player` vs `lobby.many_players`). If/when the
library grows enough plural rules, swap in a real ICU MessageFormat
library.

## What NOT to localize

- **Game ids** (`color_blocks`, `tetris`, etc.) — these are stable
  string identifiers, not human strings. Never translate them.
- **Internal achievement ids** (`first_tetris`, `survivor`) — same.
- **Network message field names** — these are part of the wire
  protocol and must be stable.
- **Log messages** in `Debug.Log` calls — keep them in English so
  bug reports are intelligible regardless of locale.

## How translations get changed by users

`SettingsService.PreferredLanguage` is the source of truth. The
language picker UI calls
`AppBootstrap.Services.Localization.SetLanguage("zh")`, which:

1. Updates `LocalizationService.Language`.
2. Persists the choice into `SettingsService`.
3. Fires `LanguageChanged` so the UI can re-render.

UI code that needs to refresh on language change should subscribe to
that event.
