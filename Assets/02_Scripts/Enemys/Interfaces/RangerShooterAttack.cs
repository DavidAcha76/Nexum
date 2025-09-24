// RangedShooterAttack.cs
using UnityEngine;

public class RangerShooterAttack : MonoBehaviour, IEnemyAttack
{
    public GameObject projectilePrefab;
    public Transform muzzle;
    public float range = 12f;
    public float cooldown = 1.5f;
    public float shotDamage = 14f;
    public float shotSpeed = 16f;
    public float shotLife = 4f;
    public bool aimAtChest = true;
    public float chestOffset = 1.1f;

    float lastTime = -999f;
    public float Range => range;
    public float Cooldown => cooldown;

    public bool CanAttack(Transform target, float now)
    {
        if (!target || projectilePrefab == null) return false;
        if (now - lastTime < cooldown) return false;
        return Vector3.Distance(transform.position, target.position) <= range;
    }

    public void DoAttack(Transform target)
    {
        lastTime = Time.time;
        Vector3 origin = muzzle ? muzzle.position : transform.position + Vector3.up * 1.4f;
        Vector3 aim = target.position + (aimAtChest ? Vector3.up * chestOffset : Vector3.zero);
        Vector3 dir = (aim - origin).normalized;

        var go = GameObject.Instantiate(projectilePrefab, origin, Quaternion.LookRotation(dir));
        var pr = go.GetComponent<ProjectileSimple>();
        if (pr)
        {
            pr.damage = shotDamage;
            pr.speed = shotSpeed;
            pr.life = shotLife;
            pr.onlyDamageEnemies = false;
        }
    }
}
