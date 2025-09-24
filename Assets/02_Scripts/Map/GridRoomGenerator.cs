using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridRoomGenerator : MonoBehaviour
{
    [Header("References")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject goalPrefab;          // opcional
    public Transform parent;               // anchor donde instanciar

    [Header("Map Settings")]
    [Range(3, 300)] public int width = 51;
    [Range(3, 300)] public int depth = 51;
    public float cellSize = 1f;
    public float wallHeight = 1.5f;
    [Range(0.02f, 0.5f)] public float wallThickness = 0.15f;

    [Header("Room Settings")]
    [Range(3, 9)] public int roomSize = 3;      // impar
    [Range(4, 20)] public int roomStep = 6;
    [Range(1, 200)] public int maxRooms = 20;
    [Range(0, 200)] public int extraConnections = 12;
    [Range(1, 6)] public int border = 2;

    [Header("Random")]
    public bool useRandomSeed = true;
    public int seed = 12345;

    [Header("Goal (opcional)")]
    public bool placeGoal = false;
    public float goalInsetCells = 0.25f;

    [Header("Build")]
    public bool autoBuildOnStart = false;

    public Action OnBuilt;

    // Estado
    private bool[,] passable;
    private readonly List<GameObject> spawned = new List<GameObject>();
    private List<(int, int)> roomCenters;
    private (int x, int z) startCenter;
    private (int x, int z) goalCenter;
    private System.Random rng;


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
            Debug.LogWarning("[GridRoomGenerator] No hay parent/anchor. Llama SetAnchor() antes.");
            return;
        }

        rng = useRandomSeed ? new System.Random(Guid.NewGuid().GetHashCode())
                            : new System.Random(seed);

        if (width % 2 == 0) width++;
        if (depth % 2 == 0) depth++;

        ClearPrevious();
        passable = new bool[width, depth];
        roomCenters = new List<(int, int)>();

        GenerateRoomsAndCorridorsGrid();
        RenderFromPassable();

        if (placeGoal && goalPrefab)
        {
            if (TryGetGoalSpawnInside(out var gpos, out var grot, goalInsetCells))
            {
                var goal = Instantiate(goalPrefab, gpos, grot, parent);
                spawned.Add(goal);
            }
        }

        OnBuilt?.Invoke();
    }

    // ==== Generación ====
    private void GenerateRoomsAndCorridorsGrid()
    {
        var candidates = BuildRoomCenterCandidates();

        // mezcla candidatos para variedad
        Shuffle(candidates);

        // toma hasta maxRooms aleatorios
        int take = Mathf.Clamp(maxRooms, 1, candidates.Count);
        var selected = candidates.Take(take).ToList();
        if (selected.Count == 0) return;

        startCenter = selected[0];

        // Tallar cuartos
        foreach (var c in selected)
        {
            var (rx, rz) = c;
            CarveRoom(rx, rz);
        }

        // Grafo base: K vecinos más cercanos por cada nodo
        var edgesKNN = BuildKNNAdjacency(selected, k: 3);

        // MST aleatorio
        var mst = BuildRandomizedMST(selected, edgesKNN);

        // Llevamos una lista de aristas REALMENTE talladas para comprobar conectividad
        var carvedEdges = new List<RoomEdge>();

        foreach (var e in mst)
        {
            CarveCorridorL(e.a, e.b);
            carvedEdges.Add(e);
        }

        // Conexiones extra para loops (variedad)
        var edgePool = new List<RoomEdge>(edgesKNN);
        Shuffle(edgePool);
        int added = 0;
        foreach (var e in edgePool)
        {
            if (added >= extraConnections) break;
            if (EdgeInList(mst, e)) continue;

            CarveCorridorL(e.a, e.b);
            carvedEdges.Add(e);
            added++;
        }

        // Asegurar que TODO quede conectado (si aún quedan componentes, unirlas)
        EnsureConnectivity(selected, carvedEdges);

        // Elegir meta como el cuarto más lejano desde el inicial (sobre el passable ya tallado)
        goalCenter = ChooseFarthestRoomFrom(startCenter);
    }

    // Candidatos de centros de cuarto sobre una grilla roomStep x roomStep, respetando bordes
    private List<(int, int)> BuildRoomCenterCandidates()
    {
        var list = new List<(int, int)>();
        int half = roomSize / 2;

        int xMin = border + half;
        int xMax = width - border - half;
        int zMin = border + half;
        int zMax = depth - border - half;

        int cx = width / 2, cz = depth / 2;
        int startX = Mathf.Clamp(cx - ((cx - xMin) % roomStep), xMin, xMax);
        int startZ = Mathf.Clamp(cz - ((cz - zMin) % roomStep), zMin, zMax);

        for (int x = startX; x <= xMax; x += roomStep)
            for (int z = startZ; z <= zMax; z += roomStep)
                list.Add((x, z));

        if (list.Count == 0) list.Add((cx, cz));
        return list;
    }

    private void CarveRoom(int cx, int cz)
    {
        int half = roomSize / 2;
        for (int x = cx - half; x <= cx + half; x++)
            for (int z = cz - half; z <= cz + half; z++)
                if (Inside(x, z)) passable[x, z] = true;

        roomCenters.Add((cx, cz));
    }

    private struct RoomEdge
    {
        public (int, int) a, b;
        public RoomEdge((int, int) A, (int, int) B) { a = A; b = B; }
    }

    // --- NUEVO: K-NN por distancia Manhattan (evita islas en el grafo base) ---
    private List<RoomEdge> BuildKNNAdjacency(List<(int, int)> nodes, int k = 3)
    {
        var edges = new List<RoomEdge>();
        for (int i = 0; i < nodes.Count; i++)
        {
            var a = nodes[i];
            var ordered = nodes
                .Select((p, idx) => new
                {
                    idx,
                    p,
                    d = Mathf.Abs(p.Item1 - a.Item1) + Mathf.Abs(p.Item2 - a.Item2)
                })
                .Where(x => x.idx != i)
                .OrderBy(x => x.d)
                .Take(Mathf.Clamp(k, 1, nodes.Count - 1))
                .Select(x => x.p);

            foreach (var b in ordered)
            {
                if (a.Equals(b)) continue;
                var e = new RoomEdge(a, b);
                if (!EdgeInList(edges, e)) edges.Add(e);
            }
        }
        return edges;
    }

    // MST aleatorio (Prim-like simplificado con baraja de aristas)
    private List<RoomEdge> BuildRandomizedMST(List<(int, int)> nodes, List<RoomEdge> edges)
    {
        var inTree = new HashSet<(int, int)>();
        var result = new List<RoomEdge>();
        Shuffle(edges);

        inTree.Add(nodes[0]);

        bool progressed = true;
        while (inTree.Count < nodes.Count && progressed)
        {
            progressed = false;
            foreach (var e in edges)
            {
                bool aIn = inTree.Contains(e.a);
                bool bIn = inTree.Contains(e.b);
                if (aIn ^ bIn)
                {
                    inTree.Add(aIn ? e.b : e.a);
                    result.Add(e);
                    progressed = true;
                    break;
                }
            }
        }
        return result;
    }

    private bool EdgeInList(List<RoomEdge> list, RoomEdge e)
    {
        foreach (var it in list)
            if ((it.a.Equals(e.a) && it.b.Equals(e.b)) || (it.a.Equals(e.b) && it.b.Equals(e.a)))
                return true;
        return false;
    }

    // --- NUEVO: forzar conectividad enlazando componentes restantes ---
    private void EnsureConnectivity(List<(int, int)> nodes, List<RoomEdge> carvedEdges)
    {
        if (nodes.Count <= 1) return;

        // Mapea centros a índices
        var id = new Dictionary<(int, int), int>();
        for (int i = 0; i < nodes.Count; i++) id[nodes[i]] = i;

        // Union-Find
        int n = nodes.Count;
        int[] parentUF = new int[n];
        for (int i = 0; i < n; i++) parentUF[i] = i;
        int Find(int x) => parentUF[x] == x ? x : (parentUF[x] = Find(parentUF[x]));
        void Union(int a, int b) { a = Find(a); b = Find(b); if (a != b) parentUF[b] = a; }

        // Une lo ya tallado
        foreach (var e in carvedEdges) Union(id[e.a], id[e.b]);

        // Mientras haya >1 componente, enlaza el par más cercano entre componentes
        while (true)
        {
            var groups = new Dictionary<int, List<int>>();
            for (int i = 0; i < n; i++)
            {
                int r = Find(i);
                if (!groups.ContainsKey(r)) groups[r] = new List<int>();
                groups[r].Add(i);
            }
            if (groups.Count <= 1) break;

            float best = float.MaxValue;
            int ai = -1, bi = -1;
            var reps = new List<int>(groups.Keys);

            for (int g1 = 0; g1 < reps.Count; g1++)
                for (int g2 = g1 + 1; g2 < reps.Count; g2++)
                {
                    foreach (var i in groups[reps[g1]])
                        foreach (var j in groups[reps[g2]])
                        {
                            var A = nodes[i]; var B = nodes[j];
                            float d = Mathf.Abs(A.Item1 - B.Item1) + Mathf.Abs(A.Item2 - B.Item2);
                            if (d < best) { best = d; ai = i; bi = j; }
                        }
                }

            if (ai < 0 || bi < 0) break;

            var a = nodes[ai];
            var b = nodes[bi];

            CarveCorridorL(a, b);
            carvedEdges.Add(new RoomEdge(a, b));
            Union(ai, bi);
        }
    }

    // Corredor en L con orden aleatorio (h->v o v->h)
    private void CarveCorridorL((int, int) a, (int, int) b)
    {
        var (ax, az) = a;
        var (bx, bz) = b;

        bool horizFirst = rng.NextDouble() < 0.5;

        if (horizFirst)
        {
            for (int x = Math.Min(ax, bx); x <= Math.Max(ax, bx); x++)
                if (Inside(x, az)) passable[x, az] = true;

            for (int z = Math.Min(az, bz); z <= Math.Max(az, bz); z++)
                if (Inside(bx, z)) passable[bx, z] = true;
        }
        else
        {
            for (int z = Math.Min(az, bz); z <= Math.Max(az, bz); z++)
                if (Inside(ax, z)) passable[ax, z] = true;

            for (int x = Math.Min(ax, bx); x <= Math.Max(ax, bx); x++)
                if (Inside(x, bz)) passable[x, bz] = true;
        }
    }

    private (int x, int z) ChooseFarthestRoomFrom((int x, int z) srcCenter)
    {
        var dist = new Dictionary<(int, int), int>();
        var q = new Queue<(int, int)>();
        dist[srcCenter] = 0;
        q.Enqueue(srcCenter);

        var DIR4 = new (int dx, int dz)[] { (0, 1), (1, 0), (0, -1), (-1, 0) };

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            foreach (var d in DIR4)
            {
                var nxt = (cur.Item1 + d.dx, cur.Item2 + d.dz);
                if (!Inside(nxt.Item1, nxt.Item2) || !passable[nxt.Item1, nxt.Item2]) continue;
                if (dist.ContainsKey(nxt)) continue;
                dist[nxt] = dist[cur] + 1;
                q.Enqueue(nxt);
            }
        }

        (int, int) best = srcCenter;
        int bestD = -1;
        foreach (var rc in roomCenters)
            if (dist.TryGetValue(rc, out int d) && d > bestD) { bestD = d; best = rc; }

        return best;
    }

    private bool Inside(int x, int z) => x >= 0 && x < width && z >= 0 && z < depth;

    // ==== Render (centrado en el anchor) ====
    private void RenderFromPassable()
    {
        if (!parent) parent = transform;

        for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
            {
                if (!passable[x, z]) continue;

                // Piso en el centro de la celda, respetando rotación del anchor
                var floor = Instantiate(floorPrefab, CellCenterToWorld(x, z), parent.rotation, parent);
                floor.transform.localScale = new Vector3(cellSize, Mathf.Max(0.01f, cellSize * 0.05f), cellSize);
                spawned.Add(floor);

                TryPlaceWallIfNotPassable(x, z, Dir.North);
                TryPlaceWallIfNotPassable(x, z, Dir.East);
                TryPlaceWallIfNotPassable(x, z, Dir.South);
                TryPlaceWallIfNotPassable(x, z, Dir.West);
            }
    }

    private void TryPlaceWallIfNotPassable(int x, int z, Dir d)
    {
        int nx = x + DX(d), nz = z + DZ(d);
        if (Inside(nx, nz) && passable[nx, nz]) return;

        var h = Mathf.Max(0.01f, wallHeight);
        var th = Mathf.Max(0.01f, cellSize * wallThickness);

        // base en centro de celda + offset medio-celda hacia el borde
        Vector3 basePos = CellCenterToWorld(x, z);
        Vector3 edgeOffset = parent.right * (DX(d) * (cellSize * 0.5f)) +
                             parent.forward * (DZ(d) * (cellSize * 0.5f));

        var wall = Instantiate(wallPrefab, basePos + edgeOffset + parent.up * (h * 0.5f), parent.rotation, parent);
        wall.transform.localScale = new Vector3(cellSize, h, th);
        wall.transform.rotation = Quaternion.AngleAxis(Angle(d), Vector3.up) * parent.rotation;

        spawned.Add(wall);
    }

    // Origen centrado: el anchor (tap) es el centro del mapa
    private Vector3 GetOriginCentered()
    {
        float totalX = width * cellSize;
        float totalZ = depth * cellSize;
        return parent.position
             - parent.right * (totalX * 0.5f)
             - parent.forward * (totalZ * 0.5f);
    }

    // Centro de la celda (x,z) desde ese origen
    private Vector3 CellCenterToWorld(int x, int z)
    {
        var origin = GetOriginCentered();
        return origin
             + parent.right * (x * cellSize + cellSize * 0.5f)
             + parent.forward * (z * cellSize + cellSize * 0.5f);
    }

    // Mantener la firma usada en otras partes (redirige al centro de celda)
    private Vector3 ToWorld(int x, int z) => CellCenterToWorld(x, z);

    // ==== Spawns (consistentes con parent/origen centrado) ====
    public Vector3 GetStartSpawnWorldPosInside(float insetCells = 0.35f)
    {
        var c = CellCenterToWorld(startCenter.x, startCenter.z);
        return c + parent.forward * (cellSize * insetCells);
    }
    public Vector3 GetGoalSpawnWorldPosInside(float insetCells = 0.35f)
    {
        var c = CellCenterToWorld(goalCenter.x, goalCenter.z);
        return c - parent.forward * (cellSize * insetCells);
    }
    public bool TryGetGoalSpawnInside(out Vector3 pos, out Quaternion rot, float insetCells = 0.35f)
    {
        pos = GetGoalSpawnWorldPosInside(insetCells);
        rot = Quaternion.LookRotation(parent.forward, parent.up);
        return true;
    }

    // ==== Utils ====
    private enum Dir { North, East, South, West }
    private int DX(Dir e) => (e == Dir.East ? 1 : e == Dir.West ? -1 : 0);
    private int DZ(Dir e) => (e == Dir.North ? 1 : e == Dir.South ? -1 : 0);
    private float Angle(Dir e) => (e == Dir.North ? 0f : e == Dir.East ? 90f : e == Dir.South ? 180f : 270f);

    private void Shuffle<T>(IList<T> list)
    {
        if (list == null || list.Count <= 1) return;
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void ClearPrevious()
    {
        for (int i = spawned.Count - 1; i >= 0; i--)
            if (spawned[i]) Destroy(spawned[i]);
        spawned.Clear();
    }

    // Devuelve el tamaño total del mapa (X,Z) en metros
    public Vector2 GetMapSize()
    {
        return new Vector2(width * cellSize, depth * cellSize);
    }

    // Clampa un punto de mundo al rectángulo del mapa, con margen (en metros)
    public void ClampWorldPointInsideMap(ref Vector3 worldPos, float insetMeters = 0.0f)
    {
        if (!parent) parent = transform;

        var size = GetMapSize();
        float halfX = size.x * 0.5f;
        float halfZ = size.y * 0.5f;

        Vector3 local = parent.InverseTransformPoint(worldPos);
        local.x = Mathf.Clamp(local.x, -halfX + insetMeters, halfX - insetMeters);
        local.z = Mathf.Clamp(local.z, -halfZ + insetMeters, halfZ - insetMeters);
        worldPos = parent.TransformPoint(local);
    }

    // Versión en celdas
    public void ClampWorldPointInsideMapCells(ref Vector3 worldPos, float insetCells = 0.25f)
    {
        ClampWorldPointInsideMap(ref worldPos, insetCells * Mathf.Max(0.0f, cellSize));
    }

    // === GridRoomGenerator (helpers de muestreo sobre el piso) ===

    // Devuelve el origen centrado (útil si lo necesitas fuera)
    public Vector3 GetOriginCenteredPublic()
    {
        float totalX = width * cellSize;
        float totalZ = depth * cellSize;
        return parent.position
             - parent.right * (totalX * 0.5f)
             - parent.forward * (totalZ * 0.5f);
    }

    // ¿La celda (x,z) es caminable?
    public bool IsWalkableCell(int x, int z)
    {
        if (passable == null) return false;
        if (x < 0 || x >= width || z < 0 || z >= depth) return false;
        return passable[x, z];
    }

    // Centro mundial de una celda válida
    public Vector3 CellCenterToWorldPublic(int x, int z) => CellCenterToWorld(x, z);

    // Intenta muestrear un punto de piso aleatorio (centro de celda walkable)
    // insetCells: evita bordes (0.2 = 20% de la celda hacia adentro)
    public bool TrySampleAnyFloorPoint(out Vector3 worldPos, float insetCells = 0.2f)
    {
        worldPos = default;
        if (passable == null) return false;

        // recopila todas las celdas walkable
        var pool = new System.Collections.Generic.List<(int x, int z)>();
        for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
                if (passable[x, z]) pool.Add((x, z));

        if (pool.Count == 0) return false;

        var idx = UnityEngine.Random.Range(0, pool.Count);
        var c = pool[idx];

        // centro y pequeño jitter dentro de la celda
        Vector3 center = CellCenterToWorld(c.x, c.z);
        float inset = Mathf.Clamp01(insetCells) * cellSize * 0.5f; // radio seguro
        float jx = UnityEngine.Random.Range(-inset, inset);
        float jz = UnityEngine.Random.Range(-inset, inset);
        worldPos = center + (parent ? parent.right : Vector3.right) * jx + (parent ? parent.forward : Vector3.forward) * jz;

        return true;
    }

    /// <summary>
    /// Devuelve un punto de piso dentro de una habitación aleatoria (no pasillos),
    /// respetando un padding en celdas para evitar muros.
    /// </summary>
    public bool TrySampleRoomFloorPoint(out Vector3 pos, float paddingCells = 0.25f)
    {
        pos = default;
        if (roomCenters == null || roomCenters.Count == 0) return false;

        // elige una habitación al azar
        var rc = roomCenters[UnityEngine.Random.Range(0, roomCenters.Count)];
        int half = roomSize / 2;

        // rango interno de la habitación (dejando padding)
        int pad = Mathf.FloorToInt(Mathf.Clamp(paddingCells, 0f, half - 0.5f));
        int xMin = Mathf.Max(rc.Item1 - half + pad, 0);
        int xMax = Mathf.Min(rc.Item1 + half - pad, width - 1);
        int zMin = Mathf.Max(rc.Item2 - half + pad, 0);
        int zMax = Mathf.Min(rc.Item2 + half - pad, depth - 1);

        // intenta algunas veces una celda caminable dentro de ese rectángulo
        for (int i = 0; i < 20; i++)
        {
            int x = UnityEngine.Random.Range(xMin, xMax + 1);
            int z = UnityEngine.Random.Range(zMin, zMax + 1);
            if (!Inside(x, z)) continue;
            if (!passable[x, z]) continue;

            // centro de celda -> mundo
            Vector3 c = ToWorld(x, z);

            // aplica un pequeño jitter dentro de la celda para no apilar sobre el centro exacto
            float inset = Mathf.Clamp01(paddingCells) * cellSize;
            float jitter = Mathf.Max(0f, (cellSize * 0.5f) - inset);
            Vector3 right = parent ? parent.right : Vector3.right;
            Vector3 fwd = parent ? parent.forward : Vector3.forward;
            c += right * UnityEngine.Random.Range(-jitter, jitter);
            c += fwd * UnityEngine.Random.Range(-jitter, jitter);

            pos = c;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Punto de piso en cualquier celda caminable (habitaciones o pasillos).
    /// </summary>
    public bool TrySampleFloorPoint(out Vector3 pos, float paddingCells = 0.25f)
    {
        pos = default;
        if (passable == null) return false;

        // intenta varias veces encontrar una celda caminable
        for (int i = 0; i < 50; i++)
        {
            int x = UnityEngine.Random.Range(0, width);
            int z = UnityEngine.Random.Range(0, depth);
            if (!Inside(x, z)) continue;
            if (!passable[x, z]) continue;

            Vector3 c = ToWorld(x, z);

            float inset = Mathf.Clamp01(paddingCells) * cellSize;
            float jitter = Mathf.Max(0f, (cellSize * 0.5f) - inset);
            Vector3 right = parent ? parent.right : Vector3.right;
            Vector3 fwd = parent ? parent.forward : Vector3.forward;
            c += right * UnityEngine.Random.Range(-jitter, jitter);
            c += fwd * UnityEngine.Random.Range(-jitter, jitter);

            pos = c;
            return true;
        }
        return false;
    }

}
