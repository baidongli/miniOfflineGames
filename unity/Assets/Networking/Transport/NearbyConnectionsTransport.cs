using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGames.Networking.Transport
{
    /// <summary>
    /// Unity-side facade over the native Nearby Connections bridges.
    /// Android: AndroidJavaObject -> NearbyBridge.aar
    /// iOS:     DllImport("__Internal") -> NearbyBridge.framework
    ///
    /// This class is a stub. Native plugin work tracked in native/android and native/ios.
    /// </summary>
    public sealed class NearbyConnectionsTransport : IGameTransport
    {
        public TransportRole Role { get; private set; } = TransportRole.None;

        public event Action<PeerId, string> EndpointFound;
        public event Action<PeerId> EndpointLost;
        public event Action<PeerId, string> ConnectionInitiated;
        public event Action<PeerId, ConnectionStatus> ConnectionResult;
        public event Action<PeerId> Disconnected;
        public event Action<PeerId, ArraySegment<byte>> PayloadReceived;

        private readonly HashSet<PeerId> _connected = new HashSet<PeerId>();

        public void StartAdvertising(string serviceId, string displayName)
        {
            Role = TransportRole.Host;
            Debug.Log($"[Nearby] startAdvertising serviceId={serviceId} name={displayName}");
            // TODO: call native bridge.
        }

        public void StartDiscovery(string serviceId)
        {
            Role = TransportRole.Client;
            Debug.Log($"[Nearby] startDiscovery serviceId={serviceId}");
            // TODO: call native bridge.
        }

        public void Stop()
        {
            Debug.Log("[Nearby] stop");
            Role = TransportRole.None;
            _connected.Clear();
            // TODO: call native bridge.
        }

        public void RequestConnection(PeerId peer)
        {
            Debug.Log($"[Nearby] requestConnection {peer}");
            // TODO
        }

        public void AcceptConnection(PeerId peer)
        {
            Debug.Log($"[Nearby] acceptConnection {peer}");
            _connected.Add(peer);
            // TODO
        }

        public void RejectConnection(PeerId peer)
        {
            Debug.Log($"[Nearby] rejectConnection {peer}");
            // TODO
        }

        public void Disconnect(PeerId peer)
        {
            Debug.Log($"[Nearby] disconnect {peer}");
            _connected.Remove(peer);
            // TODO
        }

        public void Send(PeerId peer, ArraySegment<byte> payload, bool reliable)
        {
            // TODO: route to native sendBytes / sendStream.
        }

        public void Broadcast(ArraySegment<byte> payload, bool reliable)
        {
            foreach (var p in _connected)
                Send(p, payload, reliable);
        }

        // --- Callbacks invoked from native side (via UnitySendMessage or AndroidJavaProxy) ---
        // Method names match what the native bridges will call. Keep stable.

        internal void OnEndpointFound(string endpointId, string name)
            => EndpointFound?.Invoke(new PeerId(endpointId), name);

        internal void OnEndpointLost(string endpointId)
            => EndpointLost?.Invoke(new PeerId(endpointId));

        internal void OnConnectionInitiated(string endpointId, string name)
            => ConnectionInitiated?.Invoke(new PeerId(endpointId), name);

        internal void OnConnectionResult(string endpointId, int status)
            => ConnectionResult?.Invoke(new PeerId(endpointId), (ConnectionStatus)status);

        internal void OnDisconnected(string endpointId)
        {
            var id = new PeerId(endpointId);
            _connected.Remove(id);
            Disconnected?.Invoke(id);
        }

        internal void OnPayloadReceived(string endpointId, byte[] payload)
            => PayloadReceived?.Invoke(new PeerId(endpointId), new ArraySegment<byte>(payload));
    }
}
