
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Collider owner; // quién disparó

    [Header("Projectile Settings")]
    public float speed = 10f;
    public float lifeTime = 3f;
    public float damage = 10f;

    Rigidbody rb;
    Collider projectileCollider;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        projectileCollider = GetComponent<Collider>();
    }

    void Start()
    {
        // Ignorar colisión con el que disparó
        if (owner && projectileCollider)
            Physics.IgnoreCollision(projectileCollider, owner);

        rb.velocity = transform.forward * speed;

        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // Ver si es enemigo
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            if (enemy.currentHealth <= 0)
            {
                PlayerController player = FindObjectOfType<PlayerController>();
                if (player != null)
                    player.AddKill();

            }
            Destroy(gameObject);
            return;
        }

        // Si choca con algo que no sea enemigo
        if (!other.CompareTag("Player"))
        {
            Debug.Log($"[Projectile] Impactó contra {other.name} (no enemigo)");
            Destroy(gameObject);
        }
    }
}
