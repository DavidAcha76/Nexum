using UnityEngine;

public class StatOrb : MonoBehaviour
{
    public enum UpgradeType { Damage, MoveSpeed, AttackSpeed, Heal, Shield }
    public UpgradeType upgrade;
    public float amount = 1f;

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player)
        {
            switch (upgrade)
            {
                case UpgradeType.Damage: player.IncreaseDamage(amount); break;
                case UpgradeType.MoveSpeed: player.IncreaseMoveSpeed(amount); break;
                case UpgradeType.AttackSpeed: player.IncreaseAttackSpeed(amount); break;
                case UpgradeType.Heal: player.Heal(amount); break;
                case UpgradeType.Shield: player.AddShield(Mathf.RoundToInt(amount)); break;
            }

            Destroy(gameObject);
        }
    }
}
