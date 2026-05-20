using System;
using System.Threading.Tasks;
using MiniGames.GameModule;
using MiniGames.Games.FruitMerge.Logic;
using MiniGames.Games.FruitMerge.Multiplayer;
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
        private MultiplayerFruitMerge _mp;
        private GameContext _ctx;

        public FruitMergeGame SoloGame => _solo;
        public MultiplayerFruitMerge MultiplayerGame => _mp;

        public Task LoadAsync(GameContext ctx) => Task.CompletedTask;

        public void StartSolo(GameContext ctx)
        {
            _ctx = ctx;
            _solo = new FruitMergeGame(seed: UnityEngine.Random.Range(int.MinValue, int.MaxValue));
        }

        public void StartMultiplayer(GameContext ctx, RoomSnapshot room, int seed, bool isHost)
        {
            _ctx = ctx;
            _mp = new MultiplayerFruitMerge(ctx.LocalPlayerId, seed);
            _mp.DropOutgoing     += m => Send((MessageType)FMMessageType.Drop, m);
            _mp.ProgressOutgoing += m => Send((MessageType)FMMessageType.ProgressUpdate, m);
            _mp.DiedOutOutgoing  += m => Send((MessageType)FMMessageType.DiedOut, m);
        }

        private void Send<T>(MessageType type, T body)
        {
            if (_ctx?.Net == null) return;
            _ctx.Net.Broadcast(type, Json.Serialize(body), reliable: true);
        }

        public void OnPeerMessage(PeerId from, MessageType type, ArraySegment<byte> payload)
        {
            if (_mp == null) return;
            switch ((FMMessageType)(byte)type)
            {
                case FMMessageType.Drop:
                    _mp.OnDropReceived(Json.Deserialize<DropMessage>(payload));
                    break;
                case FMMessageType.ProgressUpdate:
                    _mp.OnProgressReceived(Json.Deserialize<ProgressMessage>(payload));
                    break;
                case FMMessageType.DiedOut:
                    _mp.OnDiedOutReceived(Json.Deserialize<DiedOutMessage>(payload));
                    break;
            }
        }

        public void OnPeerJoined(PeerId peer) { }
        public void OnPeerLeft(PeerId peer) { }
        public void Pause() { }
        public void Resume() { }
        public Task UnloadAsync() { _solo = null; _mp = null; _ctx = null; return Task.CompletedTask; }
    }
}
