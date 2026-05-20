using UnityEngine;

namespace MiniGames.App.Bootstrap
{
    /// <summary>
    /// Single entry-point MonoBehaviour, dropped on a GameObject in the boot scene.
    /// Wires shared services, builds the GameContext factory, and hands off to the Hub.
    /// Concrete service implementations come in later milestones.
    /// </summary>
    public sealed class AppBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            Debug.Log($"[Boot] {GameRegistry.All.Count} games registered.");
            foreach (var g in GameRegistry.All)
                Debug.Log($"[Boot]  - {g.Id}  ({g.DisplayName})  caps={g.Capabilities}");
        }
    }
}
