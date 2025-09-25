using Fusion;
using UnityEngine;

public class PlayerShooter : NetworkBehaviour
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
        // Evita ejecutar en todos: solo el dueño del input dispara
        if (!Object.HasInputAuthority) return;

        if (player == null) return;
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
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_Shoot(Vector3 pos, Quaternion rot)
    {
        Runner.Spawn(bulletPrefab.GetComponent<NetworkObject>(), pos, rot, Object.InputAuthority);
    }

    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        if (!Runner.IsServer && Runner.GameMode != GameMode.Shared)
        {
            // Envía un mensaje RPC o notifica al Host para disparar
            RPC_Shoot(firePoint.position, firePoint.rotation);
            return;
        }

        // El Host spawnea la bala
        Runner.Spawn(bulletPrefab.GetComponent<NetworkObject>(),
                     firePoint.position,
                     firePoint.rotation,
                     Object.InputAuthority);
    }

}
