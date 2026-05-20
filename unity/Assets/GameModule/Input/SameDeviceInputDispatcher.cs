using System.Collections.Generic;
using UnityEngine;

namespace MiniGames.GameModule.Input
{
    /// <summary>
    /// Splits the screen into N regions and routes touches to per-player
    /// BufferedPlayerInput instances. Each touch belongs to exactly one
    /// player, decided by the region containing the touch's STARTING point —
    /// drags don't lose ownership mid-gesture.
    ///
    /// MonoBehaviour-free: caller is expected to feed it Update() with a
    /// list of active touches each frame.
    /// </summary>
    public sealed class SameDeviceInputDispatcher
    {
        public readonly ScreenRegion[] Regions;
        public readonly BufferedPlayerInput[] Inputs;
        private readonly Vector2 _screenSize;

        // Stable mapping from touch id -> player index for the duration of a touch.
        private readonly Dictionary<int, int> _touchOwner = new Dictionary<int, int>();

        public SameDeviceInputDispatcher(int playerCount, Vector2 screenSize)
        {
            Regions = ScreenLayouts.For(playerCount);
            Inputs = new BufferedPlayerInput[playerCount];
            for (int i = 0; i < playerCount; i++) Inputs[i] = new BufferedPlayerInput(i);
            _screenSize = screenSize;
        }

        public struct TouchSample
        {
            public int Id;
            public Vector2 Position;
            public bool Began;
            public bool Ended;
        }

        public void Tick(IReadOnlyList<TouchSample> touches)
        {
            var frames = new PlayerInputFrame[Inputs.Length];

            for (int i = 0; i < touches.Count; i++)
            {
                var t = touches[i];

                int owner;
                if (t.Began || !_touchOwner.TryGetValue(t.Id, out owner))
                {
                    owner = OwnerForPoint(t.Position);
                    if (owner < 0) continue;
                    _touchOwner[t.Id] = owner;
                }

                if (owner >= 0 && owner < frames.Length)
                {
                    frames[owner].TouchActive = true;
                    frames[owner].TouchPosition = t.Position;
                    frames[owner].ActionA = true;
                }

                if (t.Ended) _touchOwner.Remove(t.Id);
            }

            for (int i = 0; i < Inputs.Length; i++) Inputs[i].Write(frames[i]);
        }

        private int OwnerForPoint(Vector2 screenPx)
        {
            for (int i = 0; i < Regions.Length; i++)
                if (Regions[i].ContainsScreen(screenPx, _screenSize)) return i;
            return -1;
        }
    }
}
