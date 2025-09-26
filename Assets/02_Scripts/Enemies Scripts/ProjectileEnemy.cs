
using UnityEngine;

public class ProjectileEnemy : MonoBehaviour
{
    [Header("Config")]
    public float speed = 12f;
    public float life = 4f;
    public float damage = 10f;

    [Header("Owner")]
    public Collider owner; // 👉 para evitar que choque con el que disparó

    private float timer;

    void Update()
    {
        // 🔹 mover hacia adelante siempre
        transform.position += transform.forward * speed * Time.deltaTime;

        // 🔹 destruir después de un tiempo
        timer += Time.deltaTime;
        if (timer >= life) Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        // 👉 ignorar al que disparó
        if (owner != null && other == owner) return;

        // 🔹 solo daña objetos con tag "Player"
        if (other.CompareTag("Player"))
        {
            var pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(damage);
                Debug.Log($"[ProjectileEnemy] {other.name} recibió {damage} daño.");
            }
            Destroy(gameObject);
        }
    }
}
