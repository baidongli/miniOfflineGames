using System;
using System.Collections.Generic;

namespace MiniGames.Networking.Transport
{
    /// <summary>
    /// In-memory transport for editor testing and unit tests. Several
    /// MockTransports share a MockNetwork; advertising hosts are auto-found
    /// by discovering clients, connection requests are auto-accepted, and
    /// payloads are delivered synchronously.
    /// </summary>
    public sealed class MockNetwork
    {
        private readonly List<MockTransport> _all = new List<MockTransport>();

        public void Register(MockTransport t) { _all.Add(t); }
        public void Unregister(MockTransport t) { _all.Remove(t); }

        public IReadOnlyList<MockTransport> AllAdvertisingWith(string serviceId)
        {
            var r = new List<MockTransport>();
            for (int i = 0; i < _all.Count; i++)
                if (_all[i].IsAdvertising && _all[i].ServiceId == serviceId) r.Add(_all[i]);
            return r;
        }

        public void NotifyAdvertiserAppeared(MockTransport advertiser)
        {
            for (int i = 0; i < _all.Count; i++)
            {
                var t = _all[i];
                if (t == advertiser) continue;
                if (t.IsDiscovering && t.ServiceId == advertiser.ServiceId)
                    t.RaiseEndpointFound(advertiser);
            }
        }

        public void NotifyDiscovererAppeared(MockTransport discoverer)
        {
            for (int i = 0; i < _all.Count; i++)
            {
                var t = _all[i];
                if (t == discoverer) continue;
                if (t.IsAdvertising && t.ServiceId == discoverer.ServiceId)
                    discoverer.RaiseEndpointFound(t);
            }
        }

        public MockTransport Find(string id)
        {
            for (int i = 0; i < _all.Count; i++)
                if (_all[i].Id == id) return _all[i];
            return null;
        }
    }

    public sealed class MockTransport : IGameTransport
    {
        public string Id { get; }
        public string DisplayName { get; private set; }
        public string ServiceId { get; private set; }
        public bool IsAdvertising { get; private set; }
        public bool IsDiscovering { get; private set; }
        public TransportRole Role { get; private set; }

        public event Action<PeerId, string> EndpointFound;
        public event Action<PeerId> EndpointLost;
        public event Action<PeerId, string> ConnectionInitiated;
        public event Action<PeerId, ConnectionStatus> ConnectionResult;
        public event Action<PeerId> Disconnected;
        public event Action<PeerId, ArraySegment<byte>> PayloadReceived;

        private readonly MockNetwork _net;
        private readonly HashSet<string> _connectedIds = new HashSet<string>();

        public MockTransport(string id, MockNetwork net)
        {
            Id = id;
            _net = net;
            net.Register(this);
        }

        public IReadOnlyCollection<string> ConnectedIds => _connectedIds;

        public void StartAdvertising(string serviceId, string displayName)
        {
            ServiceId = serviceId;
            DisplayName = displayName;
            IsAdvertising = true;
            Role = TransportRole.Host;
            _net.NotifyAdvertiserAppeared(this);
        }

        public void StartDiscovery(string serviceId)
        {
            ServiceId = serviceId;
            IsDiscovering = true;
            Role = TransportRole.Client;
            // Fire endpoint-found for any host already advertising on this service.
            foreach (var adv in _net.AllAdvertisingWith(serviceId))
                EndpointFound?.Invoke(new PeerId(adv.Id), adv.DisplayName);
            _net.NotifyDiscovererAppeared(this);
        }

        public void Stop()
        {
            IsAdvertising = false;
            IsDiscovering = false;
            foreach (var id in new List<string>(_connectedIds)) Disconnect(new PeerId(id));
        }

        public void RequestConnection(PeerId peer)
        {
            var other = _net.Find(peer.Value);
            if (other == null) { ConnectionResult?.Invoke(peer, ConnectionStatus.Error); return; }
            // Both sides observe a ConnectionInitiated; the test driver calls
            // AcceptConnection on both to complete.
            ConnectionInitiated?.Invoke(new PeerId(other.Id), other.DisplayName);
            other.ConnectionInitiated?.Invoke(new PeerId(Id), DisplayName ?? Id);
        }

        public void AcceptConnection(PeerId peer)
        {
            var other = _net.Find(peer.Value);
            if (other == null) return;
            _connectedIds.Add(other.Id);
            other._connectedIds.Add(Id);
            ConnectionResult?.Invoke(peer, ConnectionStatus.Ok);
            other.ConnectionResult?.Invoke(new PeerId(Id), ConnectionStatus.Ok);
        }

        public void RejectConnection(PeerId peer)
        {
            var other = _net.Find(peer.Value);
            if (other == null) return;
            ConnectionResult?.Invoke(peer, ConnectionStatus.Rejected);
            other.ConnectionResult?.Invoke(new PeerId(Id), ConnectionStatus.Rejected);
        }

        public void Disconnect(PeerId peer)
        {
            var other = _net.Find(peer.Value);
            if (other == null) return;
            _connectedIds.Remove(other.Id);
            other._connectedIds.Remove(Id);
            Disconnected?.Invoke(peer);
            other.Disconnected?.Invoke(new PeerId(Id));
        }

        public void Send(PeerId peer, ArraySegment<byte> payload, bool reliable)
        {
            var other = _net.Find(peer.Value);
            if (other == null) return;
            // Copy so the caller can mutate their buffer.
            var copy = new byte[payload.Count];
            Buffer.BlockCopy(payload.Array, payload.Offset, copy, 0, payload.Count);
            other.PayloadReceived?.Invoke(new PeerId(Id), new ArraySegment<byte>(copy));
        }

        public void Broadcast(ArraySegment<byte> payload, bool reliable)
        {
            foreach (var id in _connectedIds) Send(new PeerId(id), payload, reliable);
        }

        // --- Internal helpers ---

        internal void RaiseEndpointFound(MockTransport other)
            => EndpointFound?.Invoke(new PeerId(other.Id), other.DisplayName);
    }
}
