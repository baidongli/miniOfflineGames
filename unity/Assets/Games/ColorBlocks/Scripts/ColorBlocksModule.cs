using System;
using System.Threading.Tasks;
using MiniGames.GameModule;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Transport;
using UnityEngine;

namespace MiniGames.Games.ColorBlocks
{
    public sealed class ColorBlocksModule : IGameModule
    {
        public string Id => "color_blocks";
        public string DisplayName => "Color Blocks";
        public GameCapabilities Capabilities =>
            GameCapabilities.Solo | GameCapabilities.Multiplayer | GameCapabilities.SameDevice;
        public int MinPlayers => 1;
        public int MaxPlayers => 4;

        public Task LoadAsync(GameContext ctx) => Task.CompletedTask;

        public void StartSolo(GameContext ctx)
        {
            Debug.Log("[ColorBlocks] StartSolo");
        }

        public void StartMultiplayer(GameContext ctx, RoomSnapshot room, int seed, bool isHost)
        {
            Debug.Log($"[ColorBlocks] StartMultiplayer host={isHost} seed={seed} players={room.Players.Count}");
        }

        public void Pause() { }
        public void Resume() { }
        public Task UnloadAsync() => Task.CompletedTask;

        public void OnPeerMessage(PeerId from, MessageType type, ArraySegment<byte> payload) { }
        public void OnPeerJoined(PeerId peer) { }
        public void OnPeerLeft(PeerId peer) { }
    }
}
