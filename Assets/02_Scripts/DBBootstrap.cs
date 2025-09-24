using System.Collections.Generic;
using UnityEngine;

public class DBBootstrap : MonoBehaviour
{
    [SerializeField] private GachaSystem gacha;   // arrástralo desde la escena

    [Header("Seed inicial (Name, Rarity)")]
    public List<SeedItem> initialCharacters = new List<SeedItem>
    {
        new SeedItem { name = "1", rarity = 3 },
        new SeedItem { name = "2", rarity = 4 },
        new SeedItem { name = "3", rarity = 3 },
        new SeedItem { name = "4", rarity = 4 },
        new SeedItem { name = "5", rarity = 4 },
        new SeedItem { name = "6", rarity = 5 },
        new SeedItem { name = "7", rarity = 4 },
    };

    private GameDatabase db;

    void Awake()
    {
        if (!gacha) gacha = FindObjectOfType<GachaSystem>();
    }

    void Start()
    {
        db = new GameDatabase();

        // Seed si la DB está vacía
        if (db.GetCharacters().Count == 0)
        {
            foreach (var s in initialCharacters)
                db.AddCharacterIfMissing(s.name, Mathf.Clamp(s.rarity, 3, 5));
        }

        // Oro de prueba básico
        if (db.GetGold() < 25) db.AddGold(100);


        Debug.Log("Gold: " + db.GetGold() + " | Catálogo: " + db.GetCharacters().Count);
    }
}

[System.Serializable]
public struct SeedItem
{
    public string name;
    public int rarity;
}
