using MiniGames.Networking.Protocol;

namespace MiniGames.Games.ColorBlocks.Multiplayer
{
    /// <summary>
    /// Game-specific message subtypes for Color Blocks. Allocated from the
    /// 0x80-0xFF range reserved by the protocol layer; see MessageType.cs.
    /// </summary>
    public enum CBMessageType : byte
    {
        Attack          = (byte)MessageType.GameSpecificBase,        // 0x80
        ProgressUpdate  = (byte)MessageType.GameSpecificBase + 1,    // 0x81
        DiedOut         = (byte)MessageType.GameSpecificBase + 2,    // 0x82
    }

    /// <summary>
    /// Sent from a player to all others when their move cleared 2+ lines.
    /// Junk rows = (linesCleared - 1) so single-line clears don't attack.
    /// </summary>
    public sealed class AttackMessage
    {
        public string FromPlayerId;
        public int JunkRows;
        public int Seed;            // deterministic gap-column generator for the receiver
    }
    public sealed class ProgressMessage
    {
        public string PlayerId;
        public int Score;
        public int CellsFilled;     // for quick "danger" UI
    }
    public sealed class DiedOutMessage
    {
        public string PlayerId;
        public int FinalScore;
    }
}
