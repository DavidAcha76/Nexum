// IEnemyAnimator.cs
public interface IEnemyAnimator
{
    void SetWalking(bool walking);
    void TriggerAttack();
    void SetDead(bool dead);
}
