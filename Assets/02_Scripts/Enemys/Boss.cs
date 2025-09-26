

using System.Collections;

using UnityEngine;

public class Boss : EnemyBase

{

    [Header("Boss Stats")]

    public float bossMaxHP = 300f;

    [Header("Animation & Audio")]

    public Animator animator;

    public AudioClip phase1Clip;   // fase 1 (normal)

    public AudioClip phase2Clip;   // fase 2 (star bullets)

    public AudioClip phase3Clip;   // fase 3 (melee)

    public AudioClip phase4Clip;   // fase 4 (charge/toro)

    public AudioClip rageClip;     // sonido de embestida

    [Header("Refs")]

    public Transform muzzle;

    public GameObject projectilePrefab;

    [Header("Movement Prefs")]

    public float preferMinDist = 6f;

    public float preferMaxDist = 12f;

    [Header("Projectile Config")]

    public float chargeTime = 1.5f;

    public int starBulletCount = 12;

    public int shotgunCount = 5;

    public float shotgunSpread = 15f;

    public int metralletaCount = 3;

    public float metralletaDelay = 0.2f;

    public float bulletSpeed = 14f;

    public float bulletLife = 4f;

    public float bulletDamage = 12f;

    [Header("Melee Config")]

    public float meleeDamage = 25f;

    public float meleeRange = 2f;

    [Header("Charge Config")]

    public float chargeForce = 20f;

    public float chargeDuration = 1.5f;

    public float chargeCooldown = 8f;

    private bool isBusy = false;

    private bool isChargingAttack = false;

    private int currentPhase = 1;

    private Rigidbody bossRb;

    [Header("Goal Drop")]

    public GameObject goalPrefab;

    protected override void Awake()

    {

        base.Awake();

        maxHealth = bossMaxHP;

        currentHealth = maxHealth;

        bossRb = GetComponent<Rigidbody>();

        if (bossRb) bossRb.isKinematic = false; // 🔹 para que use física en la embestida

    }

    void Update()

    {

        if (!isActive || !player) return;

        UpdatePhase();

        float dist = Vector3.Distance(transform.position, player.position);

        // 🔹 Movimiento si no está atacando

        bool walking = false;

        if (!isBusy && !isChargingAttack)

        {

            if (dist < preferMinDist) { MoveAwayFromPlayer(); walking = true; }

            else if (dist > preferMaxDist) { MoveTowardsPlayer(); walking = true; }

            LookAtPlayerFlat();

        }

        if (animator != null)

            animator.SetBool("IsWalking", walking);

        // 🔹 Ejecutar ataques

        if (!isBusy) StartCoroutine(DoPhaseAttack());

    }

    void UpdatePhase()

    {

        float hpPercent = currentHealth / maxHealth;

        if (hpPercent > 0.75f) currentPhase = 1;         // solo disparos normales

        else if (hpPercent > 0.50f) currentPhase = 3;    // fase melee (ANTES estaba en 3 → ahora en 2)

        else if (hpPercent > 0.25f) currentPhase = 2;    // fase estrella (ANTES estaba en 2 → ahora en 3)

        else currentPhase = 4;                           // embestida

    }

    IEnumerator DoPhaseAttack()

    {

        isBusy = true;

        yield return new WaitForSeconds(chargeTime);

        switch (currentPhase)

        {

            case 1:

                PlayClip(phase1Clip);

                if (Random.value < 0.5f) FireShotgun(); else StartCoroutine(FireMetralleta());

                break;

            case 2:

                PlayClip(phase2Clip);

                FireStar();

                break;

            case 3:

                PlayClip(phase3Clip);

                if (Random.value < 0.5f) FireShotgun(); else DoMelee();

                break;

            case 4:

                PlayClip(phase4Clip);

                DoChargeAttack();

                break;

        }

        yield return new WaitForSeconds(1f);

        isBusy = false;

    }

    // === Ataques ===

    void FireStar()

    {

        if (animator) animator.SetTrigger("DoAttack");

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

    void DoMelee()

    {

        if (animator) animator.SetTrigger("DoMelee");

        Collider[] hits = Physics.OverlapSphere(transform.position, meleeRange);

        foreach (var h in hits)

        {

            var pc = h.GetComponent<PlayerController>();

            if (pc != null) pc.TakeDamage(meleeDamage);

        }

        Debug.Log("[Boss] Golpe cuerpo a cuerpo!");

    }

    void DoChargeAttack()

    {

        if (isChargingAttack) return;

        StartCoroutine(ChargeRoutine());

    }

    IEnumerator ChargeRoutine()

    {

        isChargingAttack = true;

        if (rageClip) AudioSource.PlayClipAtPoint(rageClip, transform.position);

        if (animator) animator.SetTrigger("DoRage");

        yield return new WaitForSeconds(1f); // tiempo de "carga"

        Vector3 dir = (player.position - transform.position).normalized;

        dir.y = 0f;

        float timer = 0f;

        while (timer < chargeDuration)

        {

            bossRb.velocity = dir * chargeForce;

            timer += Time.deltaTime;

            yield return null;

        }

        bossRb.velocity = Vector3.zero;

        isChargingAttack = false;

    }

    void SpawnBullet(Vector3 origin, Vector3 dir)

    {

        dir.y = 0f; dir.Normalize();

        GameObject go = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(dir));

        ProjectileSimple pr = go.GetComponent<ProjectileSimple>();

        if (pr != null)

        {

            pr.damage = bulletDamage;

            pr.speed = bulletSpeed;

            pr.life = bulletLife;

            pr.onlyDamageEnemies = false;

            pr.owner = GetComponent<Collider>();

        }

    }

    Vector3 GetMuzzlePos()

    {

        if (muzzle != null) return muzzle.position;

        return transform.position + Vector3.up * 1.6f;

    }

    void PlayClip(AudioClip clip)

    {

        if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position);

    }

    public override void TakeDamage(float amount)

    {

        base.TakeDamage(amount);

        Debug.Log($"[Boss] Impacto recibido: {amount}, Vida restante: {currentHealth}");

    }

    protected override void OnDeath()

    {

        base.OnDeath();

        // 🔹 Loot extra al morir

        for (int i = 0; i < 3; i++)

        {

            if (powerupPrefabs.Length > 0)

            {

                int index = Random.Range(0, powerupPrefabs.Length);

                Instantiate(powerupPrefabs[index], transform.position + Vector3.up * 0.5f, Quaternion.identity);

            }

        }

        // 🔹 Spawnear el goal

        if (goalPrefab != null)

        {

            Instantiate(goalPrefab, transform.position + Vector3.forward * 2f, Quaternion.identity);

            Debug.Log("[Boss] Dropeó el Goal!");

        }

        Debug.Log("[Boss] Eliminado, soltó recompensas extra!");

    }

    void OnCollisionEnter(Collision other)

    {

        if (isChargingAttack)

        {

            // Si choca con jugador o muro, se detiene

            bossRb.velocity = Vector3.zero;

            isChargingAttack = false;

            if (other.gameObject.CompareTag("Player"))

            {

                var pc = other.gameObject.GetComponent<PlayerController>();

                if (pc != null) pc.TakeDamage(meleeDamage * 2f); // daño fuerte de embestida

                Debug.Log("[Boss] Embistió al jugador!");

            }

        }

    }

}

