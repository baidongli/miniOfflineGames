using System.Text;
using MiniGames.GameModule;
using UnityEngine;

namespace MiniGames.App.Shared.Analytics
{
    /// <summary>
    /// Stand-in analytics that just logs to Unity's console. Replace with
    /// a real provider (GameAnalytics, Firebase, etc.) by swapping the
    /// IAnalytics binding in AppBootstrap.
    /// </summary>
    public sealed class DebugAnalytics : IAnalytics
    {
        public void Track(string eventName, params (string key, object value)[] props)
        {
            if (props == null || props.Length == 0)
            {
                Debug.Log($"[Analytics] {eventName}");
                return;
            }
            var sb = new StringBuilder("[Analytics] ").Append(eventName).Append(" ");
            for (int i = 0; i < props.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(props[i].key).Append('=').Append(props[i].value);
            }
            Debug.Log(sb.ToString());
        }
    }
}
