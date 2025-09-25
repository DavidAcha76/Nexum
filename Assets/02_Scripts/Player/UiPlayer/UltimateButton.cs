﻿
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
        if (button == null) button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(OnUltimatePressed);
    }

    void Update()
    {
        // 👇 Buscar al player dinámicamente como en DashButton
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>();
            if (player != null)
                Debug.Log("[UltimateButton] Player encontrado en runtime.");
        }

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
