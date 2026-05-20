using System;
using System.Threading.Tasks;
using MessagePack;
using MiniGames.GameModule;
using MiniGames.Games.Reversi.Logic;
using MiniGames.Games.Reversi.Multiplayer;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Transport;

namespace MiniGames.Games.Reversi
{
    public sealed class ReversiModule : IGameModule
    {
        public string Id => "reversi";
        public string DisplayName => "Reversi";
        public GameCapabilities Capabilities =>
            GameCapabilities.Solo | GameCapabilities.Multiplayer | GameCapabilities.SameDevice;
        public int MinPlayers => 2;
        public int MaxPlayers => 2;

        private GameContext _ctx;
        private ReversiGame _solo;
        private MultiplayerReversi _mp;

        public ReversiGame SoloGame => _solo;
        public MultiplayerReversi MultiplayerGame => _mp;

        public Task LoadAsync(GameContext ctx) => Task.CompletedTask;

        public void StartSolo(GameContext ctx)
        {
            _ctx = ctx;
            _solo = new ReversiGame();
        }

        public void StartMultiplayer(GameContext ctx, RoomSnapshot room, int seed, bool isHost)
        {
            _ctx = ctx;
            string remoteId = "remote";
            foreach (var p in room.Players)
                if (p.PlayerId != ctx.LocalPlayerId) { remoteId = p.PlayerId; break; }
            _mp = new MultiplayerReversi(ctx.LocalPlayerId, remoteId);
            _mp.MoveOutgoing    += m => Send((MessageType)RVMessageType.Move, m);
            _mp.PassOutgoing    += m => Send((MessageType)RVMessageType.Pass, m);
            _mp.ResignOutgoing  += m => Send((MessageType)RVMessageType.Resign, m);
            _mp.RematchOutgoing += m => Send((MessageType)RVMessageType.Rematch, m);
        }

        private void Send<T>(MessageType type, T body)
        {
            if (_ctx?.Net == null) return;
            _ctx.Net.Broadcast(type, MessagePackSerializer.Serialize(body), reliable: true);
        }

        public void OnPeerMessage(PeerId from, MessageType type, ArraySegment<byte> payload)
        {
            if (_mp == null) return;
            switch ((RVMessageType)(byte)type)
            {
                case RVMessageType.Move:
                    _mp.OnMoveReceived(MessagePackSerializer.Deserialize<RVMoveMessage>(payload));
                    break;
                case RVMessageType.Pass:
                    _mp.OnPassReceived(MessagePackSerializer.Deserialize<RVPassMessage>(payload));
                    break;
                case RVMessageType.Resign:
                    _mp.OnResignReceived(MessagePackSerializer.Deserialize<RVResignMessage>(payload));
                    break;
                case RVMessageType.Rematch:
                    _mp.OnRematchReceived(MessagePackSerializer.Deserialize<RVRematchMessage>(payload));
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
