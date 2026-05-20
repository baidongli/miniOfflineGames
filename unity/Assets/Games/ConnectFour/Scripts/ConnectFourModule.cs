using System;
using System.Threading.Tasks;
using MiniGames.GameModule;
using MiniGames.Games.ConnectFour.Logic;
using MiniGames.Games.ConnectFour.Multiplayer;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Transport;
using UnityEngine;

namespace MiniGames.Games.ConnectFour
{
    public sealed class ConnectFourModule : IGameModule
    {
        public string Id => "connect_four";
        public string DisplayName => "Connect Four";
        public GameCapabilities Capabilities =>
            GameCapabilities.Solo | GameCapabilities.Multiplayer | GameCapabilities.SameDevice;
        public int MinPlayers => 2;
        public int MaxPlayers => 2;

        private GameContext _ctx;
        private ConnectFourGame _solo;
        private MultiplayerConnectFour _mp;

        public ConnectFourGame SoloGame => _solo;
        public MultiplayerConnectFour MultiplayerGame => _mp;

        public Task LoadAsync(GameContext ctx) => Task.CompletedTask;

        public void StartSolo(GameContext ctx)
        {
            _ctx = ctx;
            _solo = new ConnectFourGame();
            _solo.Moved += r =>
            {
                _ctx?.Audio?.PlaySfx("drop");
                if (r.ResultAfter != GameResult.InProgress)
                    _ctx?.Audio?.PlaySfx(r.ResultAfter == GameResult.Draw ? "draw" : "win");
            };
        }

        public void StartMultiplayer(GameContext ctx, RoomSnapshot room, int seed, bool isHost)
        {
            _ctx = ctx;
            string remoteId = "remote";
            foreach (var p in room.Players)
                if (p.PlayerId != ctx.LocalPlayerId) { remoteId = p.PlayerId; break; }
            _mp = new MultiplayerConnectFour(ctx.LocalPlayerId, remoteId);
            _mp.MoveOutgoing    += m => Send((MessageType)CFMessageType.Move, m);
            _mp.ResignOutgoing  += m => Send((MessageType)CFMessageType.Resign, m);
            _mp.RematchOutgoing += m => Send((MessageType)CFMessageType.Rematch, m);
        }

        private void Send<T>(MessageType type, T body)
        {
            if (_ctx?.Net == null) return;
            _ctx.Net.Broadcast(type, Json.Serialize(body), reliable: true);
        }

        public void OnPeerMessage(PeerId from, MessageType type, ArraySegment<byte> payload)
        {
            if (_mp == null) return;
            switch ((CFMessageType)(byte)type)
            {
                case CFMessageType.Move:
                    _mp.OnMoveReceived(Json.Deserialize<MoveMessage>(payload));
                    break;
                case CFMessageType.Resign:
                    _mp.OnResignReceived(Json.Deserialize<ResignMessage>(payload));
                    break;
                case CFMessageType.Rematch:
                    _mp.OnRematchReceived(Json.Deserialize<RematchMessage>(payload));
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
