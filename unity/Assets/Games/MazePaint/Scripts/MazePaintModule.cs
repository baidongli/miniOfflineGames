using System;
using System.Threading.Tasks;
using MiniGames.GameModule;
using MiniGames.Games.MazePaint.Logic;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Transport;
using UnityEngine;

namespace MiniGames.Games.MazePaint
{
    public sealed class MazePaintModule : IGameModule
    {
        public string Id => "maze_paint";
        public string DisplayName => "Maze Paint";
        public GameCapabilities Capabilities =>
            GameCapabilities.Solo | GameCapabilities.Multiplayer | GameCapabilities.SameDevice;
        public int MinPlayers => 2;
        public int MaxPlayers => 4;

        public const int BoardSize = 24;
        public const float TickHz = 8f;

        private MazePaintGameState _state;
        public MazePaintGameState State => _state;

        public Task LoadAsync(GameContext ctx) => Task.CompletedTask;

        public void StartSolo(GameContext ctx)
        {
            _state = new MazePaintGameState(BoardSize, playerCount: 1);
        }

        public void StartMultiplayer(GameContext ctx, RoomSnapshot room, int seed, bool isHost)
        {
            int playerCount = Mathf.Clamp(room.Players.Count, 2, MaxPlayers);
            _state = new MazePaintGameState(BoardSize, playerCount);
        }

        public void Pause() { }
        public void Resume() { }
        public Task UnloadAsync() { _state = null; return Task.CompletedTask; }
        public void OnPeerMessage(PeerId from, MessageType type, ArraySegment<byte> payload) { }
        public void OnPeerJoined(PeerId peer) { }
        public void OnPeerLeft(PeerId peer) { }
    }
}
