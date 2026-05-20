using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MiniGames.Networking.Transport
{
    /// <summary>
    /// Unity-side facade over the native Nearby Connections bridges.
    ///   Android: AndroidJavaObject -> nearby-bridge.aar (Kotlin)
    ///   iOS:     DllImport("__Internal") -> NearbyBridge.framework (Swift)
    ///   Editor:  no-op + logs (for compile-time validation)
    ///
    /// Native callbacks arrive on this MonoBehaviour via UnitySendMessage,
    /// so the instance must live on a GameObject named "_NearbyTransportReceiver".
    /// Bootstrap creates that singleton at startup.
    /// </summary>
    public sealed class NearbyConnectionsTransport : MonoBehaviour, IGameTransport
    {
        public const string ReceiverGameObjectName = "_NearbyTransportReceiver";

        public TransportRole Role { get; private set; } = TransportRole.None;

        public event Action<PeerId, string> EndpointFound;
        public event Action<PeerId> EndpointLost;
        public event Action<PeerId, string> ConnectionInitiated;
        public event Action<PeerId, ConnectionStatus> ConnectionResult;
        public event Action<PeerId> Disconnected;
        public event Action<PeerId, ArraySegment<byte>> PayloadReceived;

        private readonly HashSet<PeerId> _connected = new HashSet<PeerId>();

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _bridge;
#endif

        // --- factory ---

        public static NearbyConnectionsTransport CreateAndAttach()
        {
            var existing = GameObject.Find(ReceiverGameObjectName);
            if (existing != null)
            {
                var t = existing.GetComponent<NearbyConnectionsTransport>();
                if (t != null) return t;
            }
            var go = new GameObject(ReceiverGameObjectName);
            DontDestroyOnLoad(go);
            return go.AddComponent<NearbyConnectionsTransport>();
        }

        private void Awake()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                _bridge = new AndroidJavaObject("com.minigames.nearby.NearbyBridge", activity);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nearby] failed to instantiate Android bridge: {e}");
            }
#endif
        }

        // --- IGameTransport ---

        public void StartAdvertising(string serviceId, string displayName)
        {
            Role = TransportRole.Host;
            Debug.Log($"[Nearby] startAdvertising serviceId={serviceId} name={displayName}");
#if UNITY_ANDROID && !UNITY_EDITOR
            _bridge?.Call("startAdvertising", serviceId, displayName);
#elif UNITY_IOS && !UNITY_EDITOR
            NearbyBridge_StartAdvertising(serviceId, displayName);
#endif
        }

        public void StartDiscovery(string serviceId)
        {
            Role = TransportRole.Client;
            Debug.Log($"[Nearby] startDiscovery serviceId={serviceId}");
#if UNITY_ANDROID && !UNITY_EDITOR
            _bridge?.Call("startDiscovery", serviceId);
#elif UNITY_IOS && !UNITY_EDITOR
            NearbyBridge_StartDiscovery(serviceId);
#endif
        }

        public void Stop()
        {
            Debug.Log("[Nearby] stop");
            Role = TransportRole.None;
            _connected.Clear();
#if UNITY_ANDROID && !UNITY_EDITOR
            _bridge?.Call("stopAll");
#elif UNITY_IOS && !UNITY_EDITOR
            NearbyBridge_Stop();
#endif
        }

        public void RequestConnection(PeerId peer)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _bridge?.Call("requestConnection", peer.Value);
#elif UNITY_IOS && !UNITY_EDITOR
            NearbyBridge_RequestConnection(peer.Value);
#endif
        }

        public void AcceptConnection(PeerId peer)
        {
            _connected.Add(peer);
#if UNITY_ANDROID && !UNITY_EDITOR
            _bridge?.Call("acceptConnection", peer.Value);
#elif UNITY_IOS && !UNITY_EDITOR
            NearbyBridge_AcceptConnection(peer.Value);
#endif
        }

        public void RejectConnection(PeerId peer)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _bridge?.Call("rejectConnection", peer.Value);
#elif UNITY_IOS && !UNITY_EDITOR
            NearbyBridge_RejectConnection(peer.Value);
#endif
        }

        public void Disconnect(PeerId peer)
        {
            _connected.Remove(peer);
#if UNITY_ANDROID && !UNITY_EDITOR
            _bridge?.Call("disconnect", peer.Value);
#elif UNITY_IOS && !UNITY_EDITOR
            NearbyBridge_Disconnect(peer.Value);
#endif
        }

        public void Send(PeerId peer, ArraySegment<byte> payload, bool reliable)
        {
            // Always sending the underlying array trimmed to length; callers
            // shouldn't rely on offset semantics across the boundary.
            var bytes = TrimToArray(payload);
#if UNITY_ANDROID && !UNITY_EDITOR
            _bridge?.Call("sendBytes", peer.Value, bytes, reliable);
#elif UNITY_IOS && !UNITY_EDITOR
            NearbyBridge_SendBytes(peer.Value, bytes, bytes.Length, reliable ? 1 : 0);
#endif
        }

        public void Broadcast(ArraySegment<byte> payload, bool reliable)
        {
            foreach (var p in _connected)
                Send(p, payload, reliable);
        }

        private static byte[] TrimToArray(ArraySegment<byte> seg)
        {
            if (seg.Offset == 0 && seg.Count == seg.Array.Length) return seg.Array;
            var copy = new byte[seg.Count];
            Buffer.BlockCopy(seg.Array, seg.Offset, copy, 0, seg.Count);
            return copy;
        }

        // --- Native -> Unity callbacks (invoked by UnitySendMessage) ---
        // Payload arrives as a string-encoded blob: "endpointId|base64bytes".
        // Discrete events come as "endpointId|name" or "endpointId|statusCode".
        // Keep method names stable; the native side hardcodes them.

        public void OnEndpointFound(string msg)
        {
            if (TryParse2(msg, out var id, out var name))
                EndpointFound?.Invoke(new PeerId(id), name);
        }

        public void OnEndpointLost(string endpointId)
        {
            EndpointLost?.Invoke(new PeerId(endpointId));
        }

        public void OnConnectionInitiated(string msg)
        {
            if (TryParse2(msg, out var id, out var name))
                ConnectionInitiated?.Invoke(new PeerId(id), name);
        }

        public void OnConnectionResult(string msg)
        {
            if (TryParse2(msg, out var id, out var statusStr) &&
                int.TryParse(statusStr, out var code))
            {
                ConnectionResult?.Invoke(new PeerId(id), (ConnectionStatus)code);
            }
        }

        public void OnDisconnected(string endpointId)
        {
            var id = new PeerId(endpointId);
            _connected.Remove(id);
            Disconnected?.Invoke(id);
        }

        public void OnPayloadReceived(string msg)
        {
            if (!TryParse2(msg, out var id, out var b64)) return;
            byte[] bytes;
            try { bytes = Convert.FromBase64String(b64); }
            catch (FormatException) { return; }
            PayloadReceived?.Invoke(new PeerId(id), new ArraySegment<byte>(bytes));
        }

        private static bool TryParse2(string msg, out string a, out string b)
        {
            var i = msg?.IndexOf('|') ?? -1;
            if (i < 0) { a = b = null; return false; }
            a = msg.Substring(0, i);
            b = msg.Substring(i + 1);
            return true;
        }

#if UNITY_IOS && !UNITY_EDITOR
        // --- iOS C ABI exposed by NearbyBridge.framework (Swift @_cdecl) ---
        [DllImport("__Internal")] private static extern void NearbyBridge_StartAdvertising(string serviceId, string displayName);
        [DllImport("__Internal")] private static extern void NearbyBridge_StartDiscovery(string serviceId);
        [DllImport("__Internal")] private static extern void NearbyBridge_Stop();
        [DllImport("__Internal")] private static extern void NearbyBridge_RequestConnection(string endpointId);
        [DllImport("__Internal")] private static extern void NearbyBridge_AcceptConnection(string endpointId);
        [DllImport("__Internal")] private static extern void NearbyBridge_RejectConnection(string endpointId);
        [DllImport("__Internal")] private static extern void NearbyBridge_Disconnect(string endpointId);
        [DllImport("__Internal")] private static extern void NearbyBridge_SendBytes(string endpointId, byte[] payload, int length, int reliable);
#endif
    }
}
