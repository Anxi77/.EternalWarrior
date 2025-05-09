using System;
using System.Collections;
using Michsky.UI.Heat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoPanel : Panel
{
    public override PanelType PanelType => PanelType.PlayerInfo;

    [SerializeField]
    private PlayerSkillList skillList;

    [Header("UI Bars")]
    [SerializeField]
    private ProgressBar hpBar;

    [SerializeField]
    private ProgressBar expBar;

    [SerializeField]
    private TextMeshProUGUI levelText;

    private Player player;

    private bool isFirstSkillListUpdate = true;

    public override void Open()
    {
        Initialize();
        base.Open();
    }

    public override void Close(bool objActive = true)
    {
        base.Close(objActive);
        player.OnHpChanged -= UpdateHealthUI;
        player.OnExpChanged -= UpdateExpUI;
        player = null;
        isFirstSkillListUpdate = true;
        skillList.gameObject.SetActive(false);
    }

    public void Initialize()
    {
        hpBar.SetValue(0);
        expBar.SetValue(0);
        hpBar.maxValue = 1;
        expBar.maxValue = 1;
        levelText.text = "1";
        skillList.gameObject.SetActive(false);
    }

    public void InitializePlayerUI(Player player)
    {
        if (player == null)
        {
            Logger.LogError(typeof(PlayerInfoPanel), "Cannot initialize PlayerUI with null player");
            return;
        }

        this.player = player;
        float maxHp = player.playerStat.GetStat(StatType.MaxHp);
        float currentHp = player.playerStat.GetStat(StatType.CurrentHp);
        hpBar.maxValue = maxHp;
        hpBar.SetValue(currentHp);
        levelText.text = player.level.ToString();
        player.OnHpChanged += UpdateHealthUI;
        player.OnExpChanged += UpdateExpUI;
    }

    private void UpdateHealthUI(float currentHp, float maxHp)
    {
        hpBar.maxValue = maxHp;
        hpBar.SetValue(currentHp);
    }

    private void UpdateExpUI(float currentExp, float requiredExp)
    {
        expBar.maxValue = requiredExp;
        if (player.level >= player.ExpList.Count)
        {
            expBar.SetValue(requiredExp);
        }
        else
        {
            expBar.SetValue(currentExp);
        }
        if (player.level >= player.ExpList.Count)
        {
            levelText.text = "MAX";
        }
        else
        {
            levelText.text = player.level.ToString();
        }
    }

    public void UpdateSkillList()
    {
        if (isFirstSkillListUpdate)
        {
            skillList.gameObject.SetActive(true);
            isFirstSkillListUpdate = false;
            return;
        }
        skillList.UpdateSkillList();
    }
}
