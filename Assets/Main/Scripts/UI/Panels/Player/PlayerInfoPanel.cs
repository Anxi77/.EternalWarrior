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

    private Player player;

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
    }

    public void Initialize()
    {
        hpBar.SetValue(0);
        expBar.SetValue(0);
        hpBar.maxValue = 1;
        expBar.maxValue = 1;
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
        if (player.level >= player._expList.Count)
        {
            expBar.SetValue(requiredExp);
        }
        else
        {
            expBar.SetValue(currentExp);
        }
    }
}
