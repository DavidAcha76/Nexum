using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerSatusUI_SOLID : MonoBehaviour
{
    [SerializeField] MonoBehaviour upgradesSource;
    [SerializeField] bool autoBind = true;
    [SerializeField] float rebindInterval = 0.5f;
    [SerializeField] GameObject root;

    public Text coinsText, damageText, attackSpeedText, multiShotText, moveSpeedText;

    IPlayerUpgrades upgrades;
    float rebindTimer;

    void Awake()
    {
        if (!root) root = gameObject;
        SetVisible(false);
        TryBind();

    }

    void OnEnable()
    {
        if (RunManager.Instance)
        {
            RunManager.Instance.OnPlayerSpawned += HandleSpawn;
            if (RunManager.Instance.CurrentPlayer)
                HandleSpawn(RunManager.Instance.CurrentPlayer);
        }
    }

    void OnDisable()
    {
        if (RunManager.Instance)
            RunManager.Instance.OnPlayerSpawned -= HandleSpawn;
    }

    void HandleSpawn(PlayerController pc)
    {
        SetUpgrades(pc as IPlayerUpgrades);
        SetVisible(upgrades != null);
        Refresh();
    }

    void Update()
    {
        if (autoBind && upgrades == null)
        {
            rebindTimer -= Time.unscaledDeltaTime;
            if (rebindTimer <= 0f)
            {
                rebindTimer = rebindInterval;
                TryBind();
            }
        }

        if (upgrades != null)
        {
            Refresh();
        }
        else
        {
            SetVisible(false);
        }
    }

    void Refresh()
    {
        if (upgrades == null) return;

        if (coinsText) coinsText.text = $"Coins: {upgrades.Coins}";
        if (damageText) damageText.text = $"Damage: {upgrades.Damage:0.##}";
        if (attackSpeedText) attackSpeedText.text = $"Attack Speed: {upgrades.AttackSpeed:0.##}";
        if (multiShotText) multiShotText.text = $"MultiShot: {upgrades.MultiShot}";
        if (moveSpeedText) moveSpeedText.text = $"Move Speed: {upgrades.BaseMoveSpeed:0.##}";
    }


    void TryBind()
    {
        if (!upgradesSource)
        {
            var pc = FindObjectOfType<PlayerController>();
            if (pc) upgradesSource = pc;
        }
        upgrades = upgradesSource as IPlayerUpgrades;
        SetVisible(upgrades != null);
    }

    public void SetUpgrades(IPlayerUpgrades u)
    {
        upgrades = u;
        upgradesSource = u as MonoBehaviour;
    }

    void SetVisible(bool v)
    {
        if (root && root.activeSelf != v) root.SetActive(v);
    }
}
