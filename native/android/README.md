# Android Nearby Connections Bridge

Kotlin module that wraps `com.google.android.gms:play-services-nearby` and exposes
a flat C-style API to Unity via `UnitySendMessage` callbacks.

## Build

```
./gradlew :nearby-bridge:assembleRelease
```

Output: `nearby-bridge/build/outputs/aar/nearby-bridge-release.aar`

Copy the `.aar` into `unity/Assets/Plugins/Android/` and Unity will bundle it.

## API surface

See `unity/Assets/Networking/Transport/NearbyConnectionsTransport.cs` —
the internal `On*` methods are the exact callback signatures the JNI side
must invoke. Calls into native go through `AndroidJavaObject`.

## Permissions (added by Unity manifest merger)

- `ACCESS_WIFI_STATE`
- `CHANGE_WIFI_STATE`
- `BLUETOOTH` (legacy, API < 31)
- `BLUETOOTH_ADVERTISE`, `BLUETOOTH_CONNECT`, `BLUETOOTH_SCAN` (API 31+)
- `ACCESS_FINE_LOCATION` (required by Nearby on API < 33)
- `NEARBY_WIFI_DEVICES` (API 33+)

## Status

Stub. Module not yet implemented.
