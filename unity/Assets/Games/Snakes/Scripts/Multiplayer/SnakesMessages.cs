using System.Collections.Generic;
using MessagePack;
using MiniGames.Networking.Protocol;

namespace MiniGames.Games.Snakes.Multiplayer
{
    /// <summary>0x80+ subtypes for Snakes.</summary>
    public enum SnakesMessageType : byte
    {
        InputCmd  = (byte)MessageType.GameSpecificBase,       // 0x80 client -> host
        Snapshot  = (byte)MessageType.GameSpecificBase + 1,   // 0x81 host -> clients
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public sealed class SnakeInputCmd
    {
        public int PlayerIndex;
        public int ClientTick;        // tick the client thinks it's on
        public byte NewDirection;     // cast from Logic.Direction
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public sealed class SnakeSnapshot
    {
        public int Tick;
        public List<SnakeWire> Snakes = new List<SnakeWire>();
        public List<int> FoodFlat = new List<int>(); // x,y pairs
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public sealed class SnakeWire
    {
        public int PlayerIndex;
        public bool IsAlive;
        public byte Heading;
        public List<int> BodyFlat = new List<int>(); // head first, x,y pairs
    }
}
