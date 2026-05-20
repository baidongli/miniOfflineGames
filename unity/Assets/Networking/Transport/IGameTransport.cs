using System;

namespace MiniGames.Networking.Transport
{
    public enum TransportRole
    {
        None,
        Host,
        Client
    }

    public enum ConnectionStatus
    {
        Ok,
        Rejected,
        Error
    }

    public readonly struct PeerId : IEquatable<PeerId>
    {
        public readonly string Value;
        public PeerId(string value) { Value = value; }
        public bool Equals(PeerId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is PeerId p && Equals(p);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? "<null>";
        public static bool operator ==(PeerId a, PeerId b) => a.Equals(b);
        public static bool operator !=(PeerId a, PeerId b) => !a.Equals(b);
    }

    /// <summary>
    /// Underlying P2P transport. Implementations: Nearby Connections (Android/iOS),
    /// possibly local-loopback for editor testing.
    /// </summary>
    public interface IGameTransport
    {
        TransportRole Role { get; }

        event Action<PeerId, string> EndpointFound;
        event Action<PeerId> EndpointLost;
        event Action<PeerId, string> ConnectionInitiated;
        event Action<PeerId, ConnectionStatus> ConnectionResult;
        event Action<PeerId> Disconnected;
        event Action<PeerId, ArraySegment<byte>> PayloadReceived;

        void StartAdvertising(string serviceId, string displayName);
        void StartDiscovery(string serviceId);
        void Stop();

        void RequestConnection(PeerId peer);
        void AcceptConnection(PeerId peer);
        void RejectConnection(PeerId peer);
        void Disconnect(PeerId peer);

        void Send(PeerId peer, ArraySegment<byte> payload, bool reliable);
        void Broadcast(ArraySegment<byte> payload, bool reliable);
    }
}
