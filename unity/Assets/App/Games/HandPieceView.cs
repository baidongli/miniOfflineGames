using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MiniGames.App.Games
{
    /// <summary>
    /// A draggable hand slot for Color Blocks. Forwards begin/drag/end with its
    /// hand index and the pointer's screen position to the scene controller,
    /// which does board hit-testing and placement.
    /// </summary>
    public sealed class HandPieceView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int HandIndex;
        public Action<int> BeginDrag;
        public Action<int, Vector2> Drag;
        public Action<int, Vector2> EndDrag;

        public void OnBeginDrag(PointerEventData e) => BeginDrag?.Invoke(HandIndex);
        public void OnDrag(PointerEventData e) => Drag?.Invoke(HandIndex, e.position);
        public void OnEndDrag(PointerEventData e) => EndDrag?.Invoke(HandIndex, e.position);
    }
}
