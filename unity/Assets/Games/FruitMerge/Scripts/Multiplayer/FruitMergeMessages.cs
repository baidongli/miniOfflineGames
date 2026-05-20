using MiniGames.Networking.Protocol;

namespace MiniGames.Games.FruitMerge.Multiplayer
{
    public enum FMMessageType : byte
    {
        Drop          = (byte)MessageType.GameSpecificBase,         // 0x80
        ProgressUpdate = (byte)MessageType.GameSpecificBase + 1,    // 0x81
        DiedOut       = (byte)MessageType.GameSpecificBase + 2,     // 0x82
    }
    public sealed class DropMessage
    {
        public string PlayerId;
        public int Column;
        public byte Tier;
        public int Score;
        public int HighestTier;
    }
    public sealed class ProgressMessage
    {
        public string PlayerId;
        public int Score;
        public int HighestTier;
        public int CellsFilled;
    }
    public sealed class DiedOutMessage
    {
        public string PlayerId;
        public int FinalScore;
        public int HighestTier;
    }
}
