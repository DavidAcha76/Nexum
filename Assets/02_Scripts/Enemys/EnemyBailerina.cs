
using UnityEngine;

public class EnemyBailerina : EnemyBase
{
    [Header("Melee pesado")]
    public float attackDamage = 22f;
    public float attackRange = 2.0f;
    public float attackCooldown = 1.2f;
    protected Animator animator;

    [Header("Special Attack")]
    public GameObject clonePrefab;
    public float specialCooldown = 4f;
    private float cdSpecial = 0f;

    private float cd;
    private float chaseTimer = 0f;

    [Header("Audio")]
    public AudioClip specialAttackSfx;

    protected virtual void Start()
    {
        animator = GetComponent<Animator>();

        // 🔹 Stats especiales de este enemigo
        maxHealth = Mathf.Max(maxHealth, 220f);
        currentHealth = maxHealth;
        damageReduction = Mathf.Max(damageReduction, 0.4f);

        moveSpeed = Mathf.Min(moveSpeed, 2.6f);
    }

    protected override void Update()
    {
        base.Update(); // 🔹 chequea muerte

        if (!isActive || !player) return;

        LookAtPlayerFlat();

        if (cdSpecial > 0f) cdSpecial -= Time.deltaTime;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > Mathf.Max(attackRange, stopDistance))
        {
            MoveTowardsPlayer();
            chaseTimer += Time.deltaTime;

            if (cdSpecial <= 0f)
            {
                DoSpecialAttack();
                cdSpecial = specialCooldown;
            }
            return;
        }

        cd -= Time.deltaTime;
        if (cd <= 0f)
        {
            var playerCtrl = player.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                playerCtrl.TakeDamage(attackDamage);
                Debug.Log($"[{gameObject.name}] Golpeó al jugador → daño {attackDamage}");
            }

            cd = attackCooldown;
        }
    }

    void DoSpecialAttack()
    {
        if (animator != null)
            animator.SetTrigger("DoAttack");

        if (specialAttackSfx != null)
            AudioSource.PlayClipAtPoint(specialAttackSfx, transform.position);

        if (!clonePrefab) return;

        Vector3 spawnPos = transform.position + transform.right * 2f;
        Instantiate(clonePrefab, spawnPos, transform.rotation);
    }
}
