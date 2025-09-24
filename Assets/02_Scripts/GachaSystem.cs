using UnityEngine;

public class GachaSystem
{
    private GameDatabase db;

    public GachaSystem(GameDatabase database)
    {
        db = database;
    }

    public void Roll()
    {
        // Ejemplo simple de probabilidades
        int roll = Random.Range(0, 100);
        string name;
        int rarity;

        if (roll < 60) { name = "Slime"; rarity = 1; }
        else if (roll < 85) { name = "Knight"; rarity = 2; }
        else if (roll < 97) { name = "Wizard"; rarity = 3; }
        else { name = "Dragon"; rarity = 4; }

        db.AddCharacter(name, rarity);
        Debug.Log($"Obtuviste un {name} (Rareza {rarity})");
    }
}
