using System;
using System.Threading.Tasks;
using MessagePack;
using MiniGames.GameModule;
using MiniGames.Games.MazePaint.Logic;
using MiniGames.Games.MazePaint.Multiplayer;
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

        private MazePaintGameState _solo;
        private MazePaintMultiplayer _mp;
        private GameContext _ctx;

        public MazePaintGameState SoloState => _solo;
        public MazePaintMultiplayer MultiplayerState => _mp;

        public Task LoadAsync(GameContext ctx) => Task.CompletedTask;

        public void StartSolo(GameContext ctx)
        {
            _ctx = ctx;
            _solo = new MazePaintGameState(BoardSize, playerCount: 1);
        }

        public void StartMultiplayer(GameContext ctx, RoomSnapshot room, int seed, bool isHost)
        {
            _ctx = ctx;
            int playerCount = Mathf.Clamp(room.Players.Count, 2, MaxPlayers);
            int localIndex = FindLocalIndex(room, ctx.LocalPlayerId);
            _mp = new MazePaintMultiplayer(isHost, localIndex, BoardSize, playerCount);

            if (isHost)
                _mp.SnapshotProduced += s => Send((MessageType)MazeMessageType.Snapshot, s);
            else
                _mp.InputProduced += c => SendToHost((MessageType)MazeMessageType.InputCmd, c);
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
            switch ((MazeMessageType)(byte)type)
            {
                case MazeMessageType.InputCmd:
                    _mp.OnRemoteInput(MessagePackSerializer.Deserialize<MazeInputCmd>(payload));
                    break;
                case MazeMessageType.Snapshot:
                    _mp.OnSnapshot(MessagePackSerializer.Deserialize<MazeSnapshot>(payload));
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
