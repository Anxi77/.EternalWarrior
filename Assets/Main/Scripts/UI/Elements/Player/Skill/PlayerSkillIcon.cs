using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSkillIcon : MonoBehaviour
{
    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private TextMeshProUGUI levelText;

    public void SetSkillIcon(Sprite iconSprite, Skill skill)
    {
        try
        {
            if (iconImage != null)
            {
                if (iconSprite == null)
                {
                    iconSprite = Resources.Load<Sprite>("Icons/Default/SkillIcon");
                    Logger.LogWarning(
                        typeof(PlayerSkillIcon),
                        $"Using default icon for skill: {skill?.GetType().Name}"
                    );
                }
                iconImage.sprite = iconSprite;
                iconImage.gameObject.SetActive(iconSprite != null);
            }
            else
            {
                Logger.LogError(typeof(PlayerSkillIcon), "Icon Image component is missing!");
            }

            if (levelText != null && skill != null)
            {
                levelText.text = $"Lv.{skill.currentLevel}";
                levelText.gameObject.SetActive(true);
            }
        }
        catch (Exception e)
        {
            Logger.LogError(typeof(PlayerSkillIcon), $"Error setting skill icon: {e.Message}");
        }
    }
}
