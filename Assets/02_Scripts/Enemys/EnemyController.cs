// EnemyController.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyController : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] MonoBehaviour motorMb;
    [SerializeField] MonoBehaviour perceptionMb;
    [SerializeField] MonoBehaviour attackMb;
    [SerializeField] MonoBehaviour animatorMb;  // opcional
    [SerializeField] MonoBehaviour healthMb;    // IHealthModel
    [SerializeField] MonoBehaviour timeMb;      // ITimeSource

    IEnemyMotor motor;
    IEnemyPerception perception;
    IEnemyAttack attack;
    IEnemyAnimator anim;
    IHealthModel health;
    ITimeSource timeSrc;

    [Header("Stats")]
    public float moveSpeed = 3.5f;
    public float preferMinDist = 1.8f;   // si está más cerca que esto, se aleja
    public float preferMaxDist = 6.0f;   // si está más lejos que esto, se acerca
    public float turnSpeed = 10f;

    Transform currentTarget;
    float lastAttackTime = -999f;

    void Awake()
    {
        motor = motorMb as IEnemyMotor;
        perception = perceptionMb as IEnemyPerception;
        attack = attackMb as IEnemyAttack;
        anim = animatorMb as IEnemyAnimator;
        health = healthMb as IHealthModel;
        timeSrc = timeMb as ITimeSource;

        if (timeSrc == null) timeSrc = new UnityTimeFallback();
        if (motor == null) motor = new RigidbodyEnemyMotorFallback(GetComponent<Rigidbody>());
        if (health == null) health = new LocalHealthFallback(); // simple si no tienes Health aún
    }

    void Update()
    {
        if (health.IsDead) { anim?.SetDead(true); return; }

        // 1) Target
        if (currentTarget == null || Time.frameCount % 15 == 0)
            currentTarget = perception?.AcquireTarget();

        bool hasTarget = currentTarget != null;
        anim?.SetWalking(hasTarget);

        if (!hasTarget) return;

        float dt = timeSrc.DeltaTime;
        Vector3 tp = currentTarget.position;
        float dist = Vector3.Distance(transform.position, tp);

        // 2) Mover según “zona de confort”
        if (dist < preferMinDist) motor.MoveAwayFrom(tp, moveSpeed, dt);
        else if (dist > preferMaxDist) motor.MoveTowards(tp, moveSpeed, dt);

        motor.FaceTowards(tp, turnSpeed, dt);

        // 3) Ataque
        float now = Time.time;
        if (attack != null && attack.CanAttack(currentTarget, now))
        {
            lastAttackTime = now;
            anim?.TriggerAttack();
            attack.DoAttack(currentTarget);
        }
    }

    // ====== Fallbacks ======
    class UnityTimeFallback : ITimeSource
    {
        public float DeltaTime => Time.deltaTime;
        public float FixedDeltaTime => Time.fixedDeltaTime;
    }

    class LocalHealthFallback : IHealthModel
    {
        public float Max { get; set; } = 100f;
        public float Current { get; set; } = 100f;
        public bool IsDead => Current <= 0f;
        public void Heal(float amount) => Current = Mathf.Min(Max, Current + Mathf.Abs(amount));
        public void TakeDamage(float amount) => Current = Mathf.Max(0f, Current - Mathf.Abs(amount));
    }

    class RigidbodyEnemyMotorFallback : IEnemyMotor
    {
        readonly Rigidbody rb;
        public RigidbodyEnemyMotorFallback(Rigidbody r)
        {
            rb = r;
            if (rb) { rb.isKinematic = false; rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; }
        }
        public Vector3 Velocity { get => rb ? rb.velocity : Vector3.zero; set { if (rb) rb.velocity = value; } }
        public Component Raw => rb;

        public void MoveTowards(Vector3 worldPos, float speed, float dt)
        {
            Vector3 dir = (worldPos - (rb ? rb.position : Vector3.zero));
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                Velocity = dir.normalized * speed + Vector3.up * (rb ? rb.velocity.y : 0f);
        }

        public void MoveAwayFrom(Vector3 worldPos, float speed, float dt)
        {
            Vector3 dir = ((rb ? rb.position : Vector3.zero) - worldPos);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                Velocity = dir.normalized * speed + Vector3.up * (rb ? rb.velocity.y : 0f);
        }

        public void FaceTowards(Vector3 worldPos, float turnSpeed, float dt)
        {
            Vector3 to = worldPos - (rb ? rb.position : Vector3.zero);
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) return;
            Quaternion target = Quaternion.LookRotation(to);
            if (rb)
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, Mathf.Clamp01(turnSpeed * dt)));
        }
    }
}
