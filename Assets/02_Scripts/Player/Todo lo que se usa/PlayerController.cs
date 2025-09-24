using System.Collections; // 👉 necesario para IEnumerator
using UnityEngine;

[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float baseMoveSpeed = 5f;
    public float MoveSpeed => baseMoveSpeed;

    [Header("Dash Config")]
    [SerializeField] float dashForce = 12f;
    [SerializeField] float dashDuration = 0.2f;
    [SerializeField] float dashCooldown = 1f;
    [SerializeField] float dashCost = 30f;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;

    [Header("Stamina")]
    [SerializeField] float maxStamina = 100f;
    [SerializeField] float staminaRegenPerSecond = 20f;
    [SerializeField] float staminaRegenDelay = 0.5f;
    private float currentStamina;
    private float staminaRegenTimer;

    [Header("Health Config")]
    [SerializeField] float maxHealth = 100f;
    private float currentHealth;

    [Header("Upgrades")]
    [SerializeField] int coins = 0;
    [SerializeField] float damage = 10f;
    [SerializeField] float attackSpeed = 1f;
    [SerializeField] int multiShot = 1;

    [Header("Refs")]
    public SimpleJoystick moveJoystick;
    public Transform firePoint;
    private Rigidbody rb;
    private Animator animator;

    [Header("Ultimate Config")]
    [SerializeField] float ultimateCharge = 0f; // 0–100
    [SerializeField] float chargePerKill = 20f;
    [SerializeField] float ultimateDuration = 3f;
    private bool isUsingUltimate = false;

    public GameObject sniperBulletPrefab; // Prefab de bala ultimate
    public Transform firePointUltimate;   // FirePoint para la ulti (puedes usar el mismo)

    public bool CanUseUltimate => ultimateCharge >= 100f;
    public float Ultimate01 => ultimateCharge / 100f; // para la UI

    // === accesores para UI ===
    public int Coins => coins;
    public float Damage => damage;
    public float AttackSpeed => attackSpeed;
    public int MultiShot => multiShot;

    public float Health => currentHealth;
    public float Health01 => currentHealth / maxHealth;
    public float Stamina => currentStamina;
    public float Stamina01 => currentStamina / maxStamina;

    void Awake()
    {
        if (moveJoystick == null)
        {
            SimpleJoystick[] joysticks = FindObjectsOfType<SimpleJoystick>();
            if (joysticks.Length > 0) moveJoystick = joysticks[0];
        }

        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        currentHealth = maxHealth;
        currentStamina = maxStamina;

        if (firePointUltimate == null) firePointUltimate = firePoint; // fallback
    }

    void FixedUpdate()
    {
        // 🔹 si está en ultimate, bloquea todo
        if (isUsingUltimate)
        {
            rb.velocity = Vector3.zero;
            animator.SetBool("isWalking", false);
            return;
        }

        // === Movimiento normal ===
        if (!isDashing)
        {
            Vector2 inputDir = moveJoystick ? moveJoystick.Direction : Vector2.zero;
            Vector3 move = new Vector3(inputDir.x, 0f, inputDir.y);
            if (move.magnitude > 1f) move.Normalize();

            Vector3 velocity = move * baseMoveSpeed;
            velocity.y = rb.velocity.y;
            rb.velocity = velocity;

            // Rotación hacia dirección de movimiento
            if (move.sqrMagnitude > 0.001f)
            {
                firePoint.rotation = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = firePoint.rotation;
            }

            // Animaciones
            animator.SetBool("isWalking", move.sqrMagnitude > 0.01f);
            if (move.sqrMagnitude <= 0.01f) animator.SetBool("Idle", true);
            else animator.SetBool("Idle", false);
        }

        // === Dash en progreso ===
        if (isDashing)
        {
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                rb.velocity = Vector3.zero;
            }
        }

        // === Cooldown del dash ===
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.fixedDeltaTime;

        // === Regeneración de stamina ===
        staminaRegenTimer += Time.fixedDeltaTime;
        if (staminaRegenTimer >= staminaRegenDelay)
        {
            currentStamina += staminaRegenPerSecond * Time.fixedDeltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }
    }

    // === DASH API ===
    public void DoDash()
    {
        if (isDashing || isUsingUltimate) return;
        if (dashCooldownTimer > 0f) return;
        if (currentStamina < dashCost) return;

        currentStamina -= dashCost;
        staminaRegenTimer = 0f;

        float dashDistance = 5f;
        Vector3 dashDir = transform.forward;
        float dashSpeed = dashDistance / dashDuration;

        rb.velocity = dashDir * dashSpeed;

        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        animator.SetTrigger("Dash");
    }

    // === Health API ===
    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0f) return;

        currentHealth -= Mathf.Abs(amount);

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();
        }
    }

    public void Heal(float amount)
    {
        if (currentHealth <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.Abs(amount));
    }

    // === Ultimate API ===
    public void AddUltimateCharge(float amount)
    {
        ultimateCharge = Mathf.Clamp(ultimateCharge + amount, 0f, 100f);
    }

    public void ResetUltimate()
    {
        ultimateCharge = 0f;
    }

    public void DoUltimate()
    {
        if (!CanUseUltimate || isUsingUltimate) return;

        isUsingUltimate = true;
        ultimateCharge = 0f;
        rb.velocity = Vector3.zero;

        animator.SetTrigger("Ultimate");
        StartCoroutine(UltimateRoutine());
    }

    private IEnumerator UltimateRoutine()
    {
        yield return new WaitForSeconds(ultimateDuration);
        isUsingUltimate = false;
    }
    public bool IsUsingUltimate => isUsingUltimate;

    // 🔹 Llamado desde Animation Event en el frame de disparo
    public void FireUltimateBullet()
    {
        if (sniperBulletPrefab == null || firePointUltimate == null) return;

        GameObject bullet = Instantiate(sniperBulletPrefab, firePointUltimate.position, firePointUltimate.rotation);

        Projectile proj = bullet.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.damage *= 5f; // más daño
            proj.speed *= 2f;  // más rápido
        }
    }

    public void AddKill()
    {
        AddUltimateCharge(20f); // o usa chargePerKill si lo tienes configurado
    }


    // === Upgrades API ===
    public void AddCoins(int amount) { coins += amount; }
    public void IncreaseDamage(float amount) => damage += amount;
    public void IncreaseMoveSpeed(float amount) => baseMoveSpeed += amount;
    public void IncreaseAttackSpeed(float amount) => attackSpeed += amount;
    public void AddMultiShot(int extra) => multiShot += extra;
}
