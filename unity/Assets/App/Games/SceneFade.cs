using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniGames.App.Games
{
    /// <summary>
    /// Black fade-in on every scene load, so navigating Hub <-> games feels
    /// smooth instead of a hard cut. Hooks SceneManager.sceneLoaded once at
    /// startup and spawns a short-lived top-most overlay that fades from black
    /// to clear; no changes needed at any LoadScene call site.
    /// </summary>
    public sealed class SceneFade : MonoBehaviour
    {
        private const float Duration = 0.3f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init() => SceneManager.sceneLoaded += (_, __) => Spawn();

        private static void Spawn() => new GameObject("SceneFade").AddComponent<SceneFade>().Build();

        private CanvasGroup _cg;

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000; // above every other overlay
            _cg = gameObject.AddComponent<CanvasGroup>();
            _cg.blocksRaycasts = false; // never eat input
            _cg.interactable = false;

            var imgGo = new GameObject("Black", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)imgGo.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            imgGo.GetComponent<Image>().color = Color.black;

            StartCoroutine(FadeOut());
        }

        private IEnumerator FadeOut()
        {
            float t = 0f;
            while (t < Duration)
            {
                t += Time.unscaledDeltaTime;
                _cg.alpha = 1f - Mathf.Clamp01(t / Duration);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
