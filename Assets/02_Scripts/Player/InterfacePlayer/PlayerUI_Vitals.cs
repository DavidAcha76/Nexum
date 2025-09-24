using UnityEngine;
using UnityEngine.UI;

public class PlayerUI_Vitals : MonoBehaviour
{
    [Header("Binding")]
    [Tooltip("Déjalo vacío: se auto-bindea al PlayerController al aparecer")]
    [SerializeField] MonoBehaviour vitalsSource;
    [SerializeField] bool autoBind = true;
    [SerializeField] float rebindInterval = 0.5f;

    public Image healthFill;   // Type = Filled
    public Image staminaFill;  // Type = Filled

    IPlayerVitals vitals;
    float rebindTimer;

    void Awake() { TryBind(); }

    void Update()
    {
        // Reintenta binding si el player aún no existía
        if (autoBind && vitals == null)
        {
            rebindTimer -= Time.unscaledDeltaTime;
            if (rebindTimer <= 0f)
            {
                rebindTimer = rebindInterval;
                TryBind();
            }
        }

        if (vitals == null) return;
        if (healthFill) healthFill.fillAmount = Mathf.Clamp01(vitals.Health01);
        if (staminaFill) staminaFill.fillAmount = Mathf.Clamp01(vitals.Stamina01);
    }

    void TryBind()
    {
        if (vitalsSource == null)
        {
            var pc = FindObjectOfType<PlayerController>();
            if (pc) vitalsSource = pc;
        }

        vitals = vitalsSource as IPlayerVitals;
    }

    // Si quieres asignarlo por código:
    public void SetVitals(IPlayerVitals v)
    {
        vitals = v;
        vitalsSource = v as MonoBehaviour;
    }
}
