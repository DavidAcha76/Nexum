using TMPro; // 👈 importante para TextMeshPro
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerController player;

    [Header("Sliders")]
    public Slider healthSlider;
    public Slider staminaSlider;
    public Slider ultimateSlider;

    [Header("Texts (TMP)")]
    public TMP_Text healthText;
    public TMP_Text staminaText;
    public TMP_Text ultimateText;
    public TMP_Text coinsText;


    [Header("Stats Texts (TMP)")] // 👈 nuevos
    public TMP_Text damageText;
    public TMP_Text moveSpeedText;
    public TMP_Text attackSpeedText;
    public TMP_Text healText;
    public TMP_Text shieldsText;

    private bool initialized = false;

    void Update()
    {
        // 🔹 Inicializar player si aún no se encontró
        if (!initialized)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
            {
                player = go.GetComponent<PlayerController>();
                if (player != null)
                {
                    if (healthSlider != null) healthSlider.maxValue = 1f;
                    if (staminaSlider != null) staminaSlider.maxValue = 1f;
                    if (ultimateSlider != null) ultimateSlider.maxValue = 1f;

                    initialized = true;
                    Debug.Log("[PlayerUI] Player encontrado y UI inicializada");
                }
            }
        }

        if (!player) return;

        // === Vida ===
        if (healthSlider != null)
            healthSlider.value = player.Health01;
        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(player.Health)} / {Mathf.CeilToInt(player.MaxHealth)}";

        // === Stamina ===
        if (staminaSlider != null)
            staminaSlider.value = player.Stamina01;
        if (staminaText != null)
            staminaText.text = $"{Mathf.CeilToInt(player.Stamina)} / {Mathf.CeilToInt(player.MaxStamina)}";

        // === Ultimate ===
        if (ultimateSlider != null)
            ultimateSlider.value = player.Ultimate01;
        if (ultimateText != null)
            ultimateText.text = $"{Mathf.RoundToInt(player.Ultimate01 * 100)}%";

        // === Stats extras ===
        if (damageText != null)
            damageText.text = $"Damage: {player.Damage:0.##}";
        // === Monedas ===
        if (coinsText != null)
            coinsText.text = $"Coins: {player.Coins}";

        if (moveSpeedText != null)
            moveSpeedText.text = $"Move Speed: {player.MoveSpeed:0.##}";

        if (attackSpeedText != null)
            attackSpeedText.text = $"Attack Speed: {player.AttackSpeed:0.##}";

        if (healText != null)
            healText.text = $"Heal: {Mathf.CeilToInt(player.Health)} / {Mathf.CeilToInt(player.MaxHealth)}";

        if (shieldsText != null)
            shieldsText.text = $"Shields: {player.CurrentShields}/{player.MaxShields}";
    }
}
