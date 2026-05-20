using System;
using System.Threading.Tasks;
using MiniGames.GameModule;
using MiniGames.Games.DotsAndBoxes.Logic;
using MiniGames.Games.DotsAndBoxes.Multiplayer;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Transport;

namespace MiniGames.Games.DotsAndBoxes
{
    public sealed class DotsAndBoxesModule : IGameModule
    {
        public string Id => "dots_and_boxes";
        public string DisplayName => "Dots and Boxes";
        public GameCapabilities Capabilities =>
            GameCapabilities.Solo | GameCapabilities.Multiplayer | GameCapabilities.SameDevice;
        public int MinPlayers => 2;
        public int MaxPlayers => 4;

        private GameContext _ctx;
        private DotsGame _solo;
        private MultiplayerDots _mp;

        public DotsGame SoloGame => _solo;
        public MultiplayerDots MultiplayerGame => _mp;

        public Task LoadAsync(GameContext ctx) => Task.CompletedTask;

        public void StartSolo(GameContext ctx)
        {
            _ctx = ctx;
            _solo = new DotsGame(playerCount: 2);
        }

        public void StartMultiplayer(GameContext ctx, RoomSnapshot room, int seed, bool isHost)
        {
            _ctx = ctx;
            var ids = new string[room.Players.Count];
            for (int i = 0; i < room.Players.Count; i++) ids[i] = room.Players[i].PlayerId;
            _mp = new MultiplayerDots(ctx.LocalPlayerId, ids);
            _mp.MoveOutgoing    += m => Send((MessageType)DBMessageType.Move, m);
            _mp.ResignOutgoing  += m => Send((MessageType)DBMessageType.Resign, m);
            _mp.RematchOutgoing += m => Send((MessageType)DBMessageType.Rematch, m);
        }

        private void Send<T>(MessageType type, T body)
        {
            if (_ctx?.Net == null) return;
            _ctx.Net.Broadcast(type, Json.Serialize(body), reliable: true);
        }

        public void OnPeerMessage(PeerId from, MessageType type, ArraySegment<byte> payload)
        {
            if (_mp == null) return;
            switch ((DBMessageType)(byte)type)
            {
                case DBMessageType.Move:
                    _mp.OnMoveReceived(Json.Deserialize<DBMoveMessage>(payload));
                    break;
                case DBMessageType.Resign:
                    _mp.OnResignReceived(Json.Deserialize<DBResignMessage>(payload));
                    break;
                case DBMessageType.Rematch:
                    _mp.OnRematchReceived(Json.Deserialize<DBRematchMessage>(payload));
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
