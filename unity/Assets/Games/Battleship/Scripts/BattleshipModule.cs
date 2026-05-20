using System;
using System.Threading.Tasks;
using MessagePack;
using MiniGames.GameModule;
using MiniGames.Games.Battleship.Logic;
using MiniGames.Games.Battleship.Multiplayer;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Transport;

namespace MiniGames.Games.Battleship
{
    public sealed class BattleshipModule : IGameModule
    {
        public string Id => "battleship";
        public string DisplayName => "Battleship";
        public GameCapabilities Capabilities =>
            GameCapabilities.Solo | GameCapabilities.Multiplayer | GameCapabilities.SameDevice;
        public int MinPlayers => 2;
        public int MaxPlayers => 2;

        private GameContext _ctx;
        private BattleshipGame _solo;
        private MultiplayerBattleship _mp;

        public BattleshipGame SoloGame => _solo;
        public MultiplayerBattleship MultiplayerGame => _mp;

        public Task LoadAsync(GameContext ctx) => Task.CompletedTask;

        public void StartSolo(GameContext ctx)
        {
            _ctx = ctx;
            _solo = new BattleshipGame(localSeat: 0);
        }

        public void StartMultiplayer(GameContext ctx, RoomSnapshot room, int seed, bool isHost)
        {
            _ctx = ctx;
            string remoteId = "remote";
            foreach (var p in room.Players)
                if (p.PlayerId != ctx.LocalPlayerId) { remoteId = p.PlayerId; break; }
            _mp = new MultiplayerBattleship(ctx.LocalPlayerId, remoteId);
            _mp.ShipsReadyOutgoing  += m => Send((MessageType)BTLMessageType.ShipsReady, m);
            _mp.ShotFiredOutgoing   += m => Send((MessageType)BTLMessageType.ShotFired, m);
            _mp.ShotResultOutgoing  += m => Send((MessageType)BTLMessageType.ShotResult, m);
            _mp.ResignOutgoing      += m => Send((MessageType)BTLMessageType.Resign, m);
        }

        private void Send<T>(MessageType type, T body)
        {
            if (_ctx?.Net == null) return;
            _ctx.Net.Broadcast(type, MessagePackSerializer.Serialize(body), reliable: true);
        }

        public void OnPeerMessage(PeerId from, MessageType type, ArraySegment<byte> payload)
        {
            if (_mp == null) return;
            switch ((BTLMessageType)(byte)type)
            {
                case BTLMessageType.ShipsReady:
                    _mp.OnShipsReadyReceived(MessagePackSerializer.Deserialize<BTLShipsReadyMessage>(payload));
                    break;
                case BTLMessageType.ShotFired:
                    _mp.OnShotFiredReceived(MessagePackSerializer.Deserialize<BTLShotFiredMessage>(payload));
                    break;
                case BTLMessageType.ShotResult:
                    _mp.OnShotResultReceived(MessagePackSerializer.Deserialize<BTLShotResultMessage>(payload));
                    break;
                case BTLMessageType.Resign:
                    _mp.OnResignReceived(MessagePackSerializer.Deserialize<BTLResignMessage>(payload));
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
