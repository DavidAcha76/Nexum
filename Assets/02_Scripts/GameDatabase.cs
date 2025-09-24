using System.Collections.Generic;
using System.IO;
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

        // Asegurar que exista un jugador
        if (db.Table<Player>().Count() == 0)
        {
            db.Insert(new Player { Gold = 0 });
        }
    }

    // Oro
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

    // Personajes
    public void AddCharacter(string name, int rarity)
    {
        var c = new Character { Name = name, Rarity = rarity };
        db.Insert(c);
    }

    public List<Character> GetCharacters()
    {
        return db.Table<Character>().ToList();
    }
}
