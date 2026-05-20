using UnityEngine;

namespace MiniGames.GameModule.Input
{
    /// <summary>
    /// Plain-data snapshot for one frame. Networked input ships these.
    /// Backing struct for SameDeviceInput / NetworkedInput / etc.
    /// </summary>
    public struct PlayerInputFrame
    {
        public Vector2 Move;
        public Vector2 Aim;
        public bool ActionA;
        public bool ActionADown;
        public bool ActionB;
        public bool ActionBDown;
        public Vector2 TouchPosition;
        public bool TouchActive;
    }
}
