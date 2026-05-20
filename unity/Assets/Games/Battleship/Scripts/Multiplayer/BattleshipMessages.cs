using System.Collections.Generic;
using MiniGames.Networking.Protocol;

namespace MiniGames.Games.Battleship.Multiplayer
{
    public enum BTLMessageType : byte
    {
        ShipsReady = (byte)MessageType.GameSpecificBase,        // 0x80
        ShotFired  = (byte)MessageType.GameSpecificBase + 1,    // 0x81 shooter -> target
        ShotResult = (byte)MessageType.GameSpecificBase + 2,    // 0x82 target  -> shooter
        Resign     = (byte)MessageType.GameSpecificBase + 3,    // 0x83
    }
    public sealed class BTLShipsReadyMessage
    {
        public string PlayerId;
    }
    public sealed class BTLShotFiredMessage
    {
        public string PlayerId;     // shooter
        public int X, Y;
        public int MoveNumber;
    }
    public sealed class BTLShotResultMessage
    {
        public string PlayerId;     // the responder (target)
        public int X, Y;
        public byte Result;         // ShotResult enum
        public byte SunkKind;       // 0 if not sunk; otherwise ShipKind
        public List<int> SunkCellsFlat = new List<int>();   // x,y pairs of the sunk ship
        public int InResponseToMove;
    }
    public sealed class BTLResignMessage
    {
        public string PlayerId;
    }
}
