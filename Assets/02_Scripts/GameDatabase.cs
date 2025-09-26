using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using SQLite;

public class GameDatabase
{
    private SQLiteConnection db;

    public GameDatabase(string dbName = "game.db")
    {
        string path = Path.Combine(Application.persistentDataPath, dbName);
        db = new SQLiteConnection(path);

        db.CreateTable<Player>();
        db.CreateTable<Character>();
        db.CreateTable<OwnedCharacter>();

        // Asegurar que exista al menos un jugador (Id = 1)
        if (db.Table<Player>().Count() == 0)
        {
            var newPlayer = new Player
            {
                Id = 1,
                Gold = 0,
                SelectedCharacterId = 0 // sin selección aún
            };
            db.Insert(newPlayer);
            Debug.Log($"[DB] Jugador creado con {newPlayer.Gold} de oro");
        }

        // Si no hay selección y el jugador posee alguno, seleccionar el primero
        SelectFirstOwnedIfEmpty();
    }

    // ===================== ORO =====================
    public int GetGold()
    {
        return db.Table<Player>().FirstOrDefault()?.Gold ?? 0;
    }

    public void AddGold(int amount)
    {
        var player = db.Table<Player>().First();
        player.Gold += amount;
        db.Update(player);
    }

    public bool SpendGoldIfPossible(int cost)
    {
        var player = db.Table<Player>().First();
        if (player.Gold < cost) return false;
        player.Gold -= cost;
        db.Update(player);
        return true;
    }

    // ===================== PERSONAJES BASE =====================
    public void AddCharacter(string name, int rarity)
    {
        var c = new Character { Name = name, Rarity = rarity };
        db.Insert(c);
    }

    public List<Character> GetCharacters()
    {
        return db.Table<Character>().ToList();
    }

    public List<Character> GetCharactersByRarity(int rarity)
    {
        return db.Table<Character>().Where(c => c.Rarity == rarity).ToList();
    }

    public void AddCharacterIfMissing(string name, int rarity)
    {
        var existing = db.Table<Character>().FirstOrDefault(c => c.Name == name);
        if (existing == null)
        {
            db.Insert(new Character { Name = name, Rarity = rarity });
        }
        else if (existing.Rarity != rarity)
        {
            existing.Rarity = rarity;
            db.Update(existing);
        }
    }

    // ===================== INVENTARIO (OWNED) =====================
    public void IncrementOwned(int characterId, string characterName, int rarity, int amount = 1)
    {
        var existing = db.Table<OwnedCharacter>().FirstOrDefault(o => o.CharacterId == characterId);
        if (existing == null)
        {
            db.Insert(new OwnedCharacter
            {
                CharacterId = characterId,
                Name = characterName,
                Rarity = rarity,
                Count = amount
            });
        }
        else
        {
            existing.Count += amount;
            if (existing.Count < 0) existing.Count = 0;
            db.Update(existing);
        }

        // Si no había selección, o la selección actual ya no existe,
        // aseguramos que haya una selección válida.
        SelectFirstOwnedIfEmpty();
    }

    public List<OwnedCharacter> GetOwned()
    {
        return db.Table<OwnedCharacter>().ToList();
    }

    // ===================== SELECCIÓN DE PERSONAJE =====================
    /// <summary> Guarda en Player.SelectedCharacterId el CharacterId indicado. </summary>
    public void SaveSelectedCharacter(int characterId)
    {
        var player = db.Table<Player>().First();
        // Validar que lo posee
        bool owns = db.Table<OwnedCharacter>().Any(o => o.CharacterId == characterId && o.Count > 0);
        if (!owns)
        {
            Debug.LogWarning($"[DB] SaveSelectedCharacter: el jugador no posee CharacterId {characterId}");
            return;
        }

        player.SelectedCharacterId = characterId;
        db.Update(player);
        Debug.Log($"[DB] Selección guardada: CharacterId={characterId}");
    }

    /// <summary> Devuelve el CharacterId actualmente seleccionado, o 0 si no hay. </summary>
    public int GetSelectedCharacterId()
    {
        return db.Table<Player>().FirstOrDefault()?.SelectedCharacterId ?? 0;
    }

    /// <summary> Devuelve el OwnedCharacter correspondiente al seleccionado, o null. </summary>
    public OwnedCharacter GetSelectedOwned()
    {
        int sel = GetSelectedCharacterId();
        if (sel <= 0) return null;
        return db.Table<OwnedCharacter>().FirstOrDefault(o => o.CharacterId == sel && o.Count > 0);
    }

    /// <summary>
    /// Si no hay selección o la selección actual ya no es válida,
    /// selecciona el primer Owned disponible (ordenado por CharacterId).
    /// </summary>
    public void SelectFirstOwnedIfEmpty()
    {
        var player = db.Table<Player>().First();

        bool selectionValid = player.SelectedCharacterId > 0 &&
                              db.Table<OwnedCharacter>()
                                .Any(o => o.CharacterId == player.SelectedCharacterId && o.Count > 0);

        if (!selectionValid)
        {
            var firstOwned = db.Table<OwnedCharacter>()
                               .Where(o => o.Count > 0)
                               .OrderBy(o => o.CharacterId)
                               .FirstOrDefault();

            player.SelectedCharacterId = firstOwned != null ? firstOwned.CharacterId : 0;
            db.Update(player);

            if (player.SelectedCharacterId > 0)
                Debug.Log($"[DB] Selección autoasignada a CharacterId={player.SelectedCharacterId}");
        }
    }

    public Character GetCharacterById(int id)
    {
        return db.Table<Character>().FirstOrDefault(c => c.Id == id);
    }

    public void SetSelectedCharacter(int characterId)
    {
        var player = db.Table<Player>().FirstOrDefault();
        if (player == null)
        {
            Debug.LogError("[GameDatabase] No se encontró el jugador para guardar el personaje seleccionado.");
            return;
        }

        player.SelectedCharacterId = characterId;
        db.Update(player);
        Debug.Log($"[GameDatabase] Personaje seleccionado guardado: {characterId}");
    }




}

