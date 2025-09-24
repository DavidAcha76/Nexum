using System;
using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// RogueLikeMiniMazesAR — Roguelike + mini-laberintos anclado a AR
/// - Salas rectangulares + pasillos en L + mini-laberintos internos (DFS).
/// - Inicio (player) y salida (portal) en extremos opuestos (2 BFS).
/// - Spawns de enemigos lejos de inicio/salida.
/// - Construcción centrada en el anchor AR (parent) respetando rotación/escala.
/// - NO modifica escala/altura/grosor del wallPrefab; se usa tal cual.
/// - 10× más pequeño (cellSize=0.05) y con offset global hacia abajo (mapYOffset).
/// - Compatible con C# 8.0 (máscara int en raycast).
/// </summary>
public class RogueLikeMiniMazesAR : MonoBehaviour
{
    [Header("Anchor (AR)")]
    [Tooltip("Anchor donde se instanciará el mapa (lo setea ARGridMapPlacer)")]
    public Transform parent;

    [Header("Prefabs Básicos")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;

    [Header("Prefabs de Inicio y Salida")]
    public GameObject playerPrefab;
    public GameObject exitPrefab;

    [Header("Enemy Spawns")]
    public GameObject[] enemyPrefabs;
    public int totalEnemies = 8;
    public int minGridDistFromStart = 6;
    public int minGridDistFromExit = 4;
    public float enemySpawnJitter = 0.04f; // 10× menos que antes (acorde al cellSize)

    [Header("Grid Settings")]
    public int width = 80;
    public int height = 60;

    // 10× más pequeño (antes 0.5f)
    public float cellSize = 0.05f;

    [Header("Altura / Raycast / Offset")]
    public LayerMask floorMask;
    public float spawnRayHeight = 0.05f; // reducido
    public float spawnYOffset = 0.002f;  // pequeño ajuste sobre el piso
    [Tooltip("Desplaza TODO el mapa hacia abajo respecto al anchor (valor negativo lo baja)")]
    public float mapYOffset = -0.03f;    // baja todo el mapa

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
    [Tooltip("False => mapa aleatorio cada Play")]
    public bool useFixedSeed = false;
    public int seed = 12345;

    [Header("AutoBuild")]
    public bool autoBuildOnStart = false;

    // ===== Internals =====
    private bool[,] walk; // true = piso
    private List<RectInt> rooms = new List<RectInt>();
    private System.Random rng;

    private readonly int[] dx = { 0, 1, 0, -1 };
    private readonly int[] dy = { 1, 0, -1, 0 };

    private Vector2Int startPos;
    private Vector2Int exitPos;

    private List<Vector2Int> walkableCache = new List<Vector2Int>();
    private readonly List<Vector3> enemySpawnWorld = new List<Vector3>();
    private readonly List<GameObject> spawned = new List<GameObject>();

    // ===== Ciclo =====
    void Start()
    {
        if (autoBuildOnStart && parent != null)
            Rebuild();
    }

    public void SetAnchor(Transform anchor, bool rebuild = true)
    {
        parent = anchor;
        parent.up = Vector3.up;
        if (rebuild) Rebuild();
    }

    public void Rebuild()
    {
        if (!parent)
        {
            Debug.LogWarning("[RogueLikeMiniMazesAR] No hay parent/anchor. Llama SetAnchor() antes.");
            return;
        }

        InitRandom();
        ClampInputs();
        ClearPrevious();

        Generate();
        BuildVisuals();
        PickEndpointsAndSpawn();
        SpawnEnemies();
    }

    // ===== Setup =====
    void InitRandom()
    {
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
        minGridDistFromStart = Mathf.Max(0, minGridDistFromStart);
        minGridDistFromExit = Mathf.Max(0, minGridDistFromExit);
    }

    // ===== Generación =====
    void Generate()
    {
        walk = new bool[width, height];
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

            // Reinicia interior a muro
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

            // Mantén el marco de la sala caminable
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

    // ===== Construcción Visual (centrado al anchor) =====
    void BuildVisuals()
    {
        if (floorPrefab == null || wallPrefab == null)
        {
            Debug.LogError("[RogueLikeMiniMazesAR] Asigna floorPrefab y wallPrefab.");
            return;
        }

        // Pisos (ajusto tamaño de celda; NO toco wall)
        ForEachCell((x, y) =>
        {
            if (!walk[x, y]) return;

            var pos = CellCenterToWorld(x, y) + parent.up * mapYOffset;
            var floor = Instantiate(floorPrefab, pos, parent.rotation, parent);

            // piso cuadrado del tamaño de la celda (respetando escala Y original)
            var s = floor.transform.localScale;
            floor.transform.localScale = new Vector3(cellSize, s.y <= 0 ? 0.01f : s.y, cellSize);

            spawned.Add(floor);

            // Muros alrededor si el vecino no es caminable (sin tocar escala del wallPrefab)
            TryPlaceWallIfNotPassable(x, y, Dir.North);
            TryPlaceWallIfNotPassable(x, y, Dir.East);
            TryPlaceWallIfNotPassable(x, y, Dir.South);
            TryPlaceWallIfNotPassable(x, y, Dir.West);
        });
    }

    void TryPlaceWallIfNotPassable(int x, int y, Dir d)
    {
        int nx = x + DX(d), ny = y + DZ(d);
        if (InBounds(nx, ny) && walk[nx, ny]) return;

        Vector3 basePos = CellCenterToWorld(x, y);
        Vector3 edgeOffset = parent.right * (DX(d) * (cellSize * 0.5f)) +
                             parent.forward * (DZ(d) * (cellSize * 0.5f));

        // Importante: NO tocamos escala del wallPrefab. Solo posición, orientación y offset global Y.
        var wall = Instantiate(wallPrefab, basePos + edgeOffset + parent.up * mapYOffset, parent.rotation, parent);
        wall.transform.rotation = Quaternion.AngleAxis(Angle(d), Vector3.up) * parent.rotation;
        spawned.Add(wall);
    }

    // ===== Extremos + Spawns =====
    void PickEndpointsAndSpawn()
    {
        walkableCache.Clear();
        ForEachCell((x, y) => { if (walk[x, y]) walkableCache.Add(new Vector2Int(x, y)); });

        if (walkableCache.Count < 2)
        {
            Debug.LogWarning("[RogueLikeMiniMazesAR] No hay suficientes celdas caminables para inicio/salida.");
            return;
        }

        // Heurística de diámetro: A -> far B, B -> far C
        Vector2Int A = walkableCache[rng.Next(walkableCache.Count)];
        Vector2Int B = FarthestFrom(A, out _);
        Vector2Int C = FarthestFrom(B, out _);

        startPos = PushInwardIfEdge(B);
        exitPos = PushInwardIfEdge(C);

        if (playerPrefab)
        {
            var wp = SnapToGround(CellCenterToWorld(startPos.x, startPos.y));
            var player = Instantiate(playerPrefab, wp, parent.rotation, parent);
            spawned.Add(player);
        }

        if (exitPrefab)
        {
            var we = SnapToGround(CellCenterToWorld(exitPos.x, exitPos.y));
            var exit = Instantiate(exitPrefab, we, parent.rotation, parent);
            spawned.Add(exit);
        }

        Debug.Log($"[RogueLikeMiniMazesAR] START: {startPos}  EXIT: {exitPos}");
    }

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

        int spawnedCount = 0;
        int attempts = 0;
        int maxAttempts = Mathf.Max(200, totalEnemies * 40);

        while (spawnedCount < totalEnemies && attempts < maxAttempts)
        {
            attempts++;

            Vector2Int c = walkableCache[rng.Next(walkableCache.Count)];
            if (c == startPos || c == exitPos) continue;

            if (GridDistance(c, startPos) < minGridDistFromStart) continue;
            if (GridDistance(c, exitPos) < minGridDistFromExit) continue;

            Vector3 center = CellCenterToWorld(c.x, c.y);
            float jx = (float)(rng.NextDouble() * 2.0 - 1.0) * maxJ;
            float jz = (float)(rng.NextDouble() * 2.0 - 1.0) * maxJ;
            Vector3 world = SnapToGround(center + parent.right * jx + parent.forward * jz);

            GameObject prefab = enemyPrefabs[rng.Next(enemyPrefabs.Length)];
            if (!prefab) continue;

            var enemy = Instantiate(prefab, world, parent.rotation, parent);
            enemySpawnWorld.Add(world);
            spawned.Add(enemy);
            spawnedCount++;
        }

        if (spawnedCount < totalEnemies)
            Debug.Log($"[RogueLikeMiniMazesAR] SpawnEnemies(): {spawnedCount}/{totalEnemies}. Ajusta restricciones.");
    }

    // ===== BFS / Vecinos / Ajustes =====
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

    // ===== Snap al piso (AR) =====
    Vector3 SnapToGround(Vector3 worldPos)
    {
        // Origen para raycast (levemente por encima)
        Vector3 origin = worldPos + parent.up * spawnRayHeight;

        // Máscara compatible C# 8.0
        int mask = (floorMask.value == 0) ? ~0 : floorMask.value;

        // Raycast "hacia abajo" en mundo. Si tu plano AR tiene collider, lo detecta.
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, spawnRayHeight * 2f, mask))
            return new Vector3(worldPos.x, hit.point.y + spawnYOffset + mapYOffset, worldPos.z);

        // Fallback: usa Y del anchor + offset global del mapa
        return new Vector3(worldPos.x, parent.position.y + spawnYOffset + mapYOffset, worldPos.z);
    }

    // ===== Helpers de tallado =====
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

    // ===== Utilidades =====
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

    // ===== Conversión a mundo centrada al anchor =====
    // Origen centrado: el anchor (tap) es el centro del mapa
    Vector3 GetOriginCentered()
    {
        float totalX = width * cellSize;
        float totalZ = height * cellSize;
        return parent.position
             - parent.right * (totalX * 0.5f)
             - parent.forward * (totalZ * 0.5f);
    }

    // Centro de la celda (x,y) desde ese origen
    Vector3 CellCenterToWorld(int x, int y)
    {
        var origin = GetOriginCentered();
        return origin
             + parent.right * (x * cellSize + cellSize * 0.5f)
             + parent.forward * (y * cellSize + cellSize * 0.5f);
    }

    // ==== Gizmos (debug) ====
    void OnDrawGizmosSelected()
    {
        if (!parent) return;

        if (walk != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.15f);
            ForEachCell((x, y) =>
            {
                if (walk[x, y])
                    Gizmos.DrawCube(CellCenterToWorld(x, y) + Vector3.up * mapYOffset,
                        new Vector3(cellSize * 0.95f, 0.005f, cellSize * 0.95f));
            });
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(CellCenterToWorld(startPos.x, startPos.y) + Vector3.up * (0.02f + mapYOffset), 0.02f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(CellCenterToWorld(exitPos.x, exitPos.y) + Vector3.up * (0.02f + mapYOffset), 0.02f);

        if (enemySpawnWorld != null)
        {
            Gizmos.color = Color.magenta;
            foreach (var w in enemySpawnWorld)
                Gizmos.DrawCube(w + Vector3.up * 0.02f, new Vector3(0.03f, 0.03f, 0.03f));
        }
    }

    void ClearPrevious()
    {
        for (int i = spawned.Count - 1; i >= 0; i--)
            if (spawned[i]) Destroy(spawned[i]);
        spawned.Clear();
        enemySpawnWorld.Clear();
    }

    // ==== Dir helpers ====
    enum Dir { North, East, South, West }
    int DX(Dir e) => (e == Dir.East ? 1 : e == Dir.West ? -1 : 0);
    int DZ(Dir e) => (e == Dir.North ? 1 : e == Dir.South ? -1 : 0);
    float Angle(Dir e) => (e == Dir.North ? 0f : e == Dir.East ? 90f : e == Dir.South ? 180f : 270f);
}
