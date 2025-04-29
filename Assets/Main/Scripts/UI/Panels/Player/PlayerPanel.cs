using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPanel : Panel
{
    public override PanelType PanelType => PanelType.PlayerInfo;

    [SerializeField]
    private PlayerSkillList skillList;

    [Header("Player Info Texts")]
    [SerializeField]
    private TextMeshProUGUI playerDefText;

    [SerializeField]
    private TextMeshProUGUI playerAtkText;

    [SerializeField]
    private TextMeshProUGUI playerMSText;

    [SerializeField]
    private TextMeshProUGUI levelText;

    [SerializeField]
    private TextMeshProUGUI expText;

    [SerializeField]
    private TextMeshProUGUI hpText;

    [SerializeField]
    private TextMeshProUGUI expCollectRadText;

    [SerializeField]
    private TextMeshProUGUI playerHpRegenText;

    [SerializeField]
    private TextMeshProUGUI playerAttackRangeText;

    [SerializeField]
    private TextMeshProUGUI playerAttackSpeedText;

    [Header("UI Bars")]
    [SerializeField]
    private Slider hpBarImage;

    [SerializeField]
    private Slider expBarImage;

    private Player player;
    private PlayerStat playerStat;
    private Coroutine updateCoroutine;

    public void Initialize()
    {
        PrepareUI();
    }

    public void PrepareUI()
    {
        if (playerDefText)
            playerDefText.text = "DEF : 0";
        if (playerAtkText)
            playerAtkText.text = "ATK : 0";
        if (playerMSText)
            playerMSText.text = "MoveSpeed : 0";
        if (levelText)
            levelText.text = "LEVEL : 1";
        if (expText)
            expText.text = "EXP : 0/0";
        if (hpText)
            hpText.text = "0 / 0";
        if (expCollectRadText)
            expCollectRadText.text = "ExpRad : 0";
        if (playerHpRegenText)
            playerHpRegenText.text = "HPRegen : 0/s";
        if (playerAttackRangeText)
            playerAttackRangeText.text = "AR : 0";
        if (playerAttackSpeedText)
            playerAttackSpeedText.text = "AS : 0/s";

        if (hpBarImage)
            hpBarImage.value = 0;
        if (expBarImage)
            expBarImage.value = 0;
    }

    public void InitializePlayerUI(Player player)
    {
        if (player == null)
        {
            Logger.LogError(typeof(PlayerPanel), "Cannot initialize PlayerUI with null player");
            return;
        }

        StopUIUpdate();

        this.player = player;
        playerStat = player.GetComponent<PlayerStat>();

        if (playerStat == null)
        {
            Logger.LogError(typeof(PlayerPanel), "PlayerStat component not found on player!");
            return;
        }

        StartUIUpdate();
    }

    private void StartUIUpdate()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
        updateCoroutine = StartCoroutine(PlayerInfoUpdate());
    }

    private void StopUIUpdate()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
            updateCoroutine = null;
        }
    }

    private IEnumerator PlayerInfoUpdate()
    {
        while (true)
        {
            if (player != null && playerStat != null)
            {
                UpdatePlayerInfo();
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void UpdatePlayerInfo()
    {
        try
        {
            if (player == null || playerStat == null)
                return;
            UpdateCombatStats();
            UpdateHealthUI();
            UpdateExpUI();
            UpdateOtherStats();
        }
        catch (Exception e)
        {
            Logger.LogError(typeof(PlayerPanel), $"Error updating player info: {e.Message}");
            StopUIUpdate();
        }
    }

    private void UpdateCombatStats()
    {
        playerAtkText.text = $"ATK : {playerStat.GetStat(StatType.Damage):F1}";
        playerDefText.text = $"DEF : {playerStat.GetStat(StatType.Defense):F1}";
        levelText.text = $"LEVEL : {player.level}";
        playerMSText.text = $"MoveSpeed : {playerStat.GetStat(StatType.MoveSpeed):F1}";
    }

    private void UpdateHealthUI()
    {
        float currentHp = playerStat.GetStat(StatType.CurrentHp);
        float maxHp = playerStat.GetStat(StatType.MaxHp);
        hpText.text = $"{currentHp:F0} / {maxHp:F0}";
        hpBarImage.value = currentHp / maxHp;
    }

    private void UpdateExpUI()
    {
        if (player.level >= player._expList.Count)
        {
            expText.text = "MAX LEVEL";
            expBarImage.value = 1;
        }
        else
        {
            float currentExp = player.CurrentExp();
            float requiredExp = player.GetExpForNextLevel();
            expText.text = $"EXP : {currentExp:F0}/{requiredExp:F0}";
            expBarImage.value = player.ExpAmount;
        }
    }

    private void UpdateOtherStats()
    {
        expCollectRadText.text = $"ExpRad : {playerStat.GetStat(StatType.ExpCollectionRadius):F1}";
        playerHpRegenText.text = $"HPRegen : {playerStat.GetStat(StatType.HpRegenRate):F1}/s";
        playerAttackRangeText.text = $"AR : {playerStat.GetStat(StatType.AttackRange):F1}";
        playerAttackSpeedText.text = $"AS : {playerStat.GetStat(StatType.AttackSpeed):F1}/s";
    }

    public override void Close(bool objActive = true)
    {
        base.Close(objActive);
        StopUIUpdate();
        player = null;
        playerStat = null;
        PrepareUI();
    }
}
