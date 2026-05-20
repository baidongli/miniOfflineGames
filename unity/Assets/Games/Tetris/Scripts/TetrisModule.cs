using System;
using System.Threading.Tasks;
using MiniGames.GameModule;
using MiniGames.Games.Tetris.Logic;
using MiniGames.Games.Tetris.Multiplayer;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Transport;
using UnityEngine;

namespace MiniGames.Games.Tetris
{
    public sealed class TetrisModule : IGameModule
    {
        public string Id => "tetris";
        public string DisplayName => "Tetris";
        public GameCapabilities Capabilities =>
            GameCapabilities.Solo | GameCapabilities.Multiplayer | GameCapabilities.SameDevice;
        public int MinPlayers => 2;
        public int MaxPlayers => 4;

        private GameContext _ctx;
        private TetrisGame _solo;
        private MultiplayerTetris _mp;

        public TetrisGame SoloGame => _solo;
        public MultiplayerTetris MultiplayerGame => _mp;

        public Task LoadAsync(GameContext ctx) => Task.CompletedTask;

        public void StartSolo(GameContext ctx)
        {
            _ctx = ctx;
            _solo = new TetrisGame(seed: UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            _solo.Locked += OnLocalLocked;
        }

        public void StartMultiplayer(GameContext ctx, RoomSnapshot room, int seed, bool isHost)
        {
            _ctx = ctx;
            _mp = new MultiplayerTetris(ctx.LocalPlayerId, seed);
            _mp.AttackOutgoing     += m => Send((MessageType)TetrisMessageType.Attack, m);
            _mp.ProgressOutgoing   += m => Send((MessageType)TetrisMessageType.ProgressUpdate, m);
            _mp.DiedOutOutgoing    += m => Send((MessageType)TetrisMessageType.DiedOut, m);
        }

        private void OnLocalLocked(LockResult r)
        {
            _ctx?.Audio?.PlaySfx(r.LinesCleared >= 4 ? "tetris_clear" : "drop");
            if (r.LinesCleared > 0) _ctx?.Haptics?.Light();
        }

        private void Send<T>(MessageType type, T body)
        {
            if (_ctx?.Net == null) return;
            _ctx.Net.Broadcast(type, Json.Serialize(body), reliable: true);
        }

        public void OnPeerMessage(PeerId from, MessageType type, ArraySegment<byte> payload)
        {
            if (_mp == null) return;
            switch ((TetrisMessageType)(byte)type)
            {
                case TetrisMessageType.Attack:
                    _mp.OnAttackReceived(Json.Deserialize<TetrisAttackMessage>(payload));
                    break;
                case TetrisMessageType.ProgressUpdate:
                    _mp.OnProgressReceived(Json.Deserialize<TetrisProgressMessage>(payload));
                    break;
                case TetrisMessageType.DiedOut:
                    _mp.OnDiedOutReceived(Json.Deserialize<TetrisDiedOutMessage>(payload));
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
