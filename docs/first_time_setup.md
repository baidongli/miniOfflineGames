# First-time Setup

## What's committed

The repo ships with everything **except** the auto-generated Unity
`ProjectSettings/` files (`ProjectSettings.asset`, `TagManager.asset`,
`InputManager.asset`, etc.). Only the version pin (`ProjectVersion.txt`)
is committed so Unity knows which version to open with.

## Why this works this way

Hand-writing the full set of `ProjectSettings/` YAML files risks
breaking subtle binary-serialized fields. Letting Unity Editor generate
them on first open is the safer path; the generated files are then
committed and CI works from then on.

## Steps

1. Install **Unity 2022.3.40f1** via Unity Hub. Include Android Build
   Support + iOS Build Support modules.
2. Open Unity Hub → "Add project from disk" → pick `unity/` in this repo.
3. Unity creates `Library/`, `Temp/`, and the rest of `ProjectSettings/`
   automatically. Wait for the import to complete.
4. In Unity: File → Build Settings. Set your bundle identifier
   (e.g. `com.yourorg.minigames`), product name, and target platforms.
5. Commit the new files under `ProjectSettings/` and `Packages/packages-lock.json`.
6. Add the GitHub Actions secrets (`UNITY_LICENSE`, `UNITY_EMAIL`,
   `UNITY_PASSWORD`) to enable CI builds. See `.github/workflows/unity-activation.yml`
   for how to obtain the license file.

After step 5 is committed, every subsequent CI build and contributor
clone works without manual steps.

## Native plugins

Native bridges (`native/android/`, `native/ios/`) are independent of the
above; they build via Gradle / xcodebuild without needing Unity. See each
folder's README for instructions, and the matching CI workflow.
