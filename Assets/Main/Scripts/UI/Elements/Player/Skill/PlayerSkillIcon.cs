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

    [SerializeField]
    private Image elementalBorder;

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

            if (elementalBorder != null && skill != null)
            {
                ElementType element = skill.GetSkillData()?.Element ?? ElementType.None;
                elementalBorder.color = GetElementColor(element);
                elementalBorder.gameObject.SetActive(element != ElementType.None);
            }
        }
        catch (Exception e)
        {
            Logger.LogError(typeof(PlayerSkillIcon), $"Error setting skill icon: {e.Message}");
        }
    }

    private Color GetElementColor(ElementType element)
    {
        return element switch
        {
            ElementType.Fire => new Color(1f, 0.3f, 0.3f, 0.5f),
            ElementType.Water => new Color(0.3f, 0.3f, 1f, 0.5f),
            ElementType.Earth => new Color(0.3f, 0.8f, 0.3f, 0.5f),
            ElementType.Dark => new Color(0.5f, 0.2f, 0.7f, 0.5f),
            _ => new Color(1f, 1f, 1f, 0f),
        };
    }

    private void OnValidate()
    {
        if (iconImage == null)
            iconImage = GetComponent<Image>();

        if (levelText == null)
            levelText = GetComponentInChildren<TextMeshProUGUI>();

        if (elementalBorder == null)
            elementalBorder = transform.Find("ElementalBorder")?.GetComponent<Image>();
    }
}
