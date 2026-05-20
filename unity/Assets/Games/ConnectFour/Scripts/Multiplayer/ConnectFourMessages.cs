using MessagePack;
using MiniGames.Networking.Protocol;

namespace MiniGames.Games.ConnectFour.Multiplayer
{
    /// <summary>
    /// Connect Four uses a turn-based protocol: a single Move message per
    /// turn, broadcast to all peers, who apply it to their local copy of
    /// the game. No host-authoritative tick loop, no snapshot needed -
    /// the game state is deterministic from move history alone.
    /// </summary>
    public enum CFMessageType : byte
    {
        Move    = (byte)MessageType.GameSpecificBase,        // 0x80: I just played a move
        Resign  = (byte)MessageType.GameSpecificBase + 1,    // 0x81: I give up
        Rematch = (byte)MessageType.GameSpecificBase + 2,    // 0x82: request a new game
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public sealed class MoveMessage
    {
        public string PlayerId;
        public int Column;
        public int MoveNumber;   // for de-duplication on reconnect
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public sealed class ResignMessage
    {
        public string PlayerId;
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public sealed class RematchMessage
    {
        public string PlayerId;
    }
}
