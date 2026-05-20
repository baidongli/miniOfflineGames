using System;
using System.Threading.Tasks;
using MiniGames.GameModule;
using MiniGames.Games.ColorBlocks.Logic;
using MiniGames.Games.ColorBlocks.Multiplayer;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Transport;
using UnityEngine;

namespace MiniGames.Games.ColorBlocks
{
    public sealed class ColorBlocksModule : IGameModule
    {
        public string Id => "color_blocks";
        public string DisplayName => "Color Blocks";
        public GameCapabilities Capabilities =>
            GameCapabilities.Solo | GameCapabilities.Multiplayer | GameCapabilities.SameDevice;
        public int MinPlayers => 2;
        public int MaxPlayers => 4;

        private GameContext _ctx;
        private ColorBlocksGame _solo;
        private MultiplayerColorBlocks _mp;

        public ColorBlocksGame SoloGame => _solo;
        public MultiplayerColorBlocks MultiplayerGame => _mp;

        public Task LoadAsync(GameContext ctx) => Task.CompletedTask;

        public void StartSolo(GameContext ctx)
        {
            _ctx = ctx;
            _solo = new ColorBlocksGame(seed: UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            _solo.Placed += OnLocalPlaced;
            _solo.GameOver += () => ctx.Analytics?.Track("color_blocks_game_over",
                ("score", _solo.Score), ("mode", "solo"));
        }

        public void StartMultiplayer(GameContext ctx, RoomSnapshot room, int seed, bool isHost)
        {
            _ctx = ctx;
            _mp = new MultiplayerColorBlocks(ctx.LocalPlayerId, seed);
            _mp.Local.Placed += OnLocalPlaced;
            _mp.AttackOutgoing += msg => Send((MessageType)CBMessageType.Attack, msg);
            _mp.ProgressOutgoing += msg => Send((MessageType)CBMessageType.ProgressUpdate, msg);
            _mp.DiedOutOutgoing += msg => Send((MessageType)CBMessageType.DiedOut, msg);
        }

        private void OnLocalPlaced(PlaceResult result, int scoreDelta)
        {
            _ctx?.Audio?.PlaySfx("place");
            if (result.TotalLinesCleared > 0)
            {
                _ctx?.Audio?.PlaySfx(result.TotalLinesCleared >= 2 ? "clear_combo" : "clear");
                _ctx?.Haptics?.Light();
            }
        }

        private void Send<T>(MessageType type, T body)
        {
            if (_ctx?.Net == null) return;
            var bytes = Json.Serialize(body);
            _ctx.Net.Broadcast(type, bytes, reliable: true);
        }

        public void OnPeerMessage(PeerId from, MessageType type, ArraySegment<byte> payload)
        {
            if (_mp == null) return;
            var cb = (CBMessageType)(byte)type;
            switch (cb)
            {
                case CBMessageType.Attack:
                    var atk = Json.Deserialize<AttackMessage>(payload);
                    _mp.OnAttackReceived(atk);
                    break;
                case CBMessageType.ProgressUpdate:
                    var prog = Json.Deserialize<ProgressMessage>(payload);
                    _mp.OnProgressReceived(prog);
                    break;
                case CBMessageType.DiedOut:
                    var died = Json.Deserialize<DiedOutMessage>(payload);
                    _mp.OnDiedOutReceived(died);
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
