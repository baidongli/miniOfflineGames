using System;
using System.Threading.Tasks;
using MiniGames.GameModule;
using MiniGames.Games.FruitMerge.Logic;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Transport;
using UnityEngine;

namespace MiniGames.Games.FruitMerge
{
    public sealed class FruitMergeModule : IGameModule
    {
        public string Id => "fruit_merge";
        public string DisplayName => "Fruit Merge";
        public GameCapabilities Capabilities =>
            GameCapabilities.Solo | GameCapabilities.Multiplayer;
        public int MinPlayers => 2;
        public int MaxPlayers => 4;

        private FruitMergeGame _solo;
        public FruitMergeGame SoloGame => _solo;

        public Task LoadAsync(GameContext ctx) => Task.CompletedTask;

        public void StartSolo(GameContext ctx)
        {
            _solo = new FruitMergeGame(seed: UnityEngine.Random.Range(int.MinValue, int.MaxValue));
        }

        public void StartMultiplayer(GameContext ctx, RoomSnapshot room, int seed, bool isHost)
        {
            // Each player runs their own grid seeded identically so they see
            // the same "next fruit" sequence. Score race; attacks TBD.
            _solo = new FruitMergeGame(seed);
        }

        public void Pause() { }
        public void Resume() { }
        public Task UnloadAsync() { _solo = null; return Task.CompletedTask; }
        public void OnPeerMessage(PeerId from, MessageType type, ArraySegment<byte> payload) { }
        public void OnPeerJoined(PeerId peer) { }
        public void OnPeerLeft(PeerId peer) { }
    }
}
