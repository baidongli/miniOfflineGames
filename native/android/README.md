# Android Nearby Connections Bridge

Kotlin Android library wrapping `com.google.android.gms:play-services-nearby`,
exposing a Unity-friendly API and forwarding callbacks via
`UnityPlayer.UnitySendMessage`.

## Build

```bash
cd native/android
./gradlew :nearby-bridge:assembleRelease
```

Output: `nearby-bridge/build/outputs/aar/nearby-bridge-release.aar`.

Copy the `.aar` to `unity/Assets/Plugins/Android/`. Unity will bundle it.

## Unity's classes.jar

`NearbyBridge.kt` calls `com.unity3d.player.UnityPlayer.UnitySendMessage`. To
compile locally, drop Unity's `classes.jar` (from
`<Unity install>/Editor/Data/PlaybackEngines/AndroidPlayer/Variations/il2cpp/Release/Classes/classes.jar`)
into `nearby-bridge/libs/`. It's declared `compileOnly` so it's not bundled
into the .aar.

CI does this via the GameCI Unity install; see `.github/workflows/native-android.yml`.

## Wire conventions

Unity GameObject receiving callbacks must be named `_NearbyTransportReceiver`.
Callbacks use a `endpointId|payloadString` format (base64 for byte payloads).
See `NearbyConnectionsTransport.cs` on the Unity side.

## Permissions

Declared in `nearby-bridge/src/main/AndroidManifest.xml`. Merged into Unity's
final manifest automatically when the .aar is bundled.

Runtime permission requests for `BLUETOOTH_*` (API 31+) and
`NEARBY_WIFI_DEVICES` (API 33+) must be triggered from Unity-side code before
calling `startAdvertising` / `startDiscovery`. Use a permission-helper plugin
or the Unity 2022 `Permission.RequestUserPermission` API.
