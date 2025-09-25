using System;

using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("Refs")]
    public Transform player; // objetivo

    [Header("Stats")]
    public float moveSpeed = 4f;
    public float detectionRange = 15f;
    public float stopDistance = 1.6f;

    [Header("Combat")]
    public float maxHealth = 100f;
    public float currentHealth;
    [Range(0f, 1f)] public float damageReduction = 0f; // 0 = sin reducción, 1 = invulnerable

    protected CharacterController cc;
    protected Rigidbody rb;
    protected bool isActive = true;

    [Header("Drops")]
    public GameObject coinPrefab;
    public float coinDropChance = 1f; // 1 = 100%, 0.5 = 50%

    [Space(5)]
    public GameObject[] powerupPrefabs; // 👉 aquí arrastras tus 5 prefabs
    [Range(0f, 1f)] public float powerupDropChance = 0.3f; // 0.3 = 30%


    // === Inicialización ===
    protected virtual void Awake()
    {
        cc = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        currentHealth = maxHealth;
    }

    protected virtual void Update()
    {
        if (isActive && currentHealth <= 0f)
        {
            OnDeath();
        }
    }

    protected virtual void LateUpdate()
    {
        if (!isActive) return;

        if (!player || Time.frameCount % 15 == 0)
            player = FindClosestPlayer();
    }

    // === Vida y Daño ===
    public virtual void TakeDamage(float amount)
    {
        if (!isActive) return;

        float reduced = amount * (1f - damageReduction);
        currentHealth -= Mathf.Abs(reduced);

        Debug.Log($"[{gameObject.name}] Recibió {reduced} daño → HP restante: {currentHealth}");

        if (currentHealth <= 0f)
            OnDeath();
    }

    protected virtual void OnDeath()
    {
        isActive = false;

        Debug.Log($"[{gameObject.name}] murió");

        // 💰 Drop de monedas
        if (coinPrefab && UnityEngine.Random.value <= coinDropChance)
        {
            Instantiate(coinPrefab, transform.position + Vector3.up * 0.1f, Quaternion.identity);
        }

        // ⭐ Drop de Powerup
        if (powerupPrefabs.Length > 0 && UnityEngine.Random.value <= powerupDropChance)
        {
            int index = UnityEngine.Random.Range(0, powerupPrefabs.Length);
            GameObject prefab = powerupPrefabs[index];
            if (prefab != null)
            {
                Instantiate(prefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                Debug.Log($"[{gameObject.name}] soltó powerup: {prefab.name}");
            }
        }

        Destroy(gameObject);
    }



    // === Utilidades de movimiento y target ===
    protected Transform FindClosestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform best = null;
        float bestSqr = float.PositiveInfinity;

        foreach (var go in players)
        {
            if (!go) continue;
            float d2 = (go.transform.position - transform.position).sqrMagnitude;
            if (d2 < bestSqr)
            {
                bestSqr = d2;
                best = go.transform;
            }
        }

        if (best && detectionRange > 0f)
        {
            float d = Vector3.Distance(transform.position, best.position);
            if (d > detectionRange) best = null;
        }

        return best;
    }

    protected void LookAtPlayerFlat()
    {
        if (!player) return;
        Vector3 to = player.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(to);
    }

    protected void MoveTowardsPlayer()
    {
        if (!player) return;
        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;

        if (cc) cc.Move(dir.normalized * moveSpeed * Time.deltaTime);
        else transform.position += dir.normalized * moveSpeed * Time.deltaTime;
    }

    protected void MoveAwayFromPlayer()
    {
        if (!player) return;
        Vector3 dir = (transform.position - player.position);
        dir.y = 0f;

        if (cc) cc.Move(dir.normalized * moveSpeed * Time.deltaTime);
        else transform.position += dir.normalized * moveSpeed * Time.deltaTime;
    }

    protected bool PlayerInRange(float range)
    {
        if (!player) return false;
        return Vector3.Distance(transform.position, player.position) <= range;
    }
}
