// UISprintHoldButton.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class UISprintHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public bool IsHeld { get; private set; }
    public void OnPointerDown(PointerEventData _) { IsHeld = true; }
    public void OnPointerUp(PointerEventData _) { IsHeld = false; }
    public void OnPointerExit(PointerEventData _) { IsHeld = false; }
}
