
using System.Collections;
using UnityEngine;

public class Boss : EnemyBase
{
    [Header("Boss Stats")]
    public float bossMaxHP = 300f;

    [Header("Animation")]
    public Animator animator;

    [Header("Audio")]
    public AudioClip specialAttackClip;

    [Header("Refs")]
    public Transform muzzle; // 👉 punto exacto donde salen las balas
    public GameObject projectilePrefab;

    [Header("Movement")]
    public float preferMinDist = 6f;
    public float preferMaxDist = 12f;
    public float approachSpeed = 3f;

    [Header("Attack")]
    public float chargeTime = 1.5f;
    public int starBulletCount = 12;
    public int shotgunCount = 5;
    public float shotgunSpread = 15f;
    public int metralletaCount = 3;
    public float metralletaDelay = 0.2f;
    public float bulletSpeed = 14f;
    public float bulletLife = 4f;
    public float bulletDamage = 12f;

    bool isCharging = false;

    protected override void Awake()
    {
        base.Awake();
        maxHealth = bossMaxHP;
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (!isActive || !player) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // Movimiento
        bool walking = false;
        if (dist < preferMinDist) { MoveAwayFromPlayer(); walking = true; }
        else if (dist > preferMaxDist) { MoveTowardsPlayer(); walking = true; }

        LookAtPlayerFlat();

        // Animator de caminata
        if (animator != null)
            animator.SetBool("IsWalking", walking);

        // Ataque
        if (!isCharging)
            StartCoroutine(ChooseAndFire());
    }

    IEnumerator ChooseAndFire()
    {
        isCharging = true;

        yield return new WaitForSeconds(chargeTime);
        yield return new WaitForSeconds(0.41f);

        int pattern = Random.Range(0, 3);
        switch (pattern)
        {
            case 0: FireStar(); break;
            case 1: FireShotgun(); break;
            case 2: StartCoroutine(FireMetralleta()); break;
        }

        isCharging = false;
    }

    void FireStar()
    {
        if (animator != null)
            animator.SetTrigger("DoAttack");

        if (specialAttackClip != null)
            AudioSource.PlayClipAtPoint(specialAttackClip, transform.position);

        Vector3 origin = GetMuzzlePos();
        for (int i = 0; i < starBulletCount; i++)
        {
            float angle = (360f / starBulletCount) * i;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            SpawnBullet(origin, dir);
        }
    }

    void FireShotgun()
    {
        Vector3 origin = GetMuzzlePos();
        Vector3 baseDir = (player.position - origin).normalized;

        for (int i = 0; i < shotgunCount; i++)
        {
            float spread = Random.Range(-shotgunSpread, shotgunSpread);
            Vector3 dir = Quaternion.Euler(0, spread, 0) * baseDir;
            SpawnBullet(origin, dir);
        }
    }

    IEnumerator FireMetralleta()
    {
        Vector3 origin = GetMuzzlePos();

        for (int i = 0; i < metralletaCount; i++)
        {
            Vector3 dir = (player.position - origin).normalized;
            SpawnBullet(origin, dir);
            yield return new WaitForSeconds(metralletaDelay);
        }
    }

    void SpawnBullet(Vector3 origin, Vector3 dir)
    {
        // 🔹 Ignoramos la diferencia en altura (plano horizontal)
        dir.y = 0f;
        dir.Normalize();

        GameObject go = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(dir));

        ProjectileSimple pr = go.GetComponent<ProjectileSimple>();
        if (pr != null)
        {
            pr.damage = bulletDamage;
            pr.speed = bulletSpeed;
            pr.life = bulletLife;
            pr.onlyDamageEnemies = false;

            // 👇 MUY IMPORTANTE: ignorar colisión con el Boss
            pr.owner = GetComponent<Collider>();
        }
    }




    // 🔹 Devuelve la posición correcta del muzzle (o fallback con warning)
    Vector3 GetMuzzlePos()
    {
        if (muzzle != null) return muzzle.position;

        Debug.LogWarning("[Boss] No se asignó muzzle en el inspector, usando posición por defecto.");
        return transform.position + Vector3.up * 1.6f;
    }

    // Sobrescribimos TakeDamage para log extra (pero usamos la lógica de EnemyBase)
    public override void TakeDamage(float amount)
    {
        base.TakeDamage(amount);
        Debug.Log($"[Boss] Impacto recibido: {amount}, Vida restante: {currentHealth}");
    }
}
