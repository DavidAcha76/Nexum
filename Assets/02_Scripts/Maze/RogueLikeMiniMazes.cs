using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// RogueLikeMiniMazes — Simple & Efficient (con inicio/salida seguros + spawns de enemigos + trampas)
/// - Salas roguelike + pasillos en L + mini-laberintos internos.
/// - Extremos opuestos (2 BFS) + empuje 1 paso si caen en borde.
/// - Snap al piso con raycast (máscara int compatible C# 8.0).
/// - Spawns de enemigos en celdas caminables.
/// - Trampas: algunas celdas de piso se reemplazan por trapFloorPrefab (idéntico al piso),
///   no se distinguen hasta que el player pisa la celda; allí ejecutan el efecto.
/// </summary>
public class RogueLikeMiniMazes : MonoBehaviour
{
    [Header("Prefabs Básicos")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;

    [Header("Prefabs de Inicio y Salida")]
    public GameObject playerPrefab;
    public GameObject exitPrefab;

    [Header("Enemy Spawns")]
    [Tooltip("Prefabs posibles para enemigos (elige uno al azar por spawn)")]
    public GameObject[] enemyPrefabs;
    [Tooltip("Cantidad total de enemigos a instanciar")]
    public int totalEnemies = 8;
    [Tooltip("Jitter horizontal dentro de la celda (unidades mundo). 0 = centro exacto")]
    public float enemySpawnJitter = 0f;

    [Header("Trampas")]
    [Tooltip("Prefab de trampa (idéntico visual al piso)")]
    public GameObject trapFloorPrefab;
    [Tooltip("Probabilidad de que una celda caminable sea trampa (0..1)")]
    [Range(0f, 1f)] public float trapProbability = 0.08f;
    [Tooltip("No colocar trampas en estas celdas alrededor del inicio/salida (radio Manhattan)")]
    [Min(0)] public int trapSafeRadius = 2;

    [Header("Grid Settings")]
    public int width = 80;
    public int height = 60;
    public float cellSize = 3f;

    [Header("Rooms (Roguelike)")]
    public int maxRoomAttempts = 60;
    public int maxRooms = 12;
    public int roomMinW = 6;
    public int roomMinH = 6;
    public int roomMaxW = 14;
    public int roomMaxH = 12;

    [Header("Mini-Laberintos en Salas")]
    [Range(0, 1f)] public float mazeRoomRatio = 0.5f;
    [Min(2)] public int mazeGridStep = 2;

    [Header("Random")]
    [Tooltip("Dejar en false para mapa siempre aleatorio")]
    public bool useFixedSeed = false;
    public int seed = 12345;

    [Header("Spawn & Piso")]
    [Tooltip("Capa del piso para raycast (opcional)")]
    public LayerMask floorMask;
    [Tooltip("Altura desde donde cae el raycast")]
    public float spawnRayHeight = 50f;
    [Tooltip("Offset vertical extra tras el raycast")]
    public float spawnYOffset = 0.1f;

    // ===== Internals =====
    private bool[,] walk; // true = piso
    private List<RectInt> rooms = new List<RectInt>();
    private System.Random rng;

    private readonly int[] dx = { 0, 1, 0, -1 };
    private readonly int[] dy = { 1, 0, -1, 0 };

    private Vector2Int startPos;
    private Vector2Int exitPos;

    // Cache de celdas caminables para spawns
    private List<Vector2Int> walkableCache = new List<Vector2Int>();

    // Referencia a pisos por celda para poder reemplazar por trampas
    private GameObject[,] floorRefs;

    // Celdas marcadas como trampa (para fácil consulta)
    private bool[,] isTrap;

    // Gizmo de debug de spawns enemigos
    private readonly List<Vector3> enemySpawnWorld = new List<Vector3>();

    void Start()
    {
        InitRandom();
        ClampInputs();
        Generate();
        BuildVisuals();           // crea walls y pisos base
        PickEndpointsAndSpawn();  // player + portal
        PlaceTraps();             // reemplaza ciertos pisos por trampas
        SpawnEnemies();           // enemigos
    }

    #region Setup
    void InitRandom()
    {
        // Mantén false para que siempre sea aleatorio en cada Play.
        rng = useFixedSeed ? new System.Random(seed)
            : new System.Random(UnityEngine.Random.Range(int.MinValue, int.MaxValue));
    }

    void ClampInputs()
    {
        width = Mathf.Max(width, 10);
        height = Mathf.Max(height, 10);
        roomMinW = Mathf.Clamp(roomMinW, 3, Mathf.Max(3, roomMaxW));
        roomMinH = Mathf.Clamp(roomMinH, 3, Mathf.Max(3, roomMaxH));
        roomMaxW = Mathf.Max(roomMaxW, roomMinW);
        roomMaxH = Mathf.Max(roomMaxH, roomMinH);
        mazeGridStep = Mathf.Max(2, mazeGridStep);
        maxRooms = Mathf.Max(1, maxRooms);
        maxRoomAttempts = Mathf.Max(maxRoomAttempts, maxRooms);
        trapSafeRadius = Mathf.Max(0, trapSafeRadius);
        trapProbability = Mathf.Clamp01(trapProbability);
    }
    #endregion

    #region Generación
    void Generate()
    {
        walk = new bool[width, height];
        isTrap = new bool[width, height];
        floorRefs = new GameObject[width, height];
        rooms.Clear();

        PlaceRooms();
        ConnectRooms();
        CarveMiniMazesInRooms();
    }

    void PlaceRooms()
    {
        int placed = 0;
        for (int i = 0; i < maxRoomAttempts && placed < maxRooms; i++)
        {
            int w = rng.Next(roomMinW, roomMaxW + 1);
            int h = rng.Next(roomMinH, roomMaxH + 1);
            int x = rng.Next(1, Mathf.Max(2, width - w - 1));
            int y = rng.Next(1, Mathf.Max(2, height - h - 1));
            var r = new RectInt(x, y, w, h);

            if (IntersectsAny(r, rooms, 1)) continue;

            rooms.Add(r);
            CarveRect(r);
            placed++;
        }

        if (rooms.Count == 0)
        {
            var fallback = new RectInt(width / 2 - 5, height / 2 - 4, 10, 8);
            rooms.Add(fallback);
            CarveRect(fallback);
        }
    }

    void ConnectRooms()
    {
        rooms.Sort((a, b) => Center(a).x.CompareTo(Center(b).x));
        for (int i = 1; i < rooms.Count; i++)
        {
            Vector2Int a = Center(rooms[i - 1]);
            Vector2Int b = Center(rooms[i]);
            CarveCorridorL(a, b);
        }
    }

    void CarveMiniMazesInRooms()
    {
        foreach (var r in rooms)
        {
            if (rng.NextDouble() > mazeRoomRatio) continue;

            var inner = new RectInt(r.xMin + 1, r.yMin + 1, Mathf.Max(1, r.width - 2), Mathf.Max(1, r.height - 2));
            if (inner.width < 3 || inner.height < 3) continue;

            // Reinicia interior a MURO
            FillRect(inner, false);

            // Celdas discretas para el DFS interno
            List<Vector2Int> cells = new List<Vector2Int>();
            for (int y = inner.yMin; y < inner.yMax; y += mazeGridStep)
                for (int x = inner.xMin; x < inner.xMax; x += mazeGridStep)
                    cells.Add(new Vector2Int(x, y));
            if (cells.Count == 0) continue;

            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            Vector2Int start = cells[rng.Next(cells.Count)];
            DFS_MiniMaze(start, inner, visited);

            // Mantén el marco de la sala caminable para no romper pasillos
            CarveRectBorder(r);
        }
    }

    void DFS_MiniMaze(Vector2Int c, RectInt inner, HashSet<Vector2Int> visited)
    {
        visited.Add(c);
        MakeWalk(c);

        List<int> dirs = new List<int> { 0, 1, 2, 3 };
        Shuffle(dirs);

        foreach (int d in dirs)
        {
            Vector2Int n = new Vector2Int(c.x + dx[d] * mazeGridStep, c.y + dy[d] * mazeGridStep);
            if (!inner.Contains(n) || visited.Contains(n)) continue;

            CarveLine(c, n);
            DFS_MiniMaze(n, inner, visited);
        }
    }
    #endregion

    #region Construcción Visual
    void BuildVisuals()
    {
        if (floorPrefab == null || wallPrefab == null)
        {
            Debug.LogError("Asigna floorPrefab y wallPrefab.");
            return;
        }

        float half = cellSize * 0.5f;

        // Pisos (guardamos referencia por celda para poder reemplazar por trampas)
        ForEachCell((x, y) =>
        {
            if (!walk[x, y]) return;
            var f = Instantiate(floorPrefab, GridToWorld(x, y), Quaternion.identity, transform);
            floorRefs[x, y] = f;
        });

        // Muros (contorno de celdas caminables)
        ForEachCell((x, y) =>
        {
            if (!walk[x, y]) return;
            Vector3 basePos = GridToWorld(x, y);

            if (!IsWalk(x, y + 1))
                Instantiate(wallPrefab, basePos + new Vector3(0, 0, half), Quaternion.identity, transform);
            if (!IsWalk(x + 1, y))
                Instantiate(wallPrefab, basePos + new Vector3(half, 0, 0), Quaternion.Euler(0, 90, 0), transform);
            if (!IsWalk(x, y - 1))
                Instantiate(wallPrefab, basePos + new Vector3(0, 0, -half), Quaternion.identity, transform);
            if (!IsWalk(x - 1, y))
                Instantiate(wallPrefab, basePos + new Vector3(-half, 0, 0), Quaternion.Euler(0, 90, 0), transform);
        });
    }
    #endregion

    #region Extremos + Trampas + Enemigos
    void PickEndpointsAndSpawn()
    {
        // Cachear celdas caminables
        walkableCache.Clear();
        ForEachCell((x, y) => { if (walk[x, y]) walkableCache.Add(new Vector2Int(x, y)); });

        if (walkableCache.Count < 2)
        {
            Debug.LogWarning("No hay suficientes celdas caminables para ubicar inicio/salida.");
            return;
        }

        // Heurística de diámetro:
        Vector2Int A = walkableCache[rng.Next(walkableCache.Count)];
        Vector2Int B = FarthestFrom(A, out _);
        Vector2Int C = FarthestFrom(B, out _);

        // Usamos B como inicio y C como salida (extremos opuestos)
        startPos = PushInwardIfEdge(B);
        exitPos = PushInwardIfEdge(C);

        // Instanciar en posiciones "snap al piso"
        if (playerPrefab)
        {
            Vector3 wp = SnapToGround(GridToWorld(startPos.x, startPos.y));
            Instantiate(playerPrefab, wp, Quaternion.identity);
        }

        if (exitPrefab)
        {
            Vector3 we = SnapToGround(GridToWorld(exitPos.x, exitPos.y));
            Instantiate(exitPrefab, we, Quaternion.identity);
        }

        Debug.Log($"START: {startPos}  EXIT: {exitPos}");
    }

    // Marca y coloca trampas reemplazando ciertos pisos por trapFloorPrefab
    void PlaceTraps()
    {
        if (!trapFloorPrefab || trapProbability <= 0f) return;

        foreach (var cell in walkableCache)
        {
            // Evitar trampa en inicio/salida y un pequeño radio seguro
            if (GridDistance(cell, startPos) <= trapSafeRadius) continue;
            if (GridDistance(cell, exitPos) <= trapSafeRadius) continue;

            if (UnityEngine.Random.value <= trapProbability)
            {
                int x = cell.x, y = cell.y;
                if (!IsWalk(x, y)) continue;

                // Reemplaza el piso por el prefab de trampa
                var prev = floorRefs[x, y];
                Vector3 pos = GridToWorld(x, y);
                Quaternion rot = prev ? prev.transform.rotation : Quaternion.identity;
                Transform parent = prev ? prev.transform.parent : transform;

                if (prev) Destroy(prev);

                var trap = Instantiate(trapFloorPrefab, pos, rot, parent);

                // Asegura tener un TrapTile (si el prefab no lo tiene, lo añadimos)
                if (!trap.TryGetComponent<TrapTile>(out var tile))
                {
                    tile = trap.AddComponent<TrapTile>();
                }
                // Config mínimo opcional del componente (puedes ajustar en el prefab también)
                tile.consumeOnTrigger = true;      // se desactiva al activarse
                tile.knockUpForce = 4f;            // efecto por defecto si hay Rigidbody
                tile.debugLog = false;

                isTrap[x, y] = true;
                floorRefs[x, y] = trap; // ahora el piso de esa celda es la trampa
            }
        }
    }

    // Spawns de enemigos simples y seguros (dentro del mapa)
    void SpawnEnemies()
    {
        enemySpawnWorld.Clear();

        if (enemyPrefabs == null || enemyPrefabs.Length == 0 || totalEnemies <= 0)
            return;

        if (walkableCache == null || walkableCache.Count == 0)
        {
            walkableCache = new List<Vector2Int>();
            ForEachCell((x, y) => { if (walk[x, y]) walkableCache.Add(new Vector2Int(x, y)); });
            if (walkableCache.Count == 0) return;
        }

        float maxJ = Mathf.Clamp(enemySpawnJitter, 0f, cellSize * 0.45f);

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = Mathf.Max(200, totalEnemies * 40); // salvaguarda

        while (spawned < totalEnemies && attempts < maxAttempts)
        {
            attempts++;

            Vector2Int c = walkableCache[rng.Next(walkableCache.Count)];
            if (c == startPos || c == exitPos) continue;

            Vector3 p = GridToWorld(c.x, c.y);
            float jx = (float)(rng.NextDouble() * 2.0 - 1.0) * maxJ;
            float jz = (float)(rng.NextDouble() * 2.0 - 1.0) * maxJ;
            Vector3 world = SnapToGround(new Vector3(p.x + jx, p.y, p.z + jz));

            GameObject prefab = enemyPrefabs[rng.Next(enemyPrefabs.Length)];
            if (prefab == null) continue;

            Instantiate(prefab, world, Quaternion.identity);
            enemySpawnWorld.Add(world);
            spawned++;
        }

        if (spawned < totalEnemies)
            Debug.Log($"SpawnEnemies(): {spawned}/{totalEnemies}. Sube maxRooms o baja restricciones.");
    }
    #endregion

    #region BFS / Vecinos / Ajustes
    Vector2Int FarthestFrom(Vector2Int src, out Dictionary<Vector2Int, int> distOut)
    {
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        Dictionary<Vector2Int, int> dist = new Dictionary<Vector2Int, int>();
        q.Enqueue(src);
        dist[src] = 0;

        Vector2Int far = src;
        int maxd = 0;

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            int cd = dist[cur];
            foreach (var n in GetNeighbors(cur))
            {
                if (dist.ContainsKey(n)) continue;
                dist[n] = cd + 1;
                q.Enqueue(n);
                if (dist[n] > maxd)
                {
                    maxd = dist[n];
                    far = n;
                }
            }
        }
        distOut = dist;
        return far;
    }

    IEnumerable<Vector2Int> GetNeighbors(Vector2Int c)
    {
        for (int i = 0; i < 4; i++)
        {
            int nx = c.x + dx[i];
            int ny = c.y + dy[i];
            if (IsWalk(nx, ny)) yield return new Vector2Int(nx, ny);
        }
    }

    // Si cae en borde, intenta mover 1 paso hacia adentro manteniendo celda caminable.
    Vector2Int PushInwardIfEdge(Vector2Int p)
    {
        bool isEdge = (p.x == 0 || p.y == 0 || p.x == width - 1 || p.y == height - 1);
        if (!isEdge) return p;

        var candidates = new List<Vector2Int>();
        if (p.x == 0) candidates.Add(new Vector2Int(p.x + 1, p.y));
        if (p.x == width - 1) candidates.Add(new Vector2Int(p.x - 1, p.y));
        if (p.y == 0) candidates.Add(new Vector2Int(p.x, p.y + 1));
        if (p.y == height - 1) candidates.Add(new Vector2Int(p.x, p.y - 1));

        foreach (var c in candidates)
            if (IsWalk(c.x, c.y)) return c;

        return p;
    }
    #endregion

    #region Snap al Piso (compatible C# 8.0)
    // Raycast hacia abajo para encontrar el piso. Fallback en y=0 si no golpea nada.
    Vector3 SnapToGround(Vector3 worldPos)
    {
        Vector3 origin = worldPos + Vector3.up * spawnRayHeight;

        // Usamos int para la máscara para compatibilidad con C# 8.0
        int mask = (floorMask.value == 0) ? ~0 : floorMask.value;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, spawnRayHeight * 2f, mask))
        {
            return new Vector3(worldPos.x, hit.point.y + spawnYOffset, worldPos.z);
        }

        // Si no detecta piso, por defecto coloca en y=0
        return new Vector3(worldPos.x, spawnYOffset, worldPos.z);
    }
    #endregion

    #region Helpers de Tallado
    void CarveRect(RectInt r)
    {
        for (int y = r.yMin; y < r.yMax; y++)
            for (int x = r.xMin; x < r.xMax; x++)
                MakeWalk(new Vector2Int(x, y));
    }

    void FillRect(RectInt r, bool value)
    {
        for (int y = r.yMin; y < r.yMax; y++)
            for (int x = r.xMin; x < r.xMax; x++)
                if (InBounds(x, y)) walk[x, y] = value;
    }

    void CarveRectBorder(RectInt r)
    {
        for (int x = r.xMin; x < r.xMax; x++)
        {
            MakeWalk(new Vector2Int(x, r.yMin));
            MakeWalk(new Vector2Int(x, r.yMax - 1));
        }
        for (int y = r.yMin; y < r.yMax; y++)
        {
            MakeWalk(new Vector2Int(r.xMin, y));
            MakeWalk(new Vector2Int(r.xMax - 1, y));
        }
    }

    void CarveCorridorL(Vector2Int a, Vector2Int b)
    {
        LineX(a.x, b.x, a.y);
        LineY(a.y, b.y, b.x);
    }

    void LineX(int x0, int x1, int y)
    {
        int step = x1 >= x0 ? 1 : -1;
        for (int x = x0; x != x1 + step; x += step)
            MakeWalk(new Vector2Int(x, y));
    }

    void LineY(int y0, int y1, int x)
    {
        int step = y1 >= y0 ? 1 : -1;
        for (int y = y0; y != y1 + step; y += step)
            MakeWalk(new Vector2Int(x, y));
    }

    void CarveLine(Vector2Int a, Vector2Int b)
    {
        if (a.x == b.x) LineY(a.y, b.y, a.x);
        else if (a.y == b.y) LineX(a.x, b.x, a.y);
        else CarveCorridorL(a, b);
    }

    void MakeWalk(Vector2Int c)
    {
        if (InBounds(c.x, c.y)) walk[c.x, c.y] = true;
    }
    #endregion

    #region Utilidades
    bool InBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;
    bool IsWalk(int x, int y) => (x >= 0 && x < width && y >= 0 && y < height) && walk[x, y];
    Vector2Int Center(RectInt r) => new Vector2Int(r.xMin + r.width / 2, r.yMin + r.height / 2);

    bool IntersectsAny(RectInt r, List<RectInt> list, int padding)
    {
        RectInt expanded = new RectInt(r.xMin - padding, r.yMin - padding, r.width + 2 * padding, r.height + 2 * padding);
        foreach (var o in list) if (expanded.Overlaps(o)) return true;
        return false;
    }

    int GridDistance(Vector2Int a, Vector2Int b)
    {
        // Distancia Manhattan sobre la grilla (simple y rápida)
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    void ForEachCell(Action<int, int> fn)
    {
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                fn(x, y);
    }

    void Shuffle<T>(IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = rng.Next(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    Vector3 GridToWorld(int x, int y) => new Vector3(x * cellSize, 0f, y * cellSize);

    void OnDrawGizmosSelected()
    {
        if (walk != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.15f);
            ForEachCell((x, y) =>
            {
                if (walk[x, y])
                    Gizmos.DrawCube(GridToWorld(x, y), new Vector3(cellSize * 0.95f, 0.1f, cellSize * 0.95f));
            });
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(GridToWorld(startPos.x, startPos.y) + Vector3.up * 0.5f, 0.3f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(GridToWorld(exitPos.x, exitPos.y) + Vector3.up * 0.5f, 0.3f);

        // Enemigos
        if (enemySpawnWorld != null)
        {
            Gizmos.color = Color.magenta;
            foreach (var w in enemySpawnWorld)
                Gizmos.DrawCube(w + Vector3.up * 0.2f, new Vector3(0.3f, 0.3f, 0.3f));
        }
    }
    #endregion
}
