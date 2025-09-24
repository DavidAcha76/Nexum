
using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    [Header("Config")]
    public string enemyTag = "Enemy";
    public float shootRange = 8f;
    public float autoShootDelay = 0.8f;

    [Header("Refs")]
    public Transform firePoint;
    public GameObject bulletPrefab;
    public PlayerController player;

    private Animator animator;
    private float shootTimer;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (player == null) return;

        // 🚫 No disparar si está en ultimate
        if (player.IsUsingUltimate) return;

        shootTimer -= Time.deltaTime;

        Vector2 moveInput = player.moveJoystick ? player.moveJoystick.Direction : Vector2.zero;
        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        if (isMoving) return;

        GameObject target = FindClosestEnemy();
        if (target == null) return;

        Vector3 dir = (target.transform.position - firePoint.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir, Vector3.up);
            firePoint.rotation = lookRot;
            transform.rotation = lookRot;
        }

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

        if (animator != null)
            animator.SetTrigger("Shoot"); // 🔹 dispara animación

        Debug.Log("[AutoShoot] Disparo automático hacia enemigo");
    }
}
