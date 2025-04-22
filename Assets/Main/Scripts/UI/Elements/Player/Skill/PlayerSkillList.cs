using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerSkillList : MonoBehaviour
{
    public PlayerSkillIcon skillIconPrefab;
    private List<PlayerSkillIcon> currentIcons = new List<PlayerSkillIcon>();

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
                if (skillData != null)
                {
                    PlayerSkillIcon icon = Instantiate(skillIconPrefab, transform);
                    icon.SetSkillIcon(skillData.Icon, skill);
                    currentIcons.Add(icon);
                }
                else
                {
                    Debug.LogWarning($"Skill data is null for skill: {skill.name}");
                }
            }
        }
        else
        {
            Debug.LogError("Player skills list is null");
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
