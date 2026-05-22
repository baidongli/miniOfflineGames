using System.Collections;
using UnityEngine;

namespace MiniGames.App.Games
{
    /// <summary>
    /// Minimal UI tween runner. UiTween.Pop(rt) makes a cell appear with a
    /// small ease-out-back scale pop. Runs on a lazily-created runner in the
    /// active scene (destroyed on scene change, which cleanly stops tweens).
    /// Scaling localScale doesn't fight GridLayoutGroup, which only sets
    /// size/position.
    /// </summary>
    public sealed class UiTween : MonoBehaviour
    {
        private static UiTween _runner;

        private static UiTween Runner()
        {
            if (_runner == null)
                _runner = new GameObject("UiTween").AddComponent<UiTween>();
            return _runner;
        }

        public static void Pop(RectTransform rt, float from = 0.5f, float dur = 0.18f, float delay = 0f)
        {
            if (rt == null) return;
            if (delay > 0f) rt.localScale = new Vector3(from, from, 1f); // avoid a full-size flash before the delay
            Runner().StartCoroutine(PopRoutine(rt, from, dur, delay));
        }

        /// <summary>Coin-flip squash on the X axis (1 -> 0 -> 1). Good for flipping discs.</summary>
        public static void Flip(RectTransform rt, float dur = 0.24f)
        {
            if (rt != null) Runner().StartCoroutine(FlipRoutine(rt, dur));
        }

        private static IEnumerator PopRoutine(RectTransform rt, float from, float dur, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            float t = 0f;
            while (t < dur)
            {
                if (rt == null) yield break;
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                float s = Mathf.LerpUnclamped(from, 1f, EaseOutBack(k));
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }

        private static IEnumerator FlipRoutine(RectTransform rt, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                if (rt == null) yield break;
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                float sx = Mathf.Max(0.05f, Mathf.Abs(Mathf.Cos(k * Mathf.PI)));
                rt.localScale = new Vector3(sx, 1f, 1f);
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float p = x - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }
    }
}
