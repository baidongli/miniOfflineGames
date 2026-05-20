using System;
using System.Threading.Tasks;
using MiniGames.GameModule;
using MiniGames.Games.Snakes.Logic;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Transport;
using UnityEngine;

namespace MiniGames.Games.Snakes
{
    public sealed class SnakesModule : IGameModule
    {
        public string Id => "snakes";
        public string DisplayName => "Snakes";
        public GameCapabilities Capabilities =>
            GameCapabilities.Solo | GameCapabilities.Multiplayer | GameCapabilities.SameDevice;
        public int MinPlayers => 1;
        public int MaxPlayers => 4;

        public const int BoardSize = 20;
        public const float TickHz = 10f;

        private SnakesGameState _state;
        private GameContext _ctx;

        public Task LoadAsync(GameContext ctx) => Task.CompletedTask;

        public void StartSolo(GameContext ctx)
        {
            _ctx = ctx;
            _state = new SnakesGameState(BoardSize, BoardSize, playerCount: 1,
                seed: UnityEngine.Random.Range(int.MinValue, int.MaxValue));
        }

        public void StartMultiplayer(GameContext ctx, RoomSnapshot room, int seed, bool isHost)
        {
            _ctx = ctx;
            _state = new SnakesGameState(BoardSize, BoardSize,
                playerCount: Mathf.Clamp(room.Players.Count, 2, MaxPlayers), seed: seed);
        }

        public void Pause() { }
        public void Resume() { }

        public Task UnloadAsync()
        {
            _state = null;
            _ctx = null;
            return Task.CompletedTask;
        }

        public void OnPeerMessage(PeerId from, MessageType type, ArraySegment<byte> payload) { }
        public void OnPeerJoined(PeerId peer) { }
        public void OnPeerLeft(PeerId peer) { }
    }
}
