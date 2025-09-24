

using UnityEngine;

public class EnemyZombie : EnemyBase
{
    [Header("Melee")]
    public float attackDamage = 10f;
    public float attackRange = 1.7f;
    public float attackCooldown = 1.0f;

    [Header("Audio")]
    public AudioClip idleGroanClip;
    public float minGroanDelay = 3f;
    public float maxGroanDelay = 7f;

    private float cd;
    private float groanTimer;

    void Start()
    {
        ResetGroanTimer();
    }

    protected override void Update()
    {
        base.Update(); // 🔹 chequea muerte

        if (!isActive || !player) return;

        LookAtPlayerFlat();

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > Mathf.Max(attackRange, stopDistance))
        {
            MoveTowardsPlayer();
            return;
        }

        cd -= Time.deltaTime;
        if (cd <= 0f)
        {
            PlayerController playerCtrl = player.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                playerCtrl.TakeDamage(attackDamage);
                Debug.Log($"[{gameObject.name}] Golpeó al jugador → daño {attackDamage}");
            }

            cd = attackCooldown;
        }

        HandleGroan();
    }

    void HandleGroan()
    {
        if (idleGroanClip == null) return;

        groanTimer -= Time.deltaTime;
        if (groanTimer <= 0f)
        {
            AudioSource.PlayClipAtPoint(idleGroanClip, transform.position);
            ResetGroanTimer();
        }
    }

    void ResetGroanTimer()
    {
        groanTimer = Random.Range(minGroanDelay, maxGroanDelay);
    }
}
