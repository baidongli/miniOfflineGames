using System;
using MiniGames.GameModule.Tick;
using UnityEngine;

namespace MiniGames.App.Bootstrap
{
    /// <summary>
    /// Production ITicker driven by Unity Update + Time.unscaledDeltaTime.
    /// Add as a MonoBehaviour component on the same GameObject that owns
    /// the gameplay scene (or DontDestroyOnLoad to span scenes).
    /// </summary>
    public sealed class UnityTicker : MonoBehaviour, ITicker
    {
        public bool IsRunning { get; private set; }
        public float Hz { get; private set; }

        private Action _onTick;
        private float _accum;
        private float _period;

        public void Begin(float hz, Action onTick)
        {
            if (hz <= 0f) throw new ArgumentOutOfRangeException(nameof(hz));
            Hz = hz;
            _period = 1f / hz;
            _onTick = onTick;
            _accum = 0f;
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
            _onTick = null;
        }

        private void Update()
        {
            if (!IsRunning || _onTick == null) return;
            _accum += Time.unscaledDeltaTime;
            // Cap at 4 ticks per frame to avoid death spirals on big stalls.
            int safety = 4;
            while (_accum >= _period && safety-- > 0)
            {
                _accum -= _period;
                _onTick();
                if (!IsRunning) break;
            }
        }
    }
}
