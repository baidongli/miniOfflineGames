using MiniGames.Networking.Protocol;

namespace MiniGames.Games.Reversi.Multiplayer
{
    public enum RVMessageType : byte
    {
        Move    = (byte)MessageType.GameSpecificBase,        // 0x80
        Pass    = (byte)MessageType.GameSpecificBase + 1,    // 0x81
        Resign  = (byte)MessageType.GameSpecificBase + 2,    // 0x82
        Rematch = (byte)MessageType.GameSpecificBase + 3,    // 0x83
    }
    public sealed class RVMoveMessage
    {
        public string PlayerId;
        public int X, Y;
        public int MoveNumber;
    }
    public sealed class RVPassMessage
    {
        public string PlayerId;
        public int MoveNumber;
    }
    public sealed class RVResignMessage
    {
        public string PlayerId;
    }
    public sealed class RVRematchMessage
    {
        public string PlayerId;
    }
}
