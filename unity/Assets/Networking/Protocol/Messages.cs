using System.Collections.Generic;

namespace MiniGames.Networking.Protocol
{
    // These are POCOs intended to be MessagePack-serialized once the package is added.
    // For now they're plain classes; we'll add [MessagePackObject] attributes when
    // the runtime dependency is wired up.

    public sealed class Hello
    {
        public string PlayerId;
        public string DisplayName;
        public int AppVersionMajor;
        public int AppVersionMinor;
        public string Platform; // "android" | "ios" | "editor"
    }

    public sealed class RoomSnapshot
    {
        public string RoomId;
        public string HostPlayerId;
        public string SelectedGameId;
        public List<PlayerSlot> Players = new List<PlayerSlot>();
    }

    public sealed class PlayerSlot
    {
        public string PlayerId;
        public string DisplayName;
        public int ColorIndex;
        public bool IsReady;
        public bool IsHost;
        public bool IsConnected;
    }

    public sealed class PlayerReady
    {
        public bool Ready;
    }

    public sealed class SelectGame
    {
        public string GameId;
    }

    public sealed class StartGame
    {
        public string GameId;
        public int CountdownMs;
        public int Seed;
    }

    public sealed class InputCommand
    {
        public int ClientFrame;
        public byte[] Payload; // game-specific opaque blob
    }

    public sealed class StateSnapshot
    {
        public int HostFrame;
        public byte[] Payload; // game-specific opaque blob
    }

    public sealed class GameEvent
    {
        public byte EventType;
        public byte[] Payload;
    }

    public sealed class EndGame
    {
        public List<PlayerResult> Results = new List<PlayerResult>();
    }

    public sealed class PlayerResult
    {
        public string PlayerId;
        public int Score;
        public int Place;
    }

    public sealed class Ping
    {
        public long ClientSendUtcMs;
    }

    public sealed class Pong
    {
        public long ClientSendUtcMs;
        public long HostRecvUtcMs;
    }

    public sealed class Chat
    {
        public string PlayerId;
        public byte EmoteId; // preset set; no free text in v1
    }
}
