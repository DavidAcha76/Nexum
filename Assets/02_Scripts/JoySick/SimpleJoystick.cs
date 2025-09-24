using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform handle;
    public float maxRadius = 60f; // px

    public Vector2 Direction { get; private set; }

    private RectTransform _rect;
    private Vector2 _startPos;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _startPos = handle.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rect, eventData.position, eventData.pressEventCamera, out var local);

        var delta = local - _startPos;
        delta = Vector2.ClampMagnitude(delta, maxRadius);
        handle.anchoredPosition = _startPos + delta;

        Direction = delta / maxRadius; // -1..1
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        handle.anchoredPosition = _startPos;
        Direction = Vector2.zero;
    }
}
