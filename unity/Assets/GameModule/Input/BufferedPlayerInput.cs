using UnityEngine;

namespace MiniGames.GameModule.Input
{
    /// <summary>
    /// IPlayerInput backed by a writable frame buffer. The owner pushes a
    /// PlayerInputFrame each tick; game code reads it as IPlayerInput.
    /// Used by SameDeviceInput dispatcher and NetworkedInput.
    /// </summary>
    public sealed class BufferedPlayerInput : IPlayerInput
    {
        public int PlayerIndex { get; }
        private PlayerInputFrame _current;
        private PlayerInputFrame _prev;

        public BufferedPlayerInput(int playerIndex) { PlayerIndex = playerIndex; }

        public void Write(PlayerInputFrame frame)
        {
            _prev = _current;
            _current = frame;
        }

        public Vector2 Move => _current.Move;
        public Vector2 Aim => _current.Aim;
        public bool ActionA => _current.ActionA;
        public bool ActionADown => _current.ActionA && !_prev.ActionA;
        public bool ActionB => _current.ActionB;
        public bool ActionBDown => _current.ActionB && !_prev.ActionB;
        public Vector2 TouchPosition => _current.TouchPosition;
        public bool TouchActive => _current.TouchActive;
    }
}
