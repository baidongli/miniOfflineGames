using MiniGames.Networking.Protocol;
using MiniGames.Networking.Transport;
using UnityEngine;

namespace MiniGames.App.Bootstrap
{
    /// <summary>
    /// Single entry-point MonoBehaviour, dropped on a GameObject in the boot scene.
    /// Wires shared services + transport singleton, then hands off to the Hub.
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

            Services = new AppServices
            {
                Transport = transport,
                Serializer = serializer,
                LocalPlayerId = LoadOrCreatePlayerId(),
                LocalDisplayName = LoadOrCreateDisplayName()
            };

            Debug.Log($"[Boot] {GameRegistry.All.Count} games registered. " +
                      $"playerId={Services.LocalPlayerId} name={Services.LocalDisplayName}");
        }

        private static string LoadOrCreatePlayerId()
        {
            const string Key = "playerId";
            var id = PlayerPrefs.GetString(Key, null);
            if (string.IsNullOrEmpty(id))
            {
                id = System.Guid.NewGuid().ToString("N");
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
                name = $"Player{Random.Range(1000, 9999)}";
                PlayerPrefs.SetString(Key, name);
            }
            return name;
        }
    }

    public sealed class AppServices
    {
        public IGameTransport Transport;
        public IMessageSerializer Serializer;
        public string LocalPlayerId;
        public string LocalDisplayName;
    }
}
