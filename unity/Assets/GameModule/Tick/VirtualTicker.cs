using System;

namespace MiniGames.GameModule.Tick
{
    /// <summary>
    /// Manually-driven ticker for tests. Use Advance(seconds) or
    /// AdvanceTicks(n) to fire OnTick deterministically.
    /// </summary>
    public sealed class VirtualTicker : ITicker
    {
        public bool IsRunning { get; private set; }
        public float Hz { get; private set; }
        public int TotalTicksFired { get; private set; }

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

        public int Advance(float seconds)
        {
            if (!IsRunning || seconds <= 0f) return 0;
            _accum += seconds;
            int fired = 0;
            while (_accum >= _period)
            {
                _accum -= _period;
                _onTick?.Invoke();
                fired++;
                TotalTicksFired++;
                if (!IsRunning) break; // onTick may stop us
            }
            return fired;
        }

        public void AdvanceTicks(int n)
        {
            if (!IsRunning || n <= 0) return;
            for (int i = 0; i < n && IsRunning; i++)
            {
                _onTick?.Invoke();
                TotalTicksFired++;
            }
        }
    }
}
