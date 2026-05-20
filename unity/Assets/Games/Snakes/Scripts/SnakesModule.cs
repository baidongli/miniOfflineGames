using System;
using System.Threading.Tasks;
using MessagePack;
using MiniGames.GameModule;
using MiniGames.Games.Snakes.Logic;
using MiniGames.Games.Snakes.Multiplayer;
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
        public int MinPlayers => 2;
        public int MaxPlayers => 4;

        public const int BoardSize = 20;
        public const float TickHz = 10f;

        private SnakesGameState _solo;
        private SnakesMultiplayer _mp;
        private GameContext _ctx;

        public SnakesGameState SoloState => _solo;
        public SnakesMultiplayer MultiplayerState => _mp;

        public Task LoadAsync(GameContext ctx) => Task.CompletedTask;

        public void StartSolo(GameContext ctx)
        {
            _ctx = ctx;
            _solo = new SnakesGameState(BoardSize, BoardSize, playerCount: 1,
                seed: UnityEngine.Random.Range(int.MinValue, int.MaxValue));
        }

        public void StartMultiplayer(GameContext ctx, RoomSnapshot room, int seed, bool isHost)
        {
            _ctx = ctx;
            int playerCount = Mathf.Clamp(room.Players.Count, 2, MaxPlayers);
            int localIndex = FindLocalIndex(room, ctx.LocalPlayerId);
            _mp = new SnakesMultiplayer(isHost, localIndex, BoardSize, BoardSize, playerCount, seed);

            if (isHost)
                _mp.SnapshotProduced += snap => Send((MessageType)SnakesMessageType.Snapshot, snap);
            else
                _mp.InputProduced += cmd => SendToHost((MessageType)SnakesMessageType.InputCmd, cmd);
        }

        private static int FindLocalIndex(RoomSnapshot room, string localPlayerId)
        {
            for (int i = 0; i < room.Players.Count; i++)
                if (room.Players[i].PlayerId == localPlayerId) return i;
            return 0;
        }

        private void Send<T>(MessageType type, T body)
        {
            if (_ctx?.Net == null) return;
            _ctx.Net.Broadcast(type, MessagePackSerializer.Serialize(body), reliable: false);
        }

        private void SendToHost<T>(MessageType type, T body)
        {
            if (_ctx?.Net == null) return;
            _ctx.Net.SendToHost(type, MessagePackSerializer.Serialize(body), reliable: false);
        }

        public void OnPeerMessage(PeerId from, MessageType type, ArraySegment<byte> payload)
        {
            if (_mp == null) return;
            switch ((SnakesMessageType)(byte)type)
            {
                case SnakesMessageType.InputCmd:
                    var cmd = MessagePackSerializer.Deserialize<SnakeInputCmd>(payload);
                    _mp.OnRemoteInput(cmd);
                    break;
                case SnakesMessageType.Snapshot:
                    var snap = MessagePackSerializer.Deserialize<SnakeSnapshot>(payload);
                    _mp.OnSnapshot(snap);
                    break;
            }
        }

        public void OnPeerJoined(PeerId peer) { }
        public void OnPeerLeft(PeerId peer) { }
        public void Pause() { }
        public void Resume() { }

        public Task UnloadAsync()
        {
            _solo = null;
            _mp = null;
            _ctx = null;
            return Task.CompletedTask;
        }
    }
}
