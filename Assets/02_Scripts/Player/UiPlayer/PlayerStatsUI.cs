using UnityEngine;
using UnityEngine.UI;


public class PlayerStatsUI : MonoBehaviour
{
    [SerializeField] PlayerController player; // se puede asignar dinámico

    public Text coinsText;
    public Text damageText;
    public Text attackSpeedText;
    public Text multiShotText;
    public Text moveSpeedText;
    public Image healthBar;
    public Image staminaBar;

    void Update()
    {
        // 👀 Si aún no tenemos referencia, intenta buscar al Player en escena
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>();
            if (player == null) return; // no hay Player todavía
        }

        // === UI ===
        if (coinsText) coinsText.text = $"Coins: {player.Coins}";
        if (damageText) damageText.text = $"Damage: {player.Damage:0.##}";
        if (attackSpeedText) attackSpeedText.text = $"Attack Speed: {player.AttackSpeed:0.##}";
        if (multiShotText) multiShotText.text = $"MultiShot: {player.MultiShot}";
        if (moveSpeedText) moveSpeedText.text = $"Move Speed: {player.MoveSpeed:0.##}";

        if (healthBar) healthBar.fillAmount = player.Health01;
        if (staminaBar) staminaBar.fillAmount = player.Stamina01;
    }

    // 👌 Método público para asignar el Player cuando spawnee
    public void SetPlayer(PlayerController newPlayer)
    {
        player = newPlayer;
    }
}
