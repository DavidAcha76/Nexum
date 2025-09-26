
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

        // 👇 Ya no se toca vida, defensa ni velocidad.
        // Esos valores vienen directo de EnemyBase (Inspector).
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

        // 🔹 Posición tentativa (a la derecha del enemigo)
        Vector3 spawnPos = transform.position + transform.right * 2f;

        // 🔹 Ajuste al suelo con raycast
        Ray ray = new Ray(spawnPos + Vector3.up * 5f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 10f))
        {
            spawnPos.y = hit.point.y + 0.1f;
        }

        Instantiate(clonePrefab, spawnPos, transform.rotation);
    }
}
