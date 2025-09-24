using UnityEngine;

[RequireComponent(typeof(EnemySpawnerArea))]
public class EnemyRespawnOnBuilt : MonoBehaviour
{
    public GridRoomGenerator grid;
    EnemySpawnerArea spawner;

    void Awake()
    {
        spawner = GetComponent<EnemySpawnerArea>();
        if (!grid) grid = FindObjectOfType<GridRoomGenerator>();
    }

    void OnEnable()
    {
        if (grid != null) grid.OnBuilt += HandleBuilt;
    }

    void OnDisable()
    {
        if (grid != null) grid.OnBuilt -= HandleBuilt;
    }

    void HandleBuilt()
    {
        // Reinicia el spawner para re-colocar
        gameObject.SetActive(false);
        gameObject.SetActive(true);
    }
}
