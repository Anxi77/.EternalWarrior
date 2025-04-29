using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltip : MonoBehaviour
{
    [SerializeField]
    private Image itemIcon;

    [SerializeField]
    private TextMeshProUGUI itemNameText;

    [SerializeField]
    private TextMeshProUGUI itemTypeText;

    [SerializeField]
    private TextMeshProUGUI itemRarityText;

    [SerializeField]
    private TextMeshProUGUI itemStatsText;

    [SerializeField]
    private TextMeshProUGUI itemEffectsText;

    [SerializeField]
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        ValidateComponents();
        Hide();
    }

    private void ValidateComponents()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    public void SetupTooltip(ItemData itemData)
    {
        if (itemData == null)
        {
            Logger.LogError(typeof(ItemTooltip), "Attempted to setup tooltip with null ItemData");
            return;
        }

        Logger.Log(typeof(ItemTooltip), $"Setting up tooltip for item: {itemData.Name}");
        Logger.Log(typeof(ItemTooltip), $"Item stats count: {itemData.Stats?.Count ?? 0}");

        itemNameText.text = $"{GetRarityColor(itemData.Rarity)}{itemData.Name}</color>";
        itemTypeText.text = $"Type: {itemData.Type}";
        itemRarityText.text = $"Rarity: {itemData.Rarity}";

        if (itemIcon != null)
        {
            itemIcon.sprite = itemData.Icon;
            itemIcon.enabled = itemData.Icon != null;
        }

        var statsBuilder = new StringBuilder("Stats:\n");
        if (itemData.Stats != null && itemData.Stats.Any())
        {
            foreach (var stat in itemData.Stats)
            {
                string valueStr = stat.Value >= 0 ? "+" + stat.Value : stat.Value.ToString();
                statsBuilder.AppendLine($"{stat.Type}: {valueStr}");
                Logger.Log(
                    typeof(ItemTooltip),
                    $"Adding stat to tooltip: {stat.Type} = {valueStr}"
                );
            }
        }
        else
        {
            statsBuilder.AppendLine("No stats");
            Logger.Log(typeof(ItemTooltip), "No stats found for item");
        }
        itemStatsText.text = statsBuilder.ToString();

        var effectsBuilder = new StringBuilder("Effects:\n");
        if (itemData.Effects != null && itemData.Effects.Any())
        {
            foreach (var effect in itemData.Effects)
            {
                effectsBuilder.AppendLine($"{effect.effectName}");
                Logger.Log(typeof(ItemTooltip), $"Adding effect to tooltip: {effect.effectName}");
            }
        }
        else
        {
            effectsBuilder.AppendLine("No effects");
            Logger.Log(typeof(ItemTooltip), "No effects found for item");
        }
        itemEffectsText.text = effectsBuilder.ToString();

        Logger.Log(typeof(ItemTooltip), $"Tooltip setup complete for {itemData.Name}");
    }

    private string GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => "<color=white>",
            ItemRarity.Uncommon => "<color=#00FF00>",
            ItemRarity.Rare => "<color=#0080FF>",
            ItemRarity.Epic => "<color=#CC33FF>",
            ItemRarity.Legendary => "<color=#FFD700>",
            _ => "<color=white>",
        };
    }

    public void Show(Vector2 position)
    {
        transform.position = position;
        canvasGroup.alpha = 1f;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        Clear();
        gameObject.SetActive(false);
    }

    public void Clear()
    {
        itemNameText.text = "";
        itemTypeText.text = "";
        itemRarityText.text = "";
        itemStatsText.text = "";
        itemEffectsText.text = "";
    }
}
