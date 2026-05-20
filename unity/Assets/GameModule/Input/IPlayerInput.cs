using UnityEngine;

namespace MiniGames.GameModule.Input
{
    /// <summary>
    /// One player's input view. Solo, same-device, and networked play all
    /// provide this interface; game code never knows where the values come
    /// from. Read once per frame.
    /// </summary>
    public interface IPlayerInput
    {
        int PlayerIndex { get; }     // 0..3
        Vector2 Move { get; }        // -1..+1 on each axis
        Vector2 Aim { get; }         // analog stick / direction
        bool ActionA { get; }        // primary action button (held)
        bool ActionADown { get; }    // pressed this frame
        bool ActionB { get; }
        bool ActionBDown { get; }
        Vector2 TouchPosition { get; }   // screen px, or zero if no touch
        bool TouchActive { get; }
    }
}
