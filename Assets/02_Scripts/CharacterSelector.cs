using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelector : MonoBehaviour
{
    [Header("UI")]
    public Button chooseButton;
    public Image characterImage;
    public TextMeshProUGUI infoText;

    private GameDatabase db;
    private List<OwnedCharacter> owned;
    private int currentIndex = 0;

    void Start()
    {
        db = new GameDatabase();

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

    void OnChooseButtonPressed()
    {
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

        // Guardar el personaje seleccionado en la BD
        var selectedCharacter = owned[currentIndex];
        db.SetSelectedCharacter(selectedCharacter.CharacterId);

        Debug.Log($"[CharacterSelector] Personaje seleccionado guardado: {selectedCharacter.Name}");

        RefreshUI();
    }

    void RefreshOwnedList()
    {
        owned = db.GetOwned();
        Debug.Log($"[CharacterSelector] Lista recargada. Total personajes: {owned.Count}");

        if (currentIndex >= owned.Count)
            currentIndex = 0;
    }

    void RefreshUI()
    {
        if (owned.Count == 0) return;

        var c = owned[currentIndex];
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
