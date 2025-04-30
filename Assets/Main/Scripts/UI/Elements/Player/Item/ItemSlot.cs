using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSlot
    : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
{
    #region Variables
    [Header("UI Components")]
    [SerializeField]
    private Image itemIcon;

    [SerializeField]
    private Image backgroundImage;

    [SerializeField]
    private GameObject hoverImage;

    [SerializeField]
    private TextMeshProUGUI amountText;

    [Header("Slot Settings")]
    public ItemType slotType = ItemType.None;
    public AccessoryType accessoryType = AccessoryType.None;

    private Inventory inventory;
    private InventorySlot slotData;
    private ItemTooltip tooltip;

    public bool isInventorySlot;
    #endregion

    public void Initialize(Inventory inventory, ItemTooltip tooltip)
    {
        this.inventory = inventory;
        this.tooltip = tooltip;
    }

    #region UI Updates
    public void UpdateUI(InventorySlot slot)
    {
        slotData = slot;

        if (slot == null || slot.item == null)
        {
            SetSlotEmpty();
            return;
        }

        UpdateSlotVisuals(slot.item.GetItemData(), slot.amount, slot.isEquipped);
    }

    private void SetSlotEmpty()
    {
        itemIcon.enabled = false;
        amountText.enabled = false;
    }

    private void UpdateSlotVisuals(ItemData itemData, int amount, bool isEquipped)
    {
        if (itemData == null)
            return;

        itemIcon.enabled = true;
        itemIcon.sprite = itemData.Icon;

        amountText.enabled = itemData.MaxStack > 1;
        if (amountText.enabled)
        {
            amountText.text = amount.ToString();
        }

        backgroundImage.color = GetRarityColor(itemData.Rarity);
    }
    #endregion

    #region Item Interactions
    public void OnPointerClick(PointerEventData eventData)
    {
        if (slotData?.item == null)
            return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            HandleRightClick();
            return;
        }

        HandleLeftClick();
    }

    private void HandleRightClick()
    {
        if (!isInventorySlot)
        {
            UIManager.Instance.OpenPopUp(
                "Warning",
                "Are you sure you want to drop this item?",
                () =>
                {
                    DropItem();
                }
            );
        }
        else
        {
            UnequipItem();
        }
    }

    private void HandleLeftClick()
    {
        var item = slotData.item;
        if (item == null)
        {
            Logger.LogError(
                typeof(ItemSlot),
                $"Failed to get item data for ID: {slotData.item.GetItemData().ID}"
            );
            return;
        }

        if (isInventorySlot)
        {
            UnequipItem();
        }
        else
        {
            if (IsEquippableItem(item.GetItemData().Type))
            {
                EquipItem(item);
            }
        }

        UpdateUI(slotData);
    }

    private void DropItem()
    {
        if (slotData?.item == null)
            return;

        inventory.RemoveItem(slotData.item.GetItemData().ID);
        UpdateUI(slotData);
    }

    private void UnequipItem()
    {
        var equipSlot = GetEquipmentSlot(slotData.item.GetItemData().Type, accessoryType);
        inventory.UnequipItem(equipSlot);
    }

    private void EquipItem(Item item)
    {
        var equipSlot = GetEquipmentSlot(item.GetItemData().Type, accessoryType);
        if (equipSlot != SlotType.Storage)
        {
            Logger.Log(
                typeof(ItemSlot),
                $"Equipping {item.GetItemData().Name} to slot {equipSlot}"
            );
            inventory.EquipItem(item, equipSlot);
        }
    }

    private bool IsEquippableItem(ItemType itemType)
    {
        return itemType == ItemType.Weapon
            || itemType == ItemType.Armor
            || itemType == ItemType.Accessory;
    }
    #endregion

    #region Tooltip
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slotData?.item != null)
        {
            hoverImage.SetActive(true);
            ShowTooltip(slotData.item.GetItemData());
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void ShowTooltip(ItemData itemData)
    {
        if (tooltip != null)
            return;

        if (tooltip != null)
        {
            tooltip.SetupTooltip(itemData);
            tooltip.Show(Input.mousePosition);
        }
    }

    private void HideTooltip()
    {
        if (tooltip != null)
        {
            tooltip.Hide();
        }
    }
    #endregion

    #region Equipment Slot Utilities
    private SlotType GetEquipmentSlot(ItemType itemType, AccessoryType accessoryType)
    {
        return itemType switch
        {
            ItemType.Weapon => SlotType.Weapon,
            ItemType.Armor => SlotType.Armor,
            ItemType.Accessory => GetAccessorySlot(accessoryType),
            _ => SlotType.Storage,
        };
    }

    private SlotType GetAccessorySlot(AccessoryType accessoryType)
    {
        if (accessoryType == AccessoryType.Ring)
        {
            return SlotType.Ring;
        }
        if (accessoryType == AccessoryType.Necklace)
        {
            return SlotType.Necklace;
        }

        return SlotType.Storage;
    }

    private Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => Color.white,
            ItemRarity.Uncommon => new Color(0.3f, 1f, 0.3f),
            ItemRarity.Rare => new Color(0.3f, 0.5f, 1f),
            ItemRarity.Epic => new Color(0.8f, 0.3f, 1f),
            ItemRarity.Legendary => new Color(1f, 0.8f, 0.2f),
            _ => Color.white,
        };
    }

    #endregion
}
