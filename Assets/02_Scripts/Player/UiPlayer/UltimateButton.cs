using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class UltimateButton : MonoBehaviour, IPointerDownHandler
{
    private PlayerController player;
    public Image chargeFill; // arrástrale en el Inspector el Fill de la barra

    public Button button;

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
        if (button == null) button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnUltimatePressed);
    }

    void Update()
    {
        if (player == null) return;

        // actualizar barra
        if (chargeFill != null)
            chargeFill.fillAmount = player.Ultimate01;

        // activar botón solo si está lista
        if (button != null)
            button.interactable = player.CanUseUltimate;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnUltimatePressed();
    }

    private void OnUltimatePressed()
    {
        if (player != null)
            player.DoUltimate();
    }
}
