using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillPanel : Panel
{
    [Serializable]
    public class ElementSprite
    {
        public ElementType element;
        public Sprite sprite;
    }

    public override PanelType PanelType => PanelType.Skill;

    [Header("UI References")]
    [SerializeField]
    private RectTransform buttonParent;

    [SerializeField]
    private SkillButton buttonPrefab;

    [SerializeField]
    private Image BG;

    [SerializeField]
    private List<ElementSprite> elementSprites;
    private List<SkillButton> skillButtons = new List<SkillButton>();

    [Header("Settings")]
    private const int SKILL_CHOICES = 3;

    public override void Open()
    {
        Initialize();
        base.Open();
        Time.timeScale = 0;
    }

    public override void Close(bool objActive = true)
    {
        ClearButtons();
        base.Close(objActive);
        Time.timeScale = 1;
    }

    public void Initialize()
    {
        gameObject.SetActive(true);

        var playerSkills = GameManager.Instance.PlayerSystem.Player.skills;

        var availableSkills = GetAvailableSkills(playerSkills);

        if (!availableSkills.Any())
        {
            Close();
            return;
        }

        var element = availableSkills.First().Element;
        BG.sprite = GetElementSprite(element);

        foreach (var skillData in availableSkills)
        {
            CreateSkillUpgradeButton(skillData, playerSkills);
        }
    }

    private Sprite GetElementSprite(ElementType element)
    {
        return elementSprites.First(e => e.element == element).sprite;
    }

    private List<SkillData> GetAvailableSkills(List<Skill> playerSkills)
    {
        return GameManager
            .Instance.SkillSystem.GetRandomSkills(SKILL_CHOICES)
            .Where(skillData => IsSkillAvailable(skillData, playerSkills))
            .ToList();
    }

    private bool IsSkillAvailable(SkillData skillData, List<Skill> playerSkills)
    {
        var existingSkill = playerSkills.Find(s => s.skillData.ID == skillData.ID);
        return existingSkill == null
            || existingSkill.skillData.GetSkillStats().baseStat.skillLevel
                < existingSkill.skillData.GetSkillStats().baseStat.maxSkillLevel;
    }

    private void CreateSkillUpgradeButton(SkillData skillData, List<Skill> playerSkills)
    {
        var existingSkill = playerSkills.Find(s => s.skillData.ID == skillData.ID);
        var button = Instantiate(buttonPrefab, buttonParent);
        var upgradeInfo = GetUpgradeInfo(existingSkill);

        skillButtons.Add(button);

        button.SetSkillSelectButton(
            skillData,
            CreateButtonCallback(skillData, existingSkill),
            upgradeInfo.levelText,
            upgradeInfo.currentStats
        );
    }

    private Action CreateButtonCallback(SkillData skillData, Skill existingSkill)
    {
        return new Action(() => OnSkillButtonClicked(skillData, existingSkill));
    }

    private (string levelText, ISkillStat currentStats) GetUpgradeInfo(Skill existingSkill)
    {
        if (existingSkill != null)
        {
            return (
                $"Lv.{existingSkill.skillData.GetSkillStats().baseStat.skillLevel} → {existingSkill.skillData.GetSkillStats().baseStat.skillLevel + 1}",
                existingSkill.skillData.GetSkillStats()
            );
        }
        return ("New!", null);
    }

    private void OnSkillButtonClicked(SkillData skillData, Skill existingSkill)
    {
        if (existingSkill != null)
        {
            GameManager.Instance.PlayerSystem.Player.AddOrUpgradeSkill(skillData);
            var updatedSkill = GameManager.Instance.PlayerSystem.Player.skills.Find(s =>
                s.skillData.ID == skillData.ID
            );
        }
        else
        {
            if (GameManager.Instance.PlayerSystem.Player.AddOrUpgradeSkill(skillData))
            {
                var newSkill = GameManager.Instance.PlayerSystem.Player.skills.Find(s =>
                    s.skillData.ID == skillData.ID
                );
            }
        }

        Close(false);
    }

    private void ClearButtons()
    {
        Logger.Log(GetType(), "ClearButtons");
        foreach (var button in skillButtons)
        {
            Destroy(button.gameObject);
        }
        skillButtons.Clear();
    }
}
