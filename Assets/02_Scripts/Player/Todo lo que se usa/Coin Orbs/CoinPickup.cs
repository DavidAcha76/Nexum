using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    public int coinValue = 1;
    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player)
        {
            player.AddCoins(coinValue);
            Destroy(gameObject);
        }
    }
}
