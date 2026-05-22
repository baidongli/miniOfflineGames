using UnityEngine;
using UnityEngine.EventSystems;

namespace MiniGames.App.Games
{
    /// <summary>
    /// A UI button that reports whether it's currently pressed, so callers can
    /// implement press-and-hold auto-repeat (used for Tetris move/soft-drop).
    /// Attach alongside an Image (raycast target) so it receives pointer events.
    /// </summary>
    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool IsHeld { get; private set; }

        public void OnPointerDown(PointerEventData eventData) => IsHeld = true;
        public void OnPointerUp(PointerEventData eventData) => IsHeld = false;
        private void OnDisable() => IsHeld = false;
    }
}
