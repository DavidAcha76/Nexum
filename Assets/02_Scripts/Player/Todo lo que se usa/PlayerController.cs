using Fusion;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : NetworkBehaviour
{
    // ========= Config =========
    [Header("Movement")]
    [SerializeField] float baseMoveSpeed = 5f;

    [Header("Dash Config")]
    [SerializeField] float dashForce = 12f;
    [SerializeField] float dashDuration = 0.2f;
    [SerializeField] float dashCooldown = 1f;
    [SerializeField] float dashCost = 30f;

    [Header("Stamina")]
    [SerializeField] float maxStamina = 100f;
    [SerializeField] float staminaRegenPerSecond = 20f;
    [SerializeField] float staminaRegenDelay = 0.5f;

    [Header("Health Config")]
    [SerializeField] float maxHealth = 100f;

    [Header("Shields")]
    [SerializeField] int maxShields = 3;

    [Header("Upgrades")]
    [SerializeField] int coins = 0;
    [SerializeField] float damage = 10f;
    [SerializeField] float attackSpeed = 1f;   // << usado por Shooter

    [Header("Refs (opcionales)")]
    public Transform firePoint;
    public Transform firePointUltimate;
    public GameObject sniperBulletPrefab;
    public Animator animator;

    [Header("Ultimate")]
    [SerializeField] float ultimateDuration = 3f; // duración de la ultimate

    // ========= Estado replicado =========
    [Networked] public float CurrentHealth { get; private set; }
    [Networked] public float CurrentStamina { get; private set; }
    [Networked] public int CurrentShields { get; private set; }
    [Networked] public bool IsDashing { get; private set; }
    [Networked] public bool IsUsingUltimate { get; private set; }
    [Networked] public float UltimateCharge { get; private set; } // 0..100
    [Networked] public Vector2 MoveInput { get; private set; }    // << replicado para otros scripts

    // Timers (solo en StateAuthority)
    private float _dashTimer;
    private float _dashCooldownTimer;
    private float _staminaRegenTimer;

    private Rigidbody _rb;

    // ========= Expuestos =========
    public float MoveSpeed => baseMoveSpeed;
    public bool CanUseUltimate => UltimateCharge >= 100f;
    public float Ultimate01 => Mathf.Clamp01(UltimateCharge / 100f);

    public int Coins => coins;
    public float Damage => damage;
    public float AttackSpeed => attackSpeed;   // << propiedad pedida

    public float Health => CurrentHealth;
    public float Health01 => Mathf.Clamp01(CurrentHealth / maxHealth);
    public float Stamina => CurrentStamina;
    public float Stamina01 => Mathf.Clamp01(CurrentStamina / maxStamina);

    public float MaxHealth => maxHealth;
    public float MaxStamina => maxStamina;
    public int MaxShields => maxShields;

    // ================== Ciclo de Vida ==================
    public override void Spawned()
    {
        _rb = GetComponent<Rigidbody>();
        if (!animator) animator = GetComponent<Animator>();

        // Si usas NetworkTransform, deja el rigidbody sin interpolación
        _rb.interpolation = RigidbodyInterpolation.None;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (firePointUltimate == null) firePointUltimate = firePoint;

        if (Object.HasStateAuthority)
        {
            CurrentHealth = maxHealth;
            CurrentStamina = maxStamina;
            CurrentShields = 0;
            IsDashing = false;
            IsUsingUltimate = false;
            UltimateCharge = 0f;
            MoveInput = Vector2.zero;

            _dashTimer = 0f;
            _dashCooldownTimer = 0f;
            _staminaRegenTimer = 0f;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        float dt = Runner.DeltaTime;

        // Lee input enviado por el dueño
        Vector2 inputDir = Vector2.zero;
        if (GetInput(out PlayerInputData input))
        {
            inputDir = input.move;
            MoveInput = inputDir;

            if (input.dash) RPC_TryDash();
            if (input.ultimate) RPC_TryUltimate();
        }

        // Ultimate bloquea movimiento
        if (IsUsingUltimate)
        {
            _rb.velocity = Vector3.zero;
            SetWalkAnim(false);
            TickRegen(dt);
            return;
        }

        // Movimiento normal si no está en dash
        if (!IsDashing)
        {
            Vector3 move = new Vector3(inputDir.x, 0f, inputDir.y);
            if (move.sqrMagnitude > 1f) move.Normalize();

            Vector3 vel = move * baseMoveSpeed;
            vel.y = _rb.velocity.y;
            _rb.velocity = vel;

            if (move.sqrMagnitude > 0.001f)
            {
                var rot = Quaternion.LookRotation(move, Vector3.up);
                if (firePoint) firePoint.rotation = rot;
                transform.rotation = rot;
            }

            SetWalkAnim(move.sqrMagnitude > 0.01f);
        }

        // Dash
        if (IsDashing)
        {
            _dashTimer -= dt;
            if (_dashTimer <= 0f)
            {
                IsDashing = false;
                _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
            }
        }

        if (_dashCooldownTimer > 0f)
            _dashCooldownTimer -= dt;

        TickRegen(dt);
    }

    private void TickRegen(float dt)
    {
        _staminaRegenTimer += dt;
        if (_staminaRegenTimer >= staminaRegenDelay)
        {
            CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + staminaRegenPerSecond * dt);
        }
    }

    private void SetWalkAnim(bool walking)
    {
        if (animator) animator.SetBool("isWalking", walking);
    }

    // ================== Acciones locales (UI puede llamarlas) ==================
    public void DoDash()
    {
        if (!Object.HasInputAuthority) return;
        RPC_TryDash();
    }

    public void DoUltimate()
    {
        if (!Object.HasInputAuthority) return;
        RPC_TryUltimate();
    }

    public void FireUltimateBullet() // anim event local (solo visual local)
    {
        if (!Object.HasInputAuthority) return;
        if (!sniperBulletPrefab || !firePointUltimate) return;

        var go = Instantiate(sniperBulletPrefab, firePointUltimate.position, firePointUltimate.rotation);
        var proj = go.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.damage *= 5f;
            proj.speed *= 2f;
        }
    }

    // ================== RPCs (dueño -> autoridad) ==================
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_TryDash()
    {
        if (IsDashing || IsUsingUltimate) return;
        if (_dashCooldownTimer > 0f) return;
        if (CurrentStamina < dashCost) return;

        CurrentStamina -= dashCost;
        _staminaRegenTimer = 0f;

        float dashSpeed = (dashForce > 0f) ? dashForce : (2f / Mathf.Max(0.01f, dashDuration));
        Vector3 forward = transform.forward;
        _rb.velocity = new Vector3(forward.x * dashSpeed, _rb.velocity.y, forward.z * dashSpeed);

        IsDashing = true;
        _dashTimer = dashDuration;
        _dashCooldownTimer = dashCooldown;

        if (animator) animator.SetTrigger("Dash");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_TryUltimate()
    {
        if (IsUsingUltimate) return;
        if (!CanUseUltimate) return;

        IsUsingUltimate = true;
        UltimateCharge = 0f;
        _rb.velocity = Vector3.zero;

        if (animator) animator.SetTrigger("Ultimate");
        StartCoroutine(UltimateCoroutine());
    }

    private IEnumerator UltimateCoroutine()
    {
        float t = 0f;
        while (t < ultimateDuration)
        {
            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        IsUsingUltimate = false;
    }

    // ================== Daño / curación / upgrades ==================
    public void TakeDamage(float amount)
    {
        if (!Object.HasStateAuthority) return;

        if (CurrentShields > 0)
        {
            CurrentShields--;
            Debug.Log($"[Player] Escudo bloqueó el daño. Escudos restantes: {CurrentShields}");
            return;
        }

        CurrentHealth -= Mathf.Abs(amount);
        if (CurrentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        var ui = UnityEngine.Object.FindFirstObjectByType<GameOverUI>();
        if (ui) ui.ShowGameOver();
        else
        {
            Time.timeScale = 0f;
            Debug.LogWarning("No hay GameOverUI en escena, congelando tiempo.");
        }
    }

    public void Heal(float amount)
    {
        if (!Object.HasStateAuthority) return;
        if (CurrentHealth <= 0f) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + Mathf.Abs(amount));
    }

    public void AddShield(int amount)
    {
        if (!Object.HasStateAuthority) return;
        CurrentShields = Mathf.Min(maxShields, CurrentShields + Mathf.Max(0, amount));
        Debug.Log($"[Player] Escudos actuales: {CurrentShields}");
    }

    public void AddUltimateCharge(float amount)
    {
        if (!Object.HasStateAuthority) return;
        UltimateCharge = Mathf.Clamp(UltimateCharge + amount, 0f, 100f);
    }

    public void ResetUltimate()
    {
        if (!Object.HasStateAuthority) return;
        UltimateCharge = 0f;
    }

    public void AddKill()
    {
        AddUltimateCharge(20f);
    }

    public void AddCoins(int amount) { if (Object.HasStateAuthority) coins += amount; }
    public void IncreaseDamage(float amount) { if (Object.HasStateAuthority) damage += amount; }
    public void IncreaseMoveSpeed(float amount) { if (Object.HasStateAuthority) baseMoveSpeed += amount; }
    public void IncreaseAttackSpeed(float amount) { if (Object.HasStateAuthority) attackSpeed += amount; }
}
