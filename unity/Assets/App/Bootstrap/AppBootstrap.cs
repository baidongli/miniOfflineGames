using System;
using MiniGames.App.Shared.Analytics;
using MiniGames.App.Shared.Audio;
using MiniGames.App.Shared.Energy;
using MiniGames.App.Shared.Haptics;
using MiniGames.App.Shared.SaveSystem;
using MiniGames.GameModule;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Session;
using MiniGames.Networking.Transport;
using UnityEngine;

namespace MiniGames.App.Bootstrap
{
    /// <summary>
    /// Single entry-point MonoBehaviour, dropped on a GameObject in the boot
    /// scene. Wires every shared service + transport singleton, then exposes
    /// them through AppBootstrap.Services. Hub UI / lobby code pulls from
    /// Services to construct GameContexts.
    /// </summary>
    public sealed class AppBootstrap : MonoBehaviour
    {
        public static AppServices Services { get; private set; }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            Application.runInBackground = false;

            var transport = NearbyConnectionsTransport.CreateAndAttach();
            var serializer = new MessagePackMessageSerializer();
            var save = JsonSaveStore.ForPlayer();
            var analytics = new DebugAnalytics();
            var haptics = ChoosePlatformHaptics();
            var audio = gameObject.AddComponent<AudioBus>();
            var energy = BuildEnergyTimer(save);

            Services = new AppServices
            {
                Transport = transport,
                Serializer = serializer,
                Save = save,
                Analytics = analytics,
                Haptics = haptics,
                Audio = audio,
                Energy = energy,
                LocalPlayerId = LoadOrCreatePlayerId(),
                LocalDisplayName = LoadOrCreateDisplayName()
            };

            Debug.Log($"[Boot] {GameRegistry.All.Count} games registered. " +
                      $"playerId={Services.LocalPlayerId} name={Services.LocalDisplayName}");
        }

        /// <summary>Build a GameContext for a play session, multiplayer-aware.</summary>
        public static GameContext BuildContext(IGameSendChannel net)
        {
            var s = Services;
            return new GameContext(s.Audio, s.Save, s.Analytics, s.Haptics, net, s.LocalPlayerId);
        }

        private static IHaptics ChoosePlatformHaptics()
        {
#if UNITY_ANDROID || UNITY_IOS
            return new UnityHaptics();
#else
            return new NullHaptics();
#endif
        }

        private static EnergyTimer BuildEnergyTimer(ISaveStore save)
        {
            const string Key = "energy";
            const int Max = 5;
            var refill = TimeSpan.FromMinutes(15);
            EnergyState state = save.TryLoad<EnergyState>(Key, out var loaded) && loaded != null
                ? loaded
                : EnergyState.Fresh(Max);
            var timer = new EnergyTimer(Max, refill, state);
            return timer;
        }

        private static string LoadOrCreatePlayerId()
        {
            const string Key = "playerId";
            var id = PlayerPrefs.GetString(Key, null);
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(Key, id);
            }
            return id;
        }

        private static string LoadOrCreateDisplayName()
        {
            const string Key = "displayName";
            var name = PlayerPrefs.GetString(Key, null);
            if (string.IsNullOrEmpty(name))
            {
                name = $"Player{UnityEngine.Random.Range(1000, 9999)}";
                PlayerPrefs.SetString(Key, name);
            }
            return name;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) PersistEnergy();
        }

        private void OnApplicationQuit() => PersistEnergy();

        private static void PersistEnergy()
        {
            if (Services?.Energy == null || Services.Save == null) return;
            Services.Save.Save("energy", Services.Energy.State);
        }
    }

    public sealed class AppServices
    {
        public IGameTransport Transport;
        public IMessageSerializer Serializer;
        public ISaveStore Save;
        public IAnalytics Analytics;
        public IHaptics Haptics;
        public IAudio Audio;
        public EnergyTimer Energy;
        public string LocalPlayerId;
        public string LocalDisplayName;
    }
}
