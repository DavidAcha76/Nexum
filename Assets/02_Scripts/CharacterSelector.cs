using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelector : MonoBehaviour
{
    [Header("UI")]
    public Button chooseButton;
    public Image characterImage;         // el Image debajo del botón
    public TextMeshProUGUI infoText;     // opcional, para mostrar nombre/rareza

    private GameDatabase db;
    private List<OwnedCharacter> owned;
    private int currentIndex = 0;

    void Start()
    {
        db = new GameDatabase();
        owned = db.GetOwned();

        Debug.Log($"[CharacterSelector] Total personajes en Owned: {owned.Count}");

        if (chooseButton) chooseButton.onClick.AddListener(ChooseNext);
        RefreshUI();
    }

    void OnDestroy()
    {
        if (chooseButton) chooseButton.onClick.RemoveListener(ChooseNext);
    }

    void ChooseNext()
    {
        if (owned.Count == 0)
        {
            if (infoText) infoText.text = "No tienes personajes";
            Debug.Log("[CharacterSelector] No hay personajes en Owned.");
            return;
        }

        currentIndex = (currentIndex + 1) % owned.Count;
        RefreshUI();
    }

    void RefreshUI()
    {
        if (owned.Count == 0) return;

        var c = owned[currentIndex];

        // ✅ Cargar por Id
        var spr = Resources.Load<Sprite>($"Images/{c.Id}");
        if (spr != null)
        {
            characterImage.sprite = spr;
            characterImage.enabled = true;
        }

        if (infoText != null)
            infoText.text = $"ID {c.Id} (Rareza {c.Rarity}) - Copias: {c.Count}";
    }
}

