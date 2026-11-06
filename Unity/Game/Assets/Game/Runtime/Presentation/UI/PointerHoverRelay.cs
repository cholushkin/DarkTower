using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game
{
    /// Forwards pointer enter/exit on the GameObject it's attached to as plain C# events, so a parent
    /// View can react to hover over one specific child widget without a prefab-level EventTrigger.
    /// Intentionally generic (no Storehouse/feature-specific logic) so any screen can reuse it.
    public sealed class PointerHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action OnEnter;
        public event Action OnExit;

        public void OnPointerEnter(PointerEventData eventData) => OnEnter?.Invoke();
        public void OnPointerExit(PointerEventData eventData) => OnExit?.Invoke();
    }
}
