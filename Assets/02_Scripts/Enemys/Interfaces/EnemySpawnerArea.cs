using System; // Remueve este using si no lo necesitas para evitar conflicto con Random
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnerArea : MonoBehaviour
{
    [Header("Refs")]
    public GridRoomGenerator grid;

    [Header("Qué y cuánto")]
    public GameObject[] enemyPrefabs;
    public int count = 6;

    [Header("Validación de posición")]
    public LayerMask floorMask;
    public LayerMask blockMask;
    public float minSeparation = 0.8f;
    public float insidePaddingCells = 0.25f;
    public int attemptsPerEnemy = 40;

    [Header("Parenting")]
    public bool parentToMazeAnchor = true;

    [Header("Enemy Scale")]
    public bool useScaleByCell = false;
    public float enemyScaleMultiplier = 1f;
    public float scalePerCell = 1.0f;
    public float minScaleMeters = 0.05f;

    [Header("Zonas de spawn")]
    public bool roomsOnly = true;

    [Header("Debug")]
    public bool verbose = false;

    readonly List<Vector3> placed = new List<Vector3>();
    private bool gridReady = false;

    void OnEnable()
    {
        if (!grid) grid = FindObjectOfType<GridRoomGenerator>();

        if (grid != null)
        {
            grid.OnBuilt += HandleGridBuilt;
            // Si el grid ya está construido, spawnear inmediatamente
            if (IsGridReady())
            {
                HandleGridBuilt();
            }
        }
        else
        {
            StartCoroutine(WaitForGrid());
        }
    }

    void OnDisable()
    {
        if (grid != null)
        {
            grid.OnBuilt -= HandleGridBuilt;
        }
    }

    IEnumerator WaitForGrid()
    {
        int attempts = 0;
        while (grid == null && attempts < 50)
        {
            grid = FindObjectOfType<GridRoomGenerator>();
            if (grid != null)
            {
                grid.OnBuilt += HandleGridBuilt;
                if (IsGridReady())
                {
                    HandleGridBuilt();
                    yield break;
                }
            }
            attempts++;
            yield return new WaitForSeconds(0.1f);
        }
    }

    bool IsGridReady()
    {
        // Método simple para verificar si el grid está listo
        return grid != null && grid.transform.childCount > 0;
    }

    void HandleGridBuilt()
    {
        gridReady = true;
        StartCoroutine(SpawnEnemiesCoroutine());
    }

    IEnumerator SpawnEnemiesCoroutine()
    {
        if (!gridReady || enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            if (verbose) Debug.LogWarning("[EnemySpawnerArea] Condiciones no cumplidas para spawnear.");
            yield break;
        }

        int spawned = 0, guard = 0;
        placed.Clear();

        while (spawned < count && guard < count * attemptsPerEnemy)
        {
            guard++;

            Vector3 pos;
            bool ok = TryGetAnyValidPoint(out pos);

            if (ok)
            {
                // separación con los ya colocados
                if (HasNearby(pos, placed, minSeparation))
                    continue;

                // bloqueos (muros/props)
                if (Physics.CheckSphere(pos, minSeparation * 0.5f, blockMask, QueryTriggerInteraction.Ignore))
                    continue;

                // Elegir prefab
                var prefab = enemyPrefabs[UnityEngine.Random.Range(0, enemyPrefabs.Length)];
                Vector3 drop = pos + Vector3.up * 2f;

                if (Physics.Raycast(drop, Vector3.down, out var hit, 5f, floorMask, QueryTriggerInteraction.Ignore))
                    drop = hit.point;

                // Instanciar
                var go = Instantiate(prefab, drop, Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0));
                if (parentToMazeAnchor && grid.parent) go.transform.SetParent(grid.parent, true);

                // Escalado       
                if (useScaleByCell)
                {
                    float s = Mathf.Clamp(grid.cellSize * scalePerCell, 0.2f, 1.0f);
                    go.transform.localScale = new Vector3(s, s, s);
                }
                else
                {
                    // Escala fija por multiplicador
                    float scale = Mathf.Clamp(enemyScaleMultiplier, 0.1f, 1f);
                    go.transform.localScale = Vector3.one * scale;
                }


                placed.Add(drop);
                spawned++;

                if (verbose) Debug.DrawRay(drop, Vector3.up * 1f, Color.green, 10f);
            }

            if ((spawned & 1) == 0) yield return null;
        }

        if (verbose) Debug.Log($"[EnemySpawnerArea] Spawned {spawned}/{count} (intentos {guard}).");
    }

    bool TryGetAnyValidPoint(out Vector3 pos)
    {
        pos = default;
        if (grid == null) return false;

        // Si roomsOnly está activado → genera dentro de una habitación
        if (roomsOnly)
        {
            return grid.TrySampleRoomFloorPoint(out pos, insidePaddingCells);
        }
        else
        {
            // Si no, puede generar en cualquier celda caminable (habitaciones o pasillos)
            return grid.TrySampleFloorPoint(out pos, insidePaddingCells);
        }
    }


    Bounds GetGridBounds()
    {
        // Calcular bounds aproximados del grid
        if (grid == null) return new Bounds();

        Vector3 center = grid.transform.position;
        Vector3 size = Vector3.zero;

        // Asumir tamaño basado en cellSize y grid dimensions (si están disponibles)
        if (grid.GetType().GetField("gridSize") != null)
        {
            var gridSize = (Vector2Int)grid.GetType().GetField("gridSize").GetValue(grid);
            size = new Vector3(gridSize.x * grid.cellSize, 10f, gridSize.y * grid.cellSize);
        }
        else
        {
            // Fallback: bounds del renderer del grid o de sus hijos
            Renderer rend = grid.GetComponent<Renderer>();
            if (rend != null)
            {
                return rend.bounds;
            }
            else
            {
                // Estimación conservadora
                size = new Vector3(20f, 10f, 20f);
            }
        }

        return new Bounds(center, size);
    }

    bool HasNearby(Vector3 p, List<Vector3> list, float r)
    {
        float r2 = r * r;
        foreach (var v in list) if ((v - p).sqrMagnitude < r2) return true;
        return false;
    }

    // Método para spawn manual si es necesario
    public void ManualSpawn()
    {
        if (gridReady)
        {
            StartCoroutine(SpawnEnemiesCoroutine());
        }
    }
}