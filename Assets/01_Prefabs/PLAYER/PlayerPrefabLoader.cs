using UnityEngine;

public static class PlayerPrefabLoader
{
    private const string PLAYER_PREFABS_PATH = "";

    /// <summary>
    /// Obtiene el prefab del personaje guardado en la base de datos.
    /// </summary>
    public static GameObject GetPlayerPrefab()
    {
        GameDatabase db = new GameDatabase();
        int selectedId = db.GetSelectedCharacterId();

        if (selectedId == 0)
        {
            Debug.LogWarning("[PlayerPrefabLoader] No hay personaje seleccionado. Usando default.");
            return null;
        }

        Character character = db.GetCharacterById(selectedId);
        if (character == null)
        {
            Debug.LogError($"[PlayerPrefabLoader] No existe Character con ID {selectedId}");
            return null;
        }

        string prefabPath = PLAYER_PREFABS_PATH + character.Name;
        GameObject prefab = Resources.Load<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"[PlayerPrefabLoader] Prefab no encontrado en Resources/{prefabPath}");
            return null;
        }

        return prefab;
    }
}
