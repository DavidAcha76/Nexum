
using UnityEngine;

public class ProjectileSimple : MonoBehaviour
{
    [Header("Config")]
    public float speed = 10f;
    public float life = 3f;
    public float damage = 10f;
    public bool onlyDamageEnemies = false;

    [HideInInspector] public Collider owner; // 👉 quién disparó la bala

    void Start()
    {
        Collider myCol = GetComponent<Collider>();

        // ✅ Ignorar colisión con el dueño
        if (owner != null && myCol != null)
            Physics.IgnoreCollision(myCol, owner);

        // ✅ Ignorar colisiones entre proyectiles enemigos
        ProjectileSimple[] allProjectiles = FindObjectsOfType<ProjectileSimple>();
        foreach (var proj in allProjectiles)
        {
            if (proj == this) continue;
            Collider otherCol = proj.GetComponent<Collider>();
            if (otherCol != null && myCol != null)
                Physics.IgnoreCollision(myCol, otherCol);
        }
    }

    void Update()
    {
        // Mover proyectil
        transform.position += transform.forward * speed * Time.deltaTime;

        // Reducir vida del proyectil
        life -= Time.deltaTime;
        if (life <= 0f) Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        // ❌ Ignorar al dueño
        if (owner != null && other == owner)
        {
            Debug.Log($"[ProjectileSimple] ⛔ Ignoró a su dueño ({other.name})");
            return;
        }

        // ✅ Impactar al Player
        var pc = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        if (pc != null)
        {
            pc.TakeDamage(damage);
            Debug.Log($"[ProjectileSimple] ✅ Impactó al Player → daño: {damage}, HP restante: {pc.Health}");
            Destroy(gameObject);
            return;
        }

        // ✅ Evitar que enemigos se maten entre sí
        if (onlyDamageEnemies && other.CompareTag("Enemy"))
        {
            Debug.Log($"[ProjectileSimple] ⛔ Impactó contra enemigo {other.name}, ignorado.");
            return;
        }

        // ❌ Colisión con algo que no es Player ni enemigo
        Debug.Log($"[ProjectileSimple] ❌ No impactó al Player (colisión con {other.name})");
        Destroy(gameObject);
    }
}
