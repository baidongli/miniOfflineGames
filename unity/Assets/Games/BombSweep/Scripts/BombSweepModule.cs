using System;
using System.Threading.Tasks;
using MiniGames.GameModule;
using MiniGames.Games.BombSweep.Logic;
using MiniGames.Games.BombSweep.Multiplayer;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Transport;
using UnityEngine;

namespace MiniGames.Games.BombSweep
{
    public sealed class BombSweepModule : IGameModule
    {
        public string Id => "bomb_sweep";
        public string DisplayName => "Bomb Sweep";
        public GameCapabilities Capabilities =>
            GameCapabilities.Solo | GameCapabilities.Multiplayer | GameCapabilities.SameDevice;
        public int MinPlayers => 2;
        public int MaxPlayers => 4;

        public const float TickHz = 8f;

        private GameContext _ctx;
        private BombSweepGameState _solo;
        private MultiplayerBombSweep _mp;

        public BombSweepGameState SoloState => _solo;
        public MultiplayerBombSweep MultiplayerState => _mp;

        public Task LoadAsync(GameContext ctx) => Task.CompletedTask;

        public void StartSolo(GameContext ctx)
        {
            _ctx = ctx;
            _solo = new BombSweepGameState(playerCount: 1,
                seed: UnityEngine.Random.Range(int.MinValue, int.MaxValue));
        }

        public void StartMultiplayer(GameContext ctx, RoomSnapshot room, int seed, bool isHost)
        {
            _ctx = ctx;
            int playerCount = Mathf.Clamp(room.Players.Count, 2, MaxPlayers);
            int localIndex = FindLocalIndex(room, ctx.LocalPlayerId);
            _mp = new MultiplayerBombSweep(isHost, localIndex, playerCount, seed);
            if (isHost)
                _mp.SnapshotProduced += s => Send((MessageType)BSMessageType.Snapshot, s);
            else
                _mp.InputProduced += c => SendToHost((MessageType)BSMessageType.InputCmd, c);
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
            _ctx.Net.Broadcast(type, Json.Serialize(body), reliable: false);
        }

        private void SendToHost<T>(MessageType type, T body)
        {
            if (_ctx?.Net == null) return;
            _ctx.Net.SendToHost(type, Json.Serialize(body), reliable: false);
        }

        public void OnPeerMessage(PeerId from, MessageType type, ArraySegment<byte> payload)
        {
            if (_mp == null) return;
            switch ((BSMessageType)(byte)type)
            {
                case BSMessageType.InputCmd:
                    _mp.OnRemoteInput(Json.Deserialize<BSInputCmd>(payload));
                    break;
                case BSMessageType.Snapshot:
                    _mp.OnSnapshot(Json.Deserialize<BSSnapshot>(payload));
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
