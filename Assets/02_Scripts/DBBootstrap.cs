using UnityEngine;

public class DBBootstrap : MonoBehaviour
{
    private GameDatabase db;
    private GachaSystem gacha;

    void Start()
    {
        // Crear DB
        db = new GameDatabase();
        gacha = new GachaSystem(db);

        // Simular final de partida
        db.AddGold(100);
        Debug.Log("💰 Oro actual: " + db.GetGold());

        // Tirar gacha
        gacha.Roll();
        Debug.Log("🎲 Personajes en DB: " + db.GetCharacters().Count);
    }
}
