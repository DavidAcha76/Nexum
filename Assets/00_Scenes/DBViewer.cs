using UnityEngine;
using System.Text;
using System.Collections.Generic;

public class DBViewer : MonoBehaviour
{
    private GameDatabase db;

    void Start()
    {
        db = new GameDatabase();

        Debug.Log("======= VISTA COMPLETA DE LA BASE DE DATOS =======");
        ShowPlayers();
        ShowCharacters();
        ShowOwnedCharacters();
        Debug.Log("==================================================");
        List<Character> allCharacters = db.GetCharacters();

        foreach (var c in allCharacters)
        {
            Debug.Log($"ID: {c.Id} | Nombre: {c.Name} | Rareza: {c.Rarity}");
        }

    }

    // Mostrar tabla Player
    void ShowPlayers()
    {
        int gold = db.GetGold();
        Debug.Log($"[PLAYER] Oro actual: {gold}");
    }

    // Mostrar tabla Character
    void ShowCharacters()
    {
        List<Character> characters = db.GetCharacters();

        if (characters.Count == 0)
        {
            Debug.Log("[CHARACTERS] No hay personajes en el catálogo.");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[CHARACTERS] Catálogo actual:");
        sb.AppendLine("ID | Name | Rarity");

        foreach (var c in characters)
        {
            sb.AppendLine($"{c.Id} | {c.Name} | {c.Rarity}");
        }

        Debug.Log(sb.ToString());
    }

    // Mostrar tabla OwnedCharacter
    void ShowOwnedCharacters()
    {
        List<OwnedCharacter> owned = db.GetOwned();

        if (owned.Count == 0)
        {
            Debug.Log("[OWNED CHARACTERS] El jugador aún no posee personajes.");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[OWNED CHARACTERS] Inventario del jugador:");
        sb.AppendLine("ID | Name | Rarity | Count");

        foreach (var o in owned)
        {
            sb.AppendLine($"{o.Id} | {o.Name} | {o.Rarity} | {o.Count}");
        }

        Debug.Log(sb.ToString());
    }
}
