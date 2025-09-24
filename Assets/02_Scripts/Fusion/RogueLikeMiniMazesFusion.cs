using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// RogueLikeMiniMazesFusion — Mapa determinista por seed + spawns en red (Fusion 2)
/// - Mapa (pisos/muros/trampas) se genera localmente en cada peer con el mismo seed (no se sincroniza).
/// - Host (StateAuthority) decide el seed y lo envía por RPC; luego spawnea ENEMIGOS por red.
/// - Jugadores se spawnean desde NetworkGameLauncher.OnPlayerJoined.
/// </summary>
public class RogueLikeMiniMazesFusion : NetworkBehaviour
{
    [Header("Prefabs Básicos (locales, NO en red)")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;

    [Header("Prefabs de Inicio y Salida (locales)")]
    public GameObject exitPrefab;

    [Header("Enemy Spawns (EN RED)")]
    [Tooltip("Prefabs de enemigos en red (NetworkObject). El Host spawnea con Runner.Spawn")]
    public NetworkObject[] enemyPrefabs;
    public int totalEnemies = 8;
    public float enemySpawnJitter = 0f;

    [Header("Trampas (local)")]
    public GameObject trapFloorPrefab;
    [Range(0f, 1f)] public float trapProbability = 0.08f;
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
    public bool useFixedSeed = false;
    public int seed = 12345;

    [Header("Spawn & Piso")]
    public LayerMask floorMask;
    public float spawnRayHeight = 50f;
    public float spawnYOffset = 0.1f;

    // ===== Internals (locales) =====
    private bool[,] walk;
    private List<RectInt> rooms = new List<RectInt>();
    private System.Random rng;

    private readonly int[] dx = { 0, 1, 0, -1 };
    private readonly int[] dy = { 1, 0, -1, 0 };

    private Vector2Int startPos;
    private Vector2Int exitPos;

    private List<Vector2Int> walkableCache = new List<Vector2Int>();
    private GameObject[,] floorRefs;
    private bool[,] isTrap;

    private readonly List<Vector3> enemySpawnWorld = new List<Vector3>();

    // Expuestos a otros scripts (p.ej. Launcher para spawn de jugadores)
    public bool HasValidStart => walk != null && IsWalk(startPos.x, startPos.y);
    public Vector3 GetSafePlayerSpawnWorld() => SnapToGround(GridToWorld(startPos.x, startPos.y));

    // ========= CICLO =========

    public override void Spawned()
    {
        // Nada más aparecer el objeto de mapa en la red
        if (Object.HasStateAuthority)
        {
            // Host decide el seed
            int chosenSeed = useFixedSeed ? seed : UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            seed = chosenSeed;

            // Broadcast + generación en todos (incluye Host)
            RPC_InitAndGenerate(chosenSeed);
        }
    }

    /// <summary>
    /// Llama esto desde el Launcher tras StartGame si ya estaba en escena.
    /// </summary>
    public void HostBroadcastSeedAndGenerate()
    {
        if (!Runner || !Runner.IsServer) return;

        int chosenSeed = useFixedSeed ? seed : UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        seed = chosenSeed;
        RPC_InitAndGenerate(chosenSeed);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_InitAndGenerate(int sharedSeed)
    {
        seed = sharedSeed;
        FullGenerateAndBuild();

        // Solo Host spawnea enemigos por red
        if (Runner != null && Runner.IsServer)
        {
            NetworkSpawnEnemies();
        }
    }

    // ========== GENERACIÓN LOCAL ==========
    private void FullGenerateAndBuild()
    {
        InitRandom();
        ClampInputs();
        ClearPreviousBuild();

        Generate();
        BuildVisuals();
        PickEndpointsAndSpawn();
        PlaceTraps();

        Debug.Log($"[Map] Seed={seed} START={startPos} EXIT={exitPos}");
    }

    private void InitRandom()
    {
        rng = new System.Random(seed);
    }

    private void ClampInputs()
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

    private void ClearPreviousBuild()
    {
        // Limpia hijos previos (si regeneras en la misma escena)
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    private void Generate()
    {
        walk = new bool[width, height];
        isTrap = new bool[width, height];
        floorRefs = new GameObject[width, height];
        rooms.Clear();

        PlaceRooms();
        ConnectRooms();
        CarveMiniMazesInRooms();
    }

    private void PlaceRooms()
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

    private void ConnectRooms()
    {
        rooms.Sort((a, b) => Center(a).x.CompareTo(Center(b).x));
        for (int i = 1; i < rooms.Count; i++)
        {
            Vector2Int a = Center(rooms[i - 1]);
            Vector2Int b = Center(rooms[i]);
            CarveCorridorL(a, b);
        }
    }

    private void CarveMiniMazesInRooms()
    {
        foreach (var r in rooms)
        {
            if (rng.NextDouble() > mazeRoomRatio) continue;

            var inner = new RectInt(r.xMin + 1, r.yMin + 1, Mathf.Max(1, r.width - 2), Mathf.Max(1, r.height - 2));
            if (inner.width < 3 || inner.height < 3) continue;

            FillRect(inner, false);

            List<Vector2Int> cells = new List<Vector2Int>();
            for (int y = inner.yMin; y < inner.yMax; y += mazeGridStep)
                for (int x = inner.xMin; x < inner.xMax; x += mazeGridStep)
                    cells.Add(new Vector2Int(x, y));
            if (cells.Count == 0) continue;

            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            Vector2Int start = cells[rng.Next(cells.Count)];
            DFS_MiniMaze(start, inner, visited);

            CarveRectBorder(r);
        }
    }

    private void DFS_MiniMaze(Vector2Int c, RectInt inner, HashSet<Vector2Int> visited)
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

    // ========== VISUAL LOCAL ==========
    private void BuildVisuals()
    {
        if (floorPrefab == null || wallPrefab == null)
        {
            Debug.LogError("Asigna floorPrefab y wallPrefab.");
            return;
        }

        float half = cellSize * 0.5f;

        // Pisos
        ForEachCell((x, y) =>
        {
            if (!walk[x, y]) return;
            var f = Instantiate(floorPrefab, GridToWorld(x, y), Quaternion.identity, transform);
            floorRefs[x, y] = f;
        });

        // Muros (bordes de celdas caminables)
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

    // ========== START/EXIT + TRAMPAS LOCAL ==========
    private void PickEndpointsAndSpawn()
    {
        walkableCache.Clear();
        ForEachCell((x, y) => { if (walk[x, y]) walkableCache.Add(new Vector2Int(x, y)); });
        if (walkableCache.Count < 2) return;

        Vector2Int A = walkableCache[rng.Next(walkableCache.Count)];
        Vector2Int B = FarthestFrom(A, out _);
        Vector2Int C = FarthestFrom(B, out _);

        startPos = PushInwardIfEdge(B);
        exitPos = PushInwardIfEdge(C);

        if (exitPrefab)
        {
            Vector3 we = SnapToGround(GridToWorld(exitPos.x, exitPos.y));
            Instantiate(exitPrefab, we, Quaternion.identity, transform);
        }
    }

    private void PlaceTraps()
    {
        if (!trapFloorPrefab || trapProbability <= 0f) return;

        foreach (var cell in walkableCache)
        {
            if (GridDistance(cell, startPos) <= trapSafeRadius) continue;
            if (GridDistance(cell, exitPos) <= trapSafeRadius) continue;

            if (UnityEngine.Random.value <= trapProbability)
            {
                int x = cell.x, y = cell.y;
                if (!IsWalk(x, y)) continue;

                var prev = floorRefs[x, y];
                Vector3 pos = GridToWorld(x, y);
                Quaternion rot = prev ? prev.transform.rotation : Quaternion.identity;
                Transform parent = prev ? prev.transform.parent : transform;

                if (prev) Destroy(prev);

                var trap = Instantiate(trapFloorPrefab, pos, rot, parent);
                floorRefs[x, y] = trap;
                isTrap[x, y] = true;

                // Añade tu componente de trampa si hace falta
                if (!trap.TryGetComponent<TrapTile>(out var tile))
                {
                    tile = trap.AddComponent<TrapTile>();
                    tile.consumeOnTrigger = true;
                    tile.knockUpForce = 4f;
                    tile.debugLog = false;
                }
            }
        }
    }

    // ========== ENEMIGOS EN RED ==========
    private void NetworkSpawnEnemies()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0 || totalEnemies <= 0 || Runner == null || !Runner.IsServer)
            return;

        enemySpawnWorld.Clear();

        if (walkableCache == null || walkableCache.Count == 0)
        {
            walkableCache = new List<Vector2Int>();
            ForEachCell((x, y) => { if (walk[x, y]) walkableCache.Add(new Vector2Int(x, y)); });
            if (walkableCache.Count == 0) return;
        }

        float maxJ = Mathf.Clamp(enemySpawnJitter, 0f, cellSize * 0.45f);

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = Mathf.Max(200, totalEnemies * 40);

        while (spawned < totalEnemies && attempts < maxAttempts)
        {
            attempts++;

            Vector2Int c = walkableCache[rng.Next(walkableCache.Count)];
            if (c == startPos || c == exitPos) continue;

            Vector3 p = GridToWorld(c.x, c.y);
            float jx = (float)(rng.NextDouble() * 2.0 - 1.0) * maxJ;
            float jz = (float)(rng.NextDouble() * 2.0 - 1.0) * maxJ;
            Vector3 world = SnapToGround(new Vector3(p.x + jx, p.y, p.z + jz));

            var enemyPrefab = enemyPrefabs[rng.Next(enemyPrefabs.Length)];
            if (enemyPrefab == null) continue;

            // Spawn EN RED (replica a todos). StateAuthority = Host
            Runner.Spawn(enemyPrefab, world, Quaternion.identity, inputAuthority: null);
            enemySpawnWorld.Add(world);
            spawned++;
        }

        if (spawned < totalEnemies)
            Debug.Log($"[Enemies] {spawned}/{totalEnemies} (ajusta parámetros si quieres más densidad).");
    }

    // ========== BFS / VECINOS ==========
    private Vector2Int FarthestFrom(Vector2Int src, out Dictionary<Vector2Int, int> distOut)
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

    private IEnumerable<Vector2Int> GetNeighbors(Vector2Int c)
    {
        for (int i = 0; i < 4; i++)
        {
            int nx = c.x + dx[i];
            int ny = c.y + dy[i];
            if (IsWalk(nx, ny)) yield return new Vector2Int(nx, ny);
        }
    }

    private Vector2Int PushInwardIfEdge(Vector2Int p)
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

    // ========== SNAP AL PISO ==========
    private Vector3 SnapToGround(Vector3 worldPos)
    {
        Vector3 origin = worldPos + Vector3.up * spawnRayHeight;
        int mask = (floorMask.value == 0) ? ~0 : floorMask.value;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, spawnRayHeight * 2f, mask))
            return new Vector3(worldPos.x, hit.point.y + spawnYOffset, worldPos.z);

        return new Vector3(worldPos.x, spawnYOffset, worldPos.z);
    }

    // ========== TALLADO / HELPERS ==========
    private void CarveRect(RectInt r)
    {
        for (int y = r.yMin; y < r.yMax; y++)
            for (int x = r.xMin; x < r.xMax; x++)
                MakeWalk(new Vector2Int(x, y));
    }

    private void FillRect(RectInt r, bool value)
    {
        for (int y = r.yMin; y < r.yMax; y++)
            for (int x = r.xMin; x < r.xMax; x++)
                if (InBounds(x, y)) walk[x, y] = value;
    }

    private void CarveRectBorder(RectInt r)
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

    private void CarveCorridorL(Vector2Int a, Vector2Int b)
    {
        LineX(a.x, b.x, a.y);
        LineY(a.y, b.y, b.x);
    }

    private void LineX(int x0, int x1, int y)
    {
        int step = x1 >= x0 ? 1 : -1;
        for (int x = x0; x != x1 + step; x += step)
            MakeWalk(new Vector2Int(x, y));
    }

    private void LineY(int y0, int y1, int x)
    {
        int step = y1 >= y0 ? 1 : -1;
        for (int y = y0; y != y1 + step; y += step)
            MakeWalk(new Vector2Int(x, y));
    }

    private void CarveLine(Vector2Int a, Vector2Int b)
    {
        if (a.x == b.x) LineY(a.y, b.y, a.x);
        else if (a.y == b.y) LineX(a.x, b.x, a.y);
        else CarveCorridorL(a, b);
    }

    private void MakeWalk(Vector2Int c)
    {
        if (InBounds(c.x, c.y)) walk[c.x, c.y] = true;
    }

    private bool InBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;
    private bool IsWalk(int x, int y) => (x >= 0 && x < width && y >= 0 && y < height) && walk[x, y];
    private Vector2Int Center(RectInt r) => new Vector2Int(r.xMin + r.width / 2, r.yMin + r.height / 2);

    private bool IntersectsAny(RectInt r, List<RectInt> list, int padding)
    {
        RectInt expanded = new RectInt(r.xMin - padding, r.yMin - padding, r.width + 2 * padding, r.height + 2 * padding);
        foreach (var o in list) if (expanded.Overlaps(o)) return true;
        return false;
    }

    private int GridDistance(Vector2Int a, Vector2Int b)
        => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    private void ForEachCell(Action<int, int> fn)
    {
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                fn(x, y);
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = rng.Next(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private Vector3 GridToWorld(int x, int y) => new Vector3(x * cellSize, 0f, y * cellSize);

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
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
    }
#endif
}
