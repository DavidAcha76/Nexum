using UnityEngine;
using UnityEngine.UI;


public class PlayerStatsUI : MonoBehaviour
{
    [SerializeField] PlayerController player; // se puede asignar dinámico

    [Header("Texts")]
    public Text coinsText;
    public Text damageText;
    public Text attackSpeedText;
    public Text moveSpeedText;
    public Text shieldsText;

    void Update()
    {
        // 👀 Buscar al Player en runtime si aún no está asignado
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>();
            if (player == null) return; // no hay Player todavía
        }

        // === Stats visibles ===
        if (coinsText) coinsText.text = $"Coins: {player.Coins}";
        if (damageText) damageText.text = $"Damage: {player.Damage:0.##}";
        if (attackSpeedText) attackSpeedText.text = $"Attack Speed: {player.AttackSpeed:0.##}";
        if (moveSpeedText) moveSpeedText.text = $"Move Speed: {player.MoveSpeed:0.##}";
        if (shieldsText) shieldsText.text = $"Shields: {player.CurrentShields}/{player.MaxShields}";
    }

    // 👌 Método público para asignar el Player cuando spawnee
    public void SetPlayer(PlayerController newPlayer)
    {
        player = newPlayer;
    }
}
