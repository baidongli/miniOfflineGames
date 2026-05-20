using System;
using System.Threading.Tasks;
using MiniGames.GameModule;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Transport;

namespace MiniGames.Networking.Session
{
    /// <summary>
    /// Glue between a started IGameModule and its RoomManager. Subscribes to
    /// game-message events and routes them to the module, and owns the
    /// module's lifecycle for the duration of a play session.
    /// </summary>
    public sealed class GameSession : IDisposable
    {
        public IGameModule Module { get; }
        public GameContext Context { get; }
        public RoomManager Room { get; }

        public GameSession(IGameModule module, GameContext ctx, RoomManager room)
        {
            Module = module;
            Context = ctx;
            Room = room;
            if (Room != null) Room.GameMessageReceived += OnGameMessage;
        }

        public Task LoadAsync() => Module.LoadAsync(Context);

        public void StartSolo() => Module.StartSolo(Context);

        public void StartMultiplayer(RoomSnapshot snapshot, int seed, bool isHost)
            => Module.StartMultiplayer(Context, snapshot, seed, isHost);

        public Task UnloadAsync() => Module.UnloadAsync();

        public void Pause() => Module.Pause();
        public void Resume() => Module.Resume();

        private void OnGameMessage(PeerId from, MessageType type, ArraySegment<byte> body)
        {
            Module.OnPeerMessage(from, type, body);
        }

        public void Dispose()
        {
            if (Room != null) Room.GameMessageReceived -= OnGameMessage;
        }
    }
}
