using MiniGames.GameModule;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Transport;

namespace MiniGames.Networking.Session
{
    /// <summary>
    /// IGameSendChannel implementation that routes through a RoomManager.
    /// Game modules see this as their "Net" - they call SendToHost/Broadcast
    /// without knowing about transport or framing.
    /// </summary>
    public sealed class RoomSendChannel : IGameSendChannel
    {
        private readonly RoomManager _room;

        public RoomSendChannel(RoomManager room) { _room = room; }

        public bool IsHost => _room.IsHost;

        public void SendToHost(MessageType type, byte[] payload, bool reliable)
            => _room.SendGameMessageToHost(type, payload, reliable);

        public void Broadcast(MessageType type, byte[] payload, bool reliable)
            => _room.BroadcastGameMessage(type, payload, reliable);

        public void SendTo(PeerId peer, MessageType type, byte[] payload, bool reliable)
            => _room.SendGameMessageTo(peer, type, payload, reliable);
    }

    /// <summary>
    /// No-op send channel for solo play. Games can still call Net.SendToHost
    /// in solo without crashing - it just goes nowhere.
    /// </summary>
    public sealed class NullSendChannel : IGameSendChannel
    {
        public static readonly NullSendChannel Instance = new NullSendChannel();
        public bool IsHost => true;
        public void SendToHost(MessageType type, byte[] payload, bool reliable) { }
        public void Broadcast(MessageType type, byte[] payload, bool reliable) { }
        public void SendTo(PeerId peer, MessageType type, byte[] payload, bool reliable) { }
    }
}
