#!/usr/bin/env bash
#
# verify_repo.sh — static sanity check before opening the project in Unity.
#
# Catches the common "I added a game and forgot one of the five places it
# needs to be registered" mistake. Run from the repo root:
#
#     bash scripts/verify_repo.sh
#
# Exit code:
#   0 = all checks passed
#   1 = at least one check failed (details printed)

set -u

# Resolve repo root relative to this script.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT"

# --- output helpers ---

if [ -t 1 ]; then
  C_RED=$'\033[31m'; C_GRN=$'\033[32m'; C_YLW=$'\033[33m'; C_BLU=$'\033[34m'; C_RST=$'\033[0m'
else
  C_RED=; C_GRN=; C_YLW=; C_BLU=; C_RST=
fi

PASS_COUNT=0
FAIL_COUNT=0

pass() { echo "${C_GRN}  ✓${C_RST} $1"; PASS_COUNT=$((PASS_COUNT + 1)); }
fail() { echo "${C_RED}  ✗${C_RST} $1"; FAIL_COUNT=$((FAIL_COUNT + 1)); }
section() { echo; echo "${C_BLU}== $1 ==${C_RST}"; }

# --- discover the game list from the filesystem ---

GAMES_DIR="unity/Assets/Games"
if [ ! -d "$GAMES_DIR" ]; then
  echo "${C_RED}FATAL: $GAMES_DIR does not exist. Are you running from the repo root?${C_RST}"
  exit 1
fi

# Each subdirectory of Games/ should be one game.
GAMES=()
for d in "$GAMES_DIR"/*/; do
  [ -d "$d" ] || continue
  name="$(basename "$d")"
  GAMES+=("$name")
done

if [ ${#GAMES[@]} -eq 0 ]; then
  fail "no games found under $GAMES_DIR"
  exit 1
fi

# --- 1. Each game has the four expected folders + an asmdef + a Module ---

section "Per-game folder layout (${#GAMES[@]} games)"
for g in "${GAMES[@]}"; do
  gdir="$GAMES_DIR/$g"
  ok=true

  if [ ! -f "$gdir/MiniGames.Games.$g.asmdef" ]; then
    fail "$g: missing MiniGames.Games.$g.asmdef"
    ok=false
  fi
  for sub in Scripts Scripts/Logic Scripts/Multiplayer Scripts/AI; do
    if [ ! -d "$gdir/$sub" ]; then
      fail "$g: missing $sub/"
      ok=false
    fi
  done

  # Find the Module.cs (named like <X>Module.cs in Scripts/).
  module_count=$(find "$gdir/Scripts" -maxdepth 1 -name '*Module.cs' 2>/dev/null | wc -l | tr -d ' ')
  if [ "$module_count" = "0" ]; then
    fail "$g: no *Module.cs in Scripts/"
    ok=false
  fi

  $ok && pass "$g: layout OK"
done

# --- 2. Each game is referenced in App.asmdef ---

section "App asmdef references"
APP_ASMDEF="unity/Assets/App/MiniGames.App.asmdef"
if [ ! -f "$APP_ASMDEF" ]; then
  fail "App asmdef missing at $APP_ASMDEF"
else
  for g in "${GAMES[@]}"; do
    if grep -q "\"MiniGames.Games.$g\"" "$APP_ASMDEF"; then
      pass "App references MiniGames.Games.$g"
    else
      fail "App asmdef missing reference to MiniGames.Games.$g"
    fi
  done
fi

# --- 3. Tests asmdef references every game ---

section "Tests asmdef references"
TESTS_ASMDEF="unity/Assets/Tests/EditMode/MiniGames.Tests.EditMode.asmdef"
if [ ! -f "$TESTS_ASMDEF" ]; then
  fail "Tests asmdef missing at $TESTS_ASMDEF"
else
  for g in "${GAMES[@]}"; do
    if grep -q "\"MiniGames.Games.$g\"" "$TESTS_ASMDEF"; then
      pass "Tests references MiniGames.Games.$g"
    else
      fail "Tests asmdef missing reference to MiniGames.Games.$g"
    fi
  done
fi

# --- 4. Each game is registered in GameRegistry.cs ---

section "GameRegistry"
REGISTRY="unity/Assets/App/Bootstrap/GameRegistry.cs"
if [ ! -f "$REGISTRY" ]; then
  fail "GameRegistry missing at $REGISTRY"
else
  for g in "${GAMES[@]}"; do
    if grep -q "new ${g}Module()" "$REGISTRY"; then
      pass "$g: registered as new ${g}Module()"
    else
      fail "$g: not registered in GameRegistry.All"
    fi
  done
fi

# --- 5. No game asmdef references another game (or App) ---

section "Game asmdef isolation"
for g in "${GAMES[@]}"; do
  gdef="$GAMES_DIR/$g/MiniGames.Games.$g.asmdef"
  [ -f "$gdef" ] || continue

  # References to App = violation.
  if grep -q "\"MiniGames.App\"" "$gdef"; then
    fail "$g references MiniGames.App (games must not depend on App)"
  fi
  # References to ANOTHER game = violation.
  for other in "${GAMES[@]}"; do
    [ "$other" = "$g" ] && continue
    if grep -q "\"MiniGames.Games.$other\"" "$gdef"; then
      fail "$g asmdef references another game ($other) — games must not cross-depend"
    fi
  done
  pass "$g: isolation OK"
done

# --- 6. Game module Id should match game folder name (sanity) ---

section "Module Id sanity"
for g in "${GAMES[@]}"; do
  module_file=$(find "$GAMES_DIR/$g/Scripts" -maxdepth 1 -name '*Module.cs' 2>/dev/null | head -1)
  if [ -z "$module_file" ]; then continue; fi
  # Just check that the module file defines an Id property (string-typed).
  if grep -Eq 'public string Id => ".*"' "$module_file"; then
    pass "$g: module has Id property"
  else
    fail "$g: module has no \"public string Id => ...\" line"
  fi
done

# --- 7. Unity version pin and manifest exist ---

section "Top-level Unity files"
if [ -f "unity/ProjectSettings/ProjectVersion.txt" ]; then
  ver=$(grep '^m_EditorVersion:' unity/ProjectSettings/ProjectVersion.txt | sed 's/m_EditorVersion: //' | tr -d '[:space:]')
  pass "ProjectVersion.txt: $ver"
else
  fail "ProjectVersion.txt missing"
fi
if [ -f "unity/Packages/manifest.json" ]; then
  pass "Packages/manifest.json present"
else
  fail "Packages/manifest.json missing"
fi

# --- 8. Every game has at least one *.cs test under Tests/EditMode/Games/<Name>/ ---

section "Tests presence"
for g in "${GAMES[@]}"; do
  tdir="unity/Assets/Tests/EditMode/Games/$g"
  if [ -d "$tdir" ] && [ "$(find "$tdir" -name '*.cs' | wc -l | tr -d ' ')" != "0" ]; then
    pass "$g: has tests under $tdir/"
  else
    fail "$g: no tests under $tdir/"
  fi
done

# --- 9. CI workflows present ---

section "CI workflows"
for wf in unity-ci.yml unity-activation.yml native-android.yml native-ios.yml; do
  if [ -f ".github/workflows/$wf" ]; then pass "$wf present"; else fail "$wf missing"; fi
done

# --- summary ---

echo
echo "${C_BLU}== Summary ==${C_RST}"
echo "  Pass: ${C_GRN}$PASS_COUNT${C_RST}"
echo "  Fail: ${C_RED}$FAIL_COUNT${C_RST}"
echo "  Games discovered: ${#GAMES[@]}"

if [ "$FAIL_COUNT" -eq 0 ]; then
  echo
  echo "${C_GRN}All checks passed.${C_RST} Open the project in Unity."
  exit 0
else
  echo
  echo "${C_RED}$FAIL_COUNT check(s) failed.${C_RST} See above."
  exit 1
fi
