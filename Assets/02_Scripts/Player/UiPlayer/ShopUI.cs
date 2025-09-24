using UnityEngine;

public class ShopUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject shopPanel;

    private PlayerController player;

    void Start()
    {
        shopPanel.SetActive(false);

        // Busca al PlayerController en la escena (asegúrate que tu Player tenga este script)
        player = FindObjectOfType<PlayerController>();
    }

    public void ToggleShop()
    {
        shopPanel.SetActive(!shopPanel.activeSelf);
        Time.timeScale = shopPanel.activeSelf ? 0f : 1f;
    }

    public void BuyDamage()
    {
        int cost = 5;
        float amount = 2f;
        if (player != null && player.Coins >= cost)
        {
            player.AddCoins(-cost);
            player.IncreaseDamage(amount);
            Debug.Log($"Compraste +{amount} Damage");
        }
    }

    public void BuyMoveSpeed()
    {
        int cost = 10;
        float amount = 0.2f;
        if (player != null && player.Coins >= cost)
        {
            player.AddCoins(-cost);
            player.IncreaseMoveSpeed(amount);
            Debug.Log($"Compraste +{amount} Move Speed");
        }
    }

    public void BuyAttackSpeed()
    {
        int cost = 12;
        float amount = 0.2f;
        if (player != null && player.Coins >= cost)
        {
            player.AddCoins(-cost);
            player.IncreaseAttackSpeed(amount);
            Debug.Log($"Compraste +{amount} Attack Speed");
        }
    }

    public void BuyMultiShot()
    {
        int cost = 15;
        int amount = 1;
        if (player != null && player.Coins >= cost)
        {
            player.AddCoins(-cost);
            player.AddMultiShot(amount);
            Debug.Log($"Compraste +{amount} MultiShot");
        }
    }
}
