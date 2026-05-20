using System;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Transport;

namespace MiniGames.GameModule
{
    /// <summary>
    /// What a game can ask the Hub for. Passed in at Load/Start time so games
    /// don't reach into App globals.
    /// </summary>
    public sealed class GameContext
    {
        // Shared services - implementations injected by the Hub.
        public IAudio Audio { get; }
        public ISaveStore Save { get; }
        public IAnalytics Analytics { get; }
        public IHaptics Haptics { get; }

        // Multiplayer: null for solo runs.
        public IGameSendChannel Net { get; }

        // Local player identity (matches RoomManager.LocalPlayerId for multiplayer).
        public string LocalPlayerId { get; }

        public GameContext(IAudio audio, ISaveStore save, IAnalytics analytics, IHaptics haptics,
            IGameSendChannel net, string localPlayerId)
        {
            Audio = audio;
            Save = save;
            Analytics = analytics;
            Haptics = haptics;
            Net = net;
            LocalPlayerId = localPlayerId;
        }
    }

    public interface IAudio
    {
        void PlaySfx(string id, float volume = 1f);
        void PlayBgm(string id, float fadeSeconds = 0.5f);
        void StopBgm(float fadeSeconds = 0.5f);
    }

    public interface ISaveStore
    {
        bool TryLoad<T>(string key, out T value) where T : class;
        void Save<T>(string key, T value) where T : class;
        void Delete(string key);
    }

    public interface IAnalytics
    {
        void Track(string eventName, params (string key, object value)[] props);
    }

    public interface IHaptics
    {
        void Light();
        void Medium();
        void Heavy();
    }

    /// <summary>
    /// Game-side view of the network. Games send opaque per-game payloads;
    /// they're framed by the protocol layer as InputCommand / StateSnapshot /
    /// GameEvent or game-specific 0x80+ subtypes.
    /// </summary>
    public interface IGameSendChannel
    {
        bool IsHost { get; }
        void SendToHost(MessageType type, byte[] payload, bool reliable);
        void Broadcast(MessageType type, byte[] payload, bool reliable);
        void SendTo(PeerId peer, MessageType type, byte[] payload, bool reliable);
    }
}
