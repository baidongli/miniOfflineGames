using System.Collections.Generic;
using MessagePack;
using MiniGames.Networking.Protocol;

namespace MiniGames.Games.BombSweep.Multiplayer
{
    public enum BSMessageType : byte
    {
        InputCmd = (byte)MessageType.GameSpecificBase,        // 0x80 client -> host
        Snapshot = (byte)MessageType.GameSpecificBase + 1,    // 0x81 host -> clients
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public sealed class BSInputCmd
    {
        public int PlayerIndex;
        public int ClientTick;
        public byte Heading;
        public bool PlaceBomb;
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public sealed class BSSnapshot
    {
        public int Tick;
        public int Width;
        public int Height;
        public byte[] Cells;                              // length Width*Height
        public List<BSPlayerWire> Players = new List<BSPlayerWire>();
        public List<BSBombWire> Bombs = new List<BSBombWire>();
        public List<BSExplosionWire> Explosions = new List<BSExplosionWire>();
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public sealed class BSPlayerWire
    {
        public int Index;
        public short X, Y;
        public byte Heading;
        public byte MaxBombs;
        public byte CurrentBombs;
        public byte Range;
        public byte SpeedTicksPerCell;
        public bool IsAlive;
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public sealed class BSBombWire
    {
        public short X, Y;
        public int OwnerIndex;
        public byte Range;
        public short TicksUntilExplode;
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public sealed class BSExplosionWire
    {
        public List<short> CellsFlat = new List<short>();   // x,y pairs
        public byte TicksUntilFade;
    }
}
