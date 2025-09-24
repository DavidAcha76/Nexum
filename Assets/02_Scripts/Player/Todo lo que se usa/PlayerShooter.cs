
using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    [Header("Config")]
    public string enemyTag = "Enemy";   // 👉 tag de los enemigos
    public float shootRange = 8f;       // rango de detección
    public float autoShootDelay = 0.8f; // segundos entre disparos automáticos

    [Header("Refs")]
    public Transform firePoint;
    public GameObject bulletPrefab;
    public PlayerController player;

    private float shootTimer;

    void Update()
    {
        if (player == null) return;

        shootTimer -= Time.deltaTime;

        // 🔹 Verifica si el player está en movimiento con el joystick principal
        Vector2 moveInput = player.moveJoystick ? player.moveJoystick.Direction : Vector2.zero;
        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        if (isMoving) return;

        // 🔹 Buscar enemigo más cercano en rango
        GameObject target = FindClosestEnemy();
        if (target == null) return;

        // 🔹 Apuntar hacia el enemigo
        Vector3 dir = (target.transform.position - firePoint.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir, Vector3.up);
            firePoint.rotation = lookRot;
            transform.rotation = lookRot;
        }

        // 🔹 Disparar automáticamente
        if (shootTimer <= 0f)
        {
            Shoot();
            shootTimer = player.AttackSpeed > 0 ? 1f / player.AttackSpeed : autoShootDelay;
        }
    }

    GameObject FindClosestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        GameObject closest = null;
        float minDist = shootRange;

        foreach (var e in enemies)
        {
            if (e == null) continue;
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d < minDist)
            {
                minDist = d;
                closest = e;
            }
        }
        return closest;
    }

    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Debug.Log("[AutoShoot] Disparo automático hacia enemigo");
    }
}
