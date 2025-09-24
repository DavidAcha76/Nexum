using System;   // necesario para Action<>
using System.Collections;
using UnityEngine;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("Refs")]
    public GridRoomGenerator grid;
    public GameObject goalPrefab;

    [Header("Goal Visual")]
    public float goalScale = 0.3f;
    public float goalSizeCells = 0.8f;
    public float goalMinMeters = 0.06f;
    public float goalLiftMeters = 0.01f;

    [Header("Stats base y bonus")]
    public float baseMaxHealth = 100f;
    public float maxHealthBonus = 0f;

    [Header("Spawn")]
    public float spawnInsetCells = 0.35f;
    public bool spawnAtStart = true;

    [Header("Raycast piso")]
    public LayerMask floorMask = 0;
    public float floorRaycastUp = 2f;
    public float floorRaycastDown = 6f;

    [Header("Player Scale")]
    public bool autoScalePlayer = true;
    public float referenceCellSize = 0.2f;
    public Vector3 playerScaleAtReference = Vector3.one;
    public float minPlayerHeightMeters = 0.2f;
    public float minPlayerScaleFactor = 0.2f;

    [Header("Respawn")]
    public float respawnFreezeSeconds = 1f;

    // Estado
    float savedHealth;
    PlayerController player;
    GameObject goalInstance;
    int levelIndex = 1;
    bool _colliderInit;
    float _ccHeight0, _ccRadius0; Vector3 _ccCenter0;
    float _capHeight0, _capRadius0; Vector3 _capCenter0;
    bool _cooldownPending = false;

    public event Action<PlayerController> OnPlayerSpawned;
    public PlayerController CurrentPlayer => player;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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
        PlaceGoal();
        ApplyPlayerScale();
        if (player != null)
        {
            TeleportPlayerToSpawn(player);
            if (_cooldownPending)
            {
                _cooldownPending = false;
                StartCoroutine(RespawnCooldown());
            }
        }
    }

    public void RegisterPlayer(PlayerController pm)
    {
        if (!pm) return;
        player = pm;

        float maxH = baseMaxHealth + maxHealthBonus;

        // inicializamos salud
        if (savedHealth <= 0f)
        {
            player.Heal(maxH); // llenamos vida al máximo
            savedHealth = maxH;
        }
        else
        {
            player.Heal(savedHealth - player.Health); // restauramos la que tenía
        }

        InitPlayerColliderDefaults();
        ApplyPlayerScale();
        TeleportPlayerToSpawn(player);
        OnPlayerSpawned?.Invoke(player);
    }

    void InitPlayerColliderDefaults()
    {
        if (_colliderInit || player == null) return;

        var cc = player.GetComponent<CharacterController>();
        if (cc) { _ccHeight0 = cc.height; _ccRadius0 = cc.radius; _ccCenter0 = cc.center; }

        var cap = player.GetComponent<CapsuleCollider>();
        if (cap) { _capHeight0 = cap.height; _capRadius0 = cap.radius; _capCenter0 = cap.center; }

        _colliderInit = true;
    }

    void ApplyPlayerScale()
    {
        if (!autoScalePlayer || player == null || grid == null) return;

        float f = Mathf.Max(0.0001f, grid.cellSize / Mathf.Max(0.0001f, referenceCellSize));
        f = Mathf.Max(f, minPlayerScaleFactor);

        player.transform.localScale = Vector3.Scale(playerScaleAtReference, Vector3.one * f);

        var cc = player.GetComponent<CharacterController>();
        if (cc)
        {
            float targetH = Mathf.Max(_ccHeight0 * f, minPlayerHeightMeters);
            float targetR = Mathf.Max(_ccRadius0 * f, minPlayerHeightMeters * 0.25f);
            cc.height = targetH; cc.radius = targetR;
            cc.center = new Vector3(_ccCenter0.x, _ccCenter0.y * f, _ccCenter0.z);
        }
        var cap = player.GetComponent<CapsuleCollider>();
        if (cap)
        {
            float targetH = Mathf.Max(_capHeight0 * f, minPlayerHeightMeters);
            float targetR = Mathf.Max(_capRadius0 * f, minPlayerHeightMeters * 0.25f);
            cap.height = targetH; cap.radius = targetR;
            cap.center = new Vector3(_capCenter0.x, _capCenter0.y * f, _capCenter0.z);
        }
    }

    public void OnReachGoal()
    {
        if (!grid || !player) return;

        savedHealth = player.Health;

        levelIndex++;
        Debug.Log($"[RunManager] Rebuild level #{levelIndex}");

        _cooldownPending = true;
        grid.Rebuild();
    }

    public void AddMaxHealth(float amount)
    {
        maxHealthBonus += Mathf.Max(0f, amount);
    }

    void TeleportPlayerToSpawn(PlayerController p)
    {
        if (!grid || !p) return;

        Vector3 pos; Quaternion rot;

        if (spawnAtStart)
        {
            pos = grid.GetStartSpawnWorldPosInside(spawnInsetCells);
            rot = Quaternion.LookRotation(grid.transform.forward, grid.transform.up);
        }
        else
        {
            if (!grid.TryGetGoalSpawnInside(out pos, out rot, spawnInsetCells))
            {
                pos = grid.GetStartSpawnWorldPosInside(spawnInsetCells);
                rot = Quaternion.LookRotation(grid.transform.forward, grid.transform.up);
            }
        }

        try { grid.ClampWorldPointInsideMapCells(ref pos, 0.15f); } catch { }

        float upOffset = GetStandingOffset(p.gameObject);
        Vector3 rayStart = pos + Vector3.up * floorRaycastUp;
        float rayDist = floorRaycastUp + floorRaycastDown;

        if (Physics.Raycast(rayStart, Vector3.down, out var hit, rayDist, floorMask, QueryTriggerInteraction.Ignore))
            pos = hit.point + Vector3.up * upOffset;
        else
            pos = pos + Vector3.up * upOffset;

        p.transform.SetPositionAndRotation(pos, rot);

        var rb = p.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }
    }

    float GetStandingOffset(GameObject go)
    {
        var cap = go.GetComponent<CapsuleCollider>();
        if (cap) return Mathf.Max(cap.height * 0.5f, cap.radius) + 0.01f;
        var cc = go.GetComponent<CharacterController>();
        if (cc) return cc.height * 0.5f + 0.01f;
        return 0.9f;
    }

    void PlaceGoal()
    {
        if (!goalPrefab || !grid) return;

        Vector3 pos; Quaternion rot;
        if (!grid.TryGetGoalSpawnInside(out pos, out rot, 0.20f))
        {
            pos = grid.GetStartSpawnWorldPosInside(0.20f);
            rot = Quaternion.LookRotation(grid.transform.forward, Vector3.up);
        }

        try { grid.ClampWorldPointInsideMapCells(ref pos, 0.2f); } catch { }

        Vector3 rayStart = pos + Vector3.up * floorRaycastUp;
        float rayDist = floorRaycastUp + floorRaycastDown;
        bool hitFloor = Physics.Raycast(rayStart, Vector3.down, out var hit, rayDist, floorMask, QueryTriggerInteraction.Ignore);

        if (hitFloor)
            pos = hit.point + Vector3.up * goalLiftMeters;
        else
        {
            float t = Mathf.Max(0.01f, grid.cellSize * 0.05f);
            float floorTopY = grid.parent.position.y + t * 0.5f;
            pos.y = floorTopY + goalLiftMeters;
        }

        if (!goalInstance) goalInstance = Instantiate(goalPrefab, pos, rot, grid.parent);
        else
        {
            goalInstance.transform.SetParent(grid.parent, true);
            goalInstance.transform.SetPositionAndRotation(pos, rot);
        }

        if (goalSizeCells > 0f)
        {
            float s = Mathf.Max(goalMinMeters, grid.cellSize * goalSizeCells);
            goalInstance.transform.localScale = new Vector3(s, s, s);
        }
        else goalInstance.transform.localScale = Vector3.one * Mathf.Max(0.01f, goalScale);

        var goal = goalInstance.GetComponent<LevelGoal>() ?? goalInstance.AddComponent<LevelGoal>();
        var col = goalInstance.GetComponent<Collider>() ?? goalInstance.AddComponent<SphereCollider>();
        col.isTrigger = true;
        var rb = goalInstance.GetComponent<Rigidbody>() ?? goalInstance.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        var rend = goalInstance.GetComponentInChildren<Renderer>();
        if (rend)
        {
            Color c = Color.HSVToRGB(Mathf.Repeat(levelIndex * 0.17f, 1f), 0.7f, 1f);
            if (rend.material) rend.material.color = c;
        }
    }

    IEnumerator RespawnCooldown()
    {
        if (player == null) yield break;

        var rb = player.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        bool prevEnabled = player.enabled;
        player.enabled = false;

        yield return new WaitForSeconds(respawnFreezeSeconds);

        player.enabled = prevEnabled;
    }
}
