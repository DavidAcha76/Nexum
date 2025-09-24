using UnityEngine;

public class GoldInitializer : MonoBehaviour
{
    [Header("Oro inicial")]
    public int initialGold = 100;   // cantidad de oro al iniciar el juego

    private GameDatabase db;

    void Start()
    {
        db = new GameDatabase();

        // Obtener el oro actual
        int currentGold = db.GetGold();
        Debug.Log($"[GoldInitializer] Oro actual: {currentGold}");

        // Si tiene menos que el inicial, asignar el oro inicial
        if (currentGold < initialGold)
        {
            int toAdd = initialGold - currentGold;
            db.AddGold(toAdd);
            Debug.Log($"[GoldInitializer] Se añadió {toAdd} de oro. Nuevo total: {db.GetGold()}");
        }
    }
}
