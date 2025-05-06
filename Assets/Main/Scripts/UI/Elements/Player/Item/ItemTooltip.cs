using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltip : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private ItemSlot itemSlot;

    [SerializeField]
    private Image[] headerBorders;

    [SerializeField]
    private TextMeshProUGUI itemNameText;

    [SerializeField]
    private TextMeshProUGUI itemTypeText;

    [SerializeField]
    private TextMeshProUGUI itemRarityText;

    [SerializeField]
    private TextMeshProUGUI itemStatsText;

    [SerializeField]
    private TextMeshProUGUI itemDescText;

    [SerializeField]
    private TextMeshProUGUI mainStatValueText;

    [SerializeField]
    private GameObject defIcon;

    [SerializeField]
    private GameObject atkIcon;

    [SerializeField]
    private Transform effectBoxContainer;

    [SerializeField]
    private EffectBox effectBoxPrefab;

    [SerializeField]
    private List<EffectBox> effectBoxes;

    [SerializeField]
    public RectTransform rectTransform;

    public void SetupTooltip(ItemData itemData)
    {
        if (itemData == null)
        {
            Logger.LogWarning(typeof(ItemTooltip), "Attempted to setup tooltip with null ItemData");
            return;
        }

        SetTexts(itemData);
        SetMainStat(itemData);
        SetEffects(itemData);
        SetHeaderBorders(itemData.Rarity);
    }

    private void SetTexts(ItemData itemData)
    {
        itemNameText.text =
            $"<color={RarityColor.GetRarityColorString(itemData.Rarity)}>{itemData.Name}</color>";
        itemTypeText.text = itemData.Type.ToString();
        itemRarityText.text =
            $"<color={RarityColor.GetRarityColorString(itemData.Rarity)}>{itemData.Rarity}</color>";
        itemDescText.text = itemData.Description;
        itemSlot.UpdateSlotVisuals(itemData, 1);
        itemStatsText.text = BuildStatsText(itemData);
    }

    private string BuildStatsText(ItemData itemData)
    {
        if (itemData.Stats == null || !itemData.Stats.Any())
            return "No stats";

        var builder = new StringBuilder();
        foreach (var stat in itemData.Stats)
        {
            if (stat.Type == StatType.Defense || stat.Type == StatType.Damage)
                continue;
            builder.AppendLine(FormatStat(stat, itemData.Type));
        }
        return builder.ToString();
    }

    private void SetMainStat(ItemData itemData)
    {
        mainStatValueText.text = "0";
        atkIcon.SetActive(false);
        defIcon.SetActive(false);

        StatModifier mainStat = null;
        if (itemData.Type == ItemType.Weapon)
        {
            atkIcon.SetActive(true);
            mainStat = itemData.Stats?.Find(s => s.Type == StatType.Damage);
        }
        else if (itemData.Type == ItemType.Armor)
        {
            defIcon.SetActive(true);
            mainStat = itemData.Stats?.Find(s => s.Type == StatType.Defense);
        }

        if (mainStat != null)
            mainStatValueText.text = FormatStat(mainStat, itemData.Type, true);
    }

    private string FormatStat(StatModifier stat, ItemType itemType, bool isMainStat = false)
    {
        var inventory = GameManager.Instance.PlayerSystem?.Player?.inventory;
        var equipped = inventory?.GetEquippedItem(itemType);
        var equippedStat = equipped?.GetItemData().Stats.Find(s => s.Type == stat.Type);

        int equippedValue = (int)(equippedStat?.Value ?? 0);
        int statValue = (int)stat.Value;
        int diff = statValue - equippedValue;
        string color = diff >= 0 ? "#20E070" : "#FF0000";
        string sign = diff >= 0 ? "+" : "-";

        if (isMainStat)
            return $"<color={color}> {sign}{Mathf.Abs(statValue)}</color>";

        return $"{stat.Type} <color={color}> {sign}{Mathf.Abs(statValue)}</color>";
    }

    private void SetEffects(ItemData itemData)
    {
        ClearEffects();
        if (itemData.Effects == null || !itemData.Effects.Any())
            return;

        foreach (var effect in itemData.Effects)
        {
            var eb = PoolManager.Instance.Spawn<EffectBox>(
                effectBoxPrefab.gameObject,
                effectBoxContainer.position,
                Quaternion.identity
            );
            eb.transform.SetParent(effectBoxContainer);
            eb.SetEffect(effect);
            effectBoxes.Add(eb);
        }
    }

    private void ClearEffects()
    {
        foreach (var effectBox in effectBoxes)
            PoolManager.Instance.Despawn(effectBox);
        effectBoxes.Clear();
    }

    private void SetHeaderBorders(ItemRarity rarity)
    {
        var color = RarityColor.GetRarityColor(rarity);
        foreach (var border in headerBorders)
            border.color = color;
    }

    private void ForceUpdateLayout()
    {
        for (int i = 0; i < 3; i++)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }

    public void Show(Vector2 position)
    {
        gameObject.SetActive(true);
        ForceUpdateLayout();

        Vector2 tooltipSize = rectTransform.sizeDelta * rectTransform.lossyScale;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        Vector2 pivot = new Vector2(0, 1);

        if (position.x + tooltipSize.x > screenSize.x)
            pivot.x = 1;
        if (position.y - tooltipSize.y < 0)
            pivot.y = 0;

        rectTransform.pivot = pivot;

        rectTransform.position = position;

        animator.SetBool("subOpen", true);
    }

    public void Hide()
    {
        animator.SetBool("subOpen", false);
        Clear();
        gameObject.SetActive(false);
    }

    public void Clear()
    {
        itemNameText.text = "";
        itemTypeText.text = "";
        itemRarityText.text = "";
        itemStatsText.text = "";
        mainStatValueText.text = "";
        defIcon.SetActive(false);
        atkIcon.SetActive(false);
        foreach (var border in headerBorders)
        {
            border.color = Color.white;
        }
        ClearEffects();
    }
}
