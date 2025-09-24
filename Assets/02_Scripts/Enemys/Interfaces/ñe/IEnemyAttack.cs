// IEnemyAttack.cs
using UnityEngine;
public interface IEnemyAttack
{
    float Range { get; }
    float Cooldown { get; }
    bool CanAttack(Transform target, float now);
    void DoAttack(Transform target);
}
 