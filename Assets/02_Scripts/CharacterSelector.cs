using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelector : MonoBehaviour
{
    [Header("UI")]
    public Button chooseButton;
    public Image characterImage;         // Imagen que mostrará al personaje
    public TextMeshProUGUI infoText;     // Opcional: para mostrar información extra

    private GameDatabase db;
    private List<OwnedCharacter> owned;
    private int currentIndex = 0;

    void Start()
    {
        db = new GameDatabase();

        // Cargar lista inicial
        RefreshOwnedList();

        if (chooseButton)
            chooseButton.onClick.AddListener(OnChooseButtonPressed);

        RefreshUI();
    }

    void OnDestroy()
    {
        if (chooseButton)
            chooseButton.onClick.RemoveListener(OnChooseButtonPressed);
    }

    /// <summary>
    /// Método llamado al pulsar el botón
    /// Actualiza la lista y cambia al siguiente personaje
    /// </summary>
    void OnChooseButtonPressed()
    {
        // Recarga la lista desde la base de datos
        RefreshOwnedList();

        if (owned.Count == 0)
        {
            if (infoText)
                infoText.text = "No tienes personajes";
            Debug.Log("[CharacterSelector] No hay personajes en Owned.");
            return;
        }

        // Avanza al siguiente personaje
        currentIndex = (currentIndex + 1) % owned.Count;
        RefreshUI();
    }

    /// <summary>
    /// Recarga la lista de personajes que el jugador posee
    /// </summary>
    void RefreshOwnedList()
    {
        owned = db.GetOwned();
        Debug.Log($"[CharacterSelector] Lista recargada. Total personajes: {owned.Count}");

        // Ajustar índice si se borraron personajes y el index actual quedó fuera de rango
        if (currentIndex >= owned.Count)
            currentIndex = 0;
    }

    /// <summary>
    /// Actualiza la UI con la información del personaje actual
    /// </summary>
    void RefreshUI()
    {
        if (owned.Count == 0) return;

        var c = owned[currentIndex];

        // Cargar sprite desde Resources
        var spr = Resources.Load<Sprite>($"Images/{c.CharacterId}");
        if (spr != null)
        {
            characterImage.sprite = spr;
            characterImage.enabled = true;
        }
        else
        {
            Debug.LogWarning($"[CharacterSelector] Sprite no encontrado para CharacterId: {c.CharacterId}");
        }

        if (infoText != null)
            infoText.text = $"ID {c.Id} | Rareza {c.Rarity} | Copias: {c.Count}";
    }
}
