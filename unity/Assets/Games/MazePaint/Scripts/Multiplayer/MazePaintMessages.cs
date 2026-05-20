using System.Collections.Generic;
using MiniGames.Networking.Protocol;

namespace MiniGames.Games.MazePaint.Multiplayer
{
    public enum MazeMessageType : byte
    {
        InputCmd = (byte)MessageType.GameSpecificBase,         // 0x80 client -> host
        Snapshot = (byte)MessageType.GameSpecificBase + 1,     // 0x81 host -> clients
    }
    public sealed class MazeInputCmd
    {
        public int PlayerIndex;
        public int ClientTick;
        public byte NewDirection;
    }
    public sealed class MazeSnapshot
    {
        public int Tick;
        public int BoardSize;
        public byte[] OwnerBytes;    // length BoardSize*BoardSize, value = (player+1) or 0
        public byte[] TrailBytes;
        public List<MazePlayerWire> Players = new List<MazePlayerWire>();
    }
    public sealed class MazePlayerWire
    {
        public int Index;
        public bool IsAlive;
        public byte Heading;
        public short HeadX;
        public short HeadY;
        public int OwnedCells;
    }
}
