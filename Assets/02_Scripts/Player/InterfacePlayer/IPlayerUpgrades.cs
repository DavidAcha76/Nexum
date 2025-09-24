// IPlayerUpgrades.cs
public interface IPlayerUpgrades
{
    int Coins { get; set; }
    float Damage { get; set; }
    float BaseMoveSpeed { get; set; }
    float AttackSpeed { get; set; }
    int MultiShot { get; set; }

    void AddCoins(int amount);
    void IncreaseDamage(float amount);
    void IncreaseMoveSpeed(float amount);
    void IncreaseAttackSpeed(float amount);
    void AddMultiShot(int extra);
}
