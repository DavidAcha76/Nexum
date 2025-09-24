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

        // Asegurar que exista al menos un jugador
        if (db.Table<Player>().Count() == 0)
        {
            var newPlayer = new Player { Gold = 0 }; // Valor inicial normal
            db.Insert(newPlayer);

            Debug.Log($"Jugador creado con {newPlayer.Gold} de oro");
        }
    }

    // ========== ORO ==========
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

    // ========== PERSONAJES BASE ==========
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

    // ========== INVENTARIO DEL JUGADOR ==========
    public void IncrementOwned(string characterName, int rarity, int amount = 1)
    {
        var existing = db.Table<OwnedCharacter>().FirstOrDefault(o => o.Name == characterName);
        if (existing == null)
        {
            db.Insert(new OwnedCharacter
            {
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
    }

    public List<OwnedCharacter> GetOwned()
    {
        return db.Table<OwnedCharacter>().ToList();
    }
}
