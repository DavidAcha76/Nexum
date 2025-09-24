public interface IHealthModel
{
    float Max { get; set; }
    float Current { get; set; }
    bool IsDead { get; }

    void TakeDamage(float amount);
    void Heal(float amount);
}
