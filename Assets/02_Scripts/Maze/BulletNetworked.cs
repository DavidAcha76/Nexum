using Fusion;
using UnityEngine;

public class BulletNetworked : NetworkBehaviour
{
    [Header("Stats")]
    public float speed = 20f;
    public float lifeTime = 3f;
    public float damage = 10f;

    private float lifeTimer;

    public override void FixedUpdateNetwork()
    {
        // Movimiento basado en tiempo de simulación
        transform.position += transform.forward * speed * Runner.DeltaTime;

        lifeTimer += Runner.DeltaTime;
        if (lifeTimer >= lifeTime)
        {
            Runner.Despawn(Object);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        // Si impacta un jugador/enemigo con Health
        var health = other.GetComponent<PlayerController>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }

        Runner.Despawn(Object); // destruye la bala para todos
    }
}
