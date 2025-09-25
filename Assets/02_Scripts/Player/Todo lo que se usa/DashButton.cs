

using UnityEngine;
using UnityEngine.EventSystems;

public class DashButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private PlayerController player;
    private bool isPressed = false; // 👉 estado del botón

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
        if (player == null)
            Debug.LogWarning("[DashButton] No se encontró PlayerController en escena.");
    }
    void Update()
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>();
            if (player != null)
                Debug.Log("[DashButton] Player encontrado en runtime.");
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        if (player != null)
            player.DoDash();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    // 👉 Método extra por si prefieres asignarlo en el OnClick() del botón en el Inspector
    public void OnDashPressed()
    {
        if (player != null)
            player.DoDash();
    }
}
