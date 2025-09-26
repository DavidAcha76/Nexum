using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// ===============================================================
///  CATÁLOGO: mapea CharacterId → Prefab (sin Resources para prefabs)
///  Crea el asset: Create → Game → Player Prefab Catalog
/// ===============================================================
[CreateAssetMenu(menuName = "Game/Player Prefab Catalog", fileName = "PlayerPrefabCatalog")]
public class PlayerPrefabCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public int characterId;     // 1..7 (Joshu, Kobeni, etc.)
        public GameObject prefab;   // Prefab del player
    }

    public List<Entry> entries = new List<Entry>();

    public GameObject GetById(int id)
    {
        var e = entries.FirstOrDefault(x => x.characterId == id);
        return e != null ? e.prefab : null;
    }
}