using System;
using System.Threading.Tasks;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Transport;

namespace MiniGames.GameModule
{
    [Flags]
    public enum GameCapabilities
    {
        None        = 0,
        Solo        = 1 << 0,
        Multiplayer = 1 << 1,
        SameDevice  = 1 << 2  // 1 device, multiple players on same screen (hot-seat or split)
    }

    /// <summary>
    /// Contract every mini-game implements. The Hub loads the module, builds a
    /// GameContext, and calls Start*. The module talks back via the context's
    /// SendToHost/Broadcast and OnPeerMessage hooks — never directly to App.
    /// </summary>
    public interface IGameModule
    {
        string Id { get; }
        string DisplayName { get; }
        GameCapabilities Capabilities { get; }
        int MinPlayers { get; }
        int MaxPlayers { get; }

        Task LoadAsync(GameContext ctx);
        void StartSolo(GameContext ctx);
        void StartMultiplayer(GameContext ctx, RoomSnapshot room, int seed, bool isHost);
        void Pause();
        void Resume();
        Task UnloadAsync();

        void OnPeerMessage(PeerId from, MessageType type, ArraySegment<byte> payload);
        void OnPeerJoined(PeerId peer);
        void OnPeerLeft(PeerId peer);
    }
}
