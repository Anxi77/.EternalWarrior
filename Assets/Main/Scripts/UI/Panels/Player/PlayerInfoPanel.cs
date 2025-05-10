using System.Collections;
using Michsky.UI.Heat;
using TMPro;
using UnityEngine;

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

    [SerializeField]
    private Animator itemAnimator;

    [SerializeField]
    private TextMeshProUGUI itemText;

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
        if (player != null)
        {
            player.OnHpChanged -= UpdateHealthUI;
            player.OnExpChanged -= UpdateExpUI;
            player.OnLevelUp -= OnLevelUp;
            player = null;
        }
        isFirstSkillListUpdate = true;
        skillList.gameObject.SetActive(false);
    }

    public void OnLevelUp()
    {
        expBar.SetValue(0);
        levelText.text = player.level.ToString();
        UpdateSkillList();
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

    public void ShowItemUI(string itemName)
    {
        StartCoroutine(ItemUIRoutine(itemName));
    }

    public IEnumerator ItemUIRoutine(string itemName)
    {
        itemText.text = $"Item Earned {itemName}";
        itemAnimator.SetBool("subOpen", true);
        yield return new WaitForSeconds(1f);
        itemAnimator.SetBool("subOpen", false);
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
        player.OnLevelUp += OnLevelUp;
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
            skillList.UpdateSkillList();
            isFirstSkillListUpdate = false;
        }
        else
        {
            skillList.UpdateSkillList();
        }
    }
}
