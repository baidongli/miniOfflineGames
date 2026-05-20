using MiniGames.Networking.Protocol;

namespace MiniGames.Games.Tetris.Multiplayer
{
    public enum TetrisMessageType : byte
    {
        Attack         = (byte)MessageType.GameSpecificBase,        // 0x80
        ProgressUpdate = (byte)MessageType.GameSpecificBase + 1,    // 0x81
        DiedOut        = (byte)MessageType.GameSpecificBase + 2,    // 0x82
    }
    public sealed class TetrisAttackMessage
    {
        public string FromPlayerId;
        public int JunkRows;
        public int Seed;
    }
    public sealed class TetrisProgressMessage
    {
        public string PlayerId;
        public int Score;
        public int Lines;
        public int Level;
        public int Height;   // tallest column - used for "danger" UI on opponent panels
    }
    public sealed class TetrisDiedOutMessage
    {
        public string PlayerId;
        public int FinalScore;
        public int FinalLines;
    }
}
