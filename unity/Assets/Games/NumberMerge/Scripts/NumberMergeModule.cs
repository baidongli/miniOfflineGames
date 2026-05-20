using System;
using System.Threading.Tasks;
using MessagePack;
using MiniGames.GameModule;
using MiniGames.Games.NumberMerge.Logic;
using MiniGames.Games.NumberMerge.Multiplayer;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Transport;

namespace MiniGames.Games.NumberMerge
{
    public sealed class NumberMergeModule : IGameModule
    {
        public string Id => "number_merge";
        public string DisplayName => "2048";
        public GameCapabilities Capabilities =>
            GameCapabilities.Solo | GameCapabilities.Multiplayer;
        public int MinPlayers => 2;
        public int MaxPlayers => 4;

        private GameContext _ctx;
        private NumberMergeGame _solo;
        private MultiplayerNumberMerge _mp;

        public NumberMergeGame SoloGame => _solo;
        public MultiplayerNumberMerge MultiplayerGame => _mp;

        public Task LoadAsync(GameContext ctx) => Task.CompletedTask;

        public void StartSolo(GameContext ctx)
        {
            _ctx = ctx;
            _solo = new NumberMergeGame(seed: UnityEngine.Random.Range(int.MinValue, int.MaxValue));
        }

        public void StartMultiplayer(GameContext ctx, RoomSnapshot room, int seed, bool isHost)
        {
            _ctx = ctx;
            _mp = new MultiplayerNumberMerge(ctx.LocalPlayerId, seed);
            _mp.ProgressOutgoing    += m => Send((MessageType)NMMessageType.Progress, m);
            _mp.DiedOutOutgoing     += m => Send((MessageType)NMMessageType.DiedOut, m);
            _mp.ReachedGoalOutgoing += m => Send((MessageType)NMMessageType.ReachedGoal, m);
        }

        private void Send<T>(MessageType type, T body)
        {
            if (_ctx?.Net == null) return;
            _ctx.Net.Broadcast(type, MessagePackSerializer.Serialize(body), reliable: true);
        }

        public void OnPeerMessage(PeerId from, MessageType type, ArraySegment<byte> payload)
        {
            if (_mp == null) return;
            switch ((NMMessageType)(byte)type)
            {
                case NMMessageType.Progress:
                    _mp.OnProgressReceived(MessagePackSerializer.Deserialize<NMProgressMessage>(payload));
                    break;
                case NMMessageType.DiedOut:
                    _mp.OnDiedOutReceived(MessagePackSerializer.Deserialize<NMDiedOutMessage>(payload));
                    break;
                case NMMessageType.ReachedGoal:
                    _mp.OnReachedGoalReceived(MessagePackSerializer.Deserialize<NMReachedGoalMessage>(payload));
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
