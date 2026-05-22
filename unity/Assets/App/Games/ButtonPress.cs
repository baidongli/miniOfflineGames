using UnityEngine;
using UnityEngine.EventSystems;

namespace MiniGames.App.Games
{
    /// <summary>
    /// Tactile press feedback: the button shrinks slightly while held and
    /// springs back on release. Added to every button by Art.StyleButtons.
    /// </summary>
    public sealed class ButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private const float Pressed = 0.94f;
        private Vector3 _base = Vector3.one;

        private void Awake() => _base = transform.localScale;

        public void OnPointerDown(PointerEventData e) => transform.localScale = _base * Pressed;
        public void OnPointerUp(PointerEventData e) => transform.localScale = _base;
        private void OnDisable() => transform.localScale = _base;
    }
}
