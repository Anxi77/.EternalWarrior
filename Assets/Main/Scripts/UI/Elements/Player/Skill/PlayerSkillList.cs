using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerSkillList : MonoBehaviour
{
    public PlayerSkillIcon skillIconPrefab;
    private List<PlayerSkillIcon> currentIcons = new List<PlayerSkillIcon>();

    [SerializeField]
    private RectTransform layoutGroup;

    public void UpdateSkillList()
    {
        ClearCurrentIcons();

        Player player = GameManager.Instance.PlayerSystem.Player;

        if (player == null)
        {
            return;
        }

        if (player.skills != null)
        {
            var sortedSkills = player
                .skills.Where(s => s != null)
                .OrderBy(s => s.GetSkillData()?.Type)
                .ThenByDescending(s => s.currentLevel)
                .ToList();

            foreach (Skill skill in sortedSkills)
            {
                SkillData skillData = skill.GetSkillData();
                Logger.Log(GetType(), $"Skill: {skill.name}, SkillData: {skillData.Icon}");
                if (skillData != null)
                {
                    PlayerSkillIcon icon = Instantiate(skillIconPrefab, layoutGroup);
                    icon.SetSkillIcon(skillData.Icon, skill);
                    currentIcons.Add(icon);
                }
                else
                {
                    Logger.LogWarning(
                        typeof(PlayerSkillList),
                        $"Skill data is null for skill: {skill.name}"
                    );
                }
            }
        }
        else
        {
            Logger.LogError(typeof(PlayerSkillList), "Player skills list is null");
        }
    }

    private void ClearCurrentIcons()
    {
        foreach (var icon in currentIcons)
        {
            if (icon != null)
                Destroy(icon.gameObject);
        }
        currentIcons.Clear();
    }
}
