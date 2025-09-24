
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
    [SerializeField] float dashCost = 30f;   // cuánto stamina cuesta el dash
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
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        currentHealth = maxHealth;
        currentStamina = maxStamina;
    }

    void FixedUpdate()
    {
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
        if (isDashing) return;
        if (dashCooldownTimer > 0f) return;
        if (currentStamina < dashCost) return;

        // gasta stamina
        currentStamina -= dashCost;
        staminaRegenTimer = 0f;

        // dirección de dash = hacia donde esté mirando
        Vector3 dashDir = transform.forward;
        rb.velocity = dashDir * dashForce;

        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        Debug.Log($"[Player] Dash → stamina restante: {currentStamina}");
    }

    // === Health API ===
    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0f) return;

        currentHealth -= Mathf.Abs(amount);
        Debug.Log($"[Player] Recibió {amount} daño → HP restante: {currentHealth}");

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Debug.Log("[PlayerController] Player muerto");

            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();
            else
                Debug.LogWarning("[PlayerController] No hay GameManager en escena.");
        }
    }

    public void Heal(float amount)
    {
        if (currentHealth <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.Abs(amount));
    }

    // === Upgrades API ===
    public void AddCoins(int amount) { coins += amount; }
    public void IncreaseDamage(float amount) => damage += amount;
    public void IncreaseMoveSpeed(float amount) => baseMoveSpeed += amount;
    public void IncreaseAttackSpeed(float amount) => attackSpeed += amount;
    public void AddMultiShot(int extra) => multiShot += extra;
}
