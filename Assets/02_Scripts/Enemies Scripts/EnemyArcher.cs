
using UnityEngine;

public class EnemyArcher : EnemyBase
{
    [Header("Archer Stats")]
    public float attackRange = 12f;        // rango máximo de disparo
    public float safeDistance = 4f;        // si el player está demasiado cerca → retrocede
    public float attackCooldown = 1.5f;    // tiempo entre disparos
    public float projectileSpeed = 15f;
    public float projectileLife = 5f;
    public float projectileDamage = 10f;

    [Header("Refs")]
    public Transform bowMuzzle;           // punto de disparo (muzzle del arco)
    public GameObject projectilePrefab;   // prefab del proyectil (usar ProjectileEnemy)
    public AudioClip shootClip;           // sonido de flecha

    private float cd; // cooldown timer

    protected override void Update()
    {
        base.Update(); // chequea si está muerto

        if (!isActive || !player) return;

        LookAtPlayerFlat();

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > attackRange)
        {
            // 🔹 Si el player está lejos → acercarse
            MoveTowardsPlayer();
        }
        else if (dist < safeDistance)
        {
            // 🔹 Si el player está muy cerca → alejarse
            MoveAwayFromPlayer();
        }
        else
        {
            // 🔹 Dentro del rango ideal → atacar
            cd -= Time.deltaTime;
            if (cd <= 0f)
            {
                ShootArrow();
                cd = attackCooldown;
            }
        }
    }

    void ShootArrow()
    {
        if (!projectilePrefab || !bowMuzzle) return;

        Vector3 origin = bowMuzzle.position;
        Vector3 dir = (player.position - origin);
        dir.y = 0f;
        dir.Normalize();

        GameObject go = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(dir));

        ProjectileEnemy pr = go.GetComponent<ProjectileEnemy>();
        if (pr != null)
        {
            pr.damage = projectileDamage;
            pr.speed = projectileSpeed;
            pr.life = projectileLife;
            pr.owner = GetComponent<Collider>(); // 👉 para no dañarse a sí mismo
        }

        if (shootClip != null)
            AudioSource.PlayClipAtPoint(shootClip, transform.position);

        Debug.Log($"[EnemyArcher] {gameObject.name} disparó una flecha → daño {projectileDamage}");
    }
}
