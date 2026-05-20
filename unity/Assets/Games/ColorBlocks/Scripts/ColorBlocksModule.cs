using System;
using System.Threading.Tasks;
using MiniGames.GameModule;
using MiniGames.Games.ColorBlocks.Logic;
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
        public int MinPlayers => 1;
        public int MaxPlayers => 4;

        private ColorBlocksGame _session;
        private GameContext _ctx;

        public Task LoadAsync(GameContext ctx) => Task.CompletedTask;

        public void StartSolo(GameContext ctx)
        {
            _ctx = ctx;
            _session = NewSession(seed: UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            Debug.Log($"[ColorBlocks] solo session started, first hand: " +
                      string.Join(", ", System.Array.ConvertAll(_session.Hand, p => p.Id)));
        }

        public void StartMultiplayer(GameContext ctx, RoomSnapshot room, int seed, bool isHost)
        {
            _ctx = ctx;
            _session = NewSession(seed);
            Debug.Log($"[ColorBlocks] multi session started host={isHost} seed={seed} players={room.Players.Count}");
        }

        private ColorBlocksGame NewSession(int seed)
        {
            var g = new ColorBlocksGame(seed);
            g.Placed += OnPlaced;
            g.GameOver += OnGameOver;
            return g;
        }

        private void OnPlaced(PlaceResult result, int scoreDelta)
        {
            _ctx?.Audio?.PlaySfx("place");
            if (result.TotalLinesCleared > 0)
            {
                _ctx?.Audio?.PlaySfx(result.TotalLinesCleared >= 2 ? "clear_combo" : "clear");
                _ctx?.Haptics?.Light();
            }
        }

        private void OnGameOver()
        {
            _ctx?.Audio?.PlaySfx("game_over");
            _ctx?.Analytics?.Track("color_blocks_game_over", ("score", _session.Score));
        }

        public void Pause() { }
        public void Resume() { }

        public Task UnloadAsync()
        {
            _session = null;
            _ctx = null;
            return Task.CompletedTask;
        }

        public void OnPeerMessage(PeerId from, MessageType type, ArraySegment<byte> payload) { }
        public void OnPeerJoined(PeerId peer) { }
        public void OnPeerLeft(PeerId peer) { }
    }
}
