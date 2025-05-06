using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSlot
    : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler,
        IDragHandler,
        IBeginDragHandler,
        IEndDragHandler
{
    [Serializable]
    class RaritySlot
    {
        public ItemRarity rarity;
        public GameObject slot;
    }

    #region Variables
    [Header("UI Components")]
    [SerializeField]
    private Image itemIcon;

    [SerializeField]
    private Animator hoverImage;

    [SerializeField]
    private List<RaritySlot> raritySlots;

    [SerializeField]
    private GameObject placeHolder;

    [SerializeField]
    private TextMeshProUGUI amountText;

    [Header("Slot Settings")]
    public SlotType slotType = SlotType.Storage;
    private InventoryPanel inventoryPanel;

    private Inventory inventory;
    private InventorySlot slotData;
    private ItemTooltip tooltip;
    #endregion

    public void Initialize(
        Inventory inventory,
        InventorySlot slotData,
        ItemTooltip tooltip,
        InventoryPanel inventoryPanel
    )
    {
        this.inventory = inventory;
        this.slotData = slotData;
        this.tooltip = tooltip;
        this.inventoryPanel = inventoryPanel;
    }

    #region UI Updates
    public void UpdateUI()
    {
        if (slotData == null || slotData.item == null)
        {
            SetSlotEmpty();
            return;
        }

        UpdateSlotVisuals(slotData.item.GetItemData(), slotData.amount);
    }

    private void SetSlotEmpty()
    {
        itemIcon.gameObject.SetActive(false);
        amountText.gameObject.SetActive(false);
        foreach (var raritySlot in raritySlots)
        {
            raritySlot.slot.SetActive(false);
        }
        placeHolder.SetActive(true);
    }

    public void UpdateSlotVisuals(ItemData itemData, int amount)
    {
        if (itemData == null)
            return;

        itemIcon.gameObject.SetActive(true);
        itemIcon.sprite = itemData.Icon;

        amountText.gameObject.SetActive(itemData.MaxStack > 1);
        if (amountText.gameObject.activeSelf)
        {
            amountText.text = amount.ToString();
        }

        foreach (var raritySlot in raritySlots)
        {
            raritySlot.slot.SetActive(raritySlot.rarity == itemData.Rarity);
        }
    }
    #endregion

    #region Item Interactions
    public void OnPointerClick(PointerEventData eventData)
    {
        if (slotData?.item == null)
            return;

        switch (eventData.button)
        {
            case PointerEventData.InputButton.Left:
                HandleLeftClick();
                break;
            case PointerEventData.InputButton.Right:
                HandleRightClick();
                break;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (slotData?.item == null)
            return;
        inventoryPanel.BeginItemDrag(
            this,
            itemIcon.sprite,
            new Vector2(50, 50),
            eventData.position
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        inventoryPanel.UpdateItemDrag(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ItemSlot targetSlot = null;
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var r in results)
        {
            targetSlot = r.gameObject.GetComponentInParent<ItemSlot>();
            if (targetSlot != null)
                break;
        }
        if (targetSlot == null)
        {
            inventoryPanel.EndItemDrag(this);
        }
        else
        {
            inventoryPanel.EndItemDrag(targetSlot);
        }
    }

    private void HandleRightClick()
    {
        if (slotData.slotType == SlotType.Storage)
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

        if (slotData.slotType != SlotType.Storage)
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

        UpdateUI();
    }

    private void DropItem()
    {
        if (slotData?.item == null)
            return;

        inventory.RemoveItem(slotData.item.GetItemData().ID);

        UpdateUI();
    }

    public void EquipItem(Item item)
    {
        Logger.Log(typeof(ItemSlot), $"Equipping {item.GetItemData().Name} to slot {slotType}");
        if (slotType == SlotType.Storage)
        {
            ItemType itemType = item.GetItemData().Type;
            SlotType targetSlotType = new SlotType();
            switch (itemType)
            {
                case ItemType.Weapon:
                    targetSlotType = SlotType.Weapon;
                    break;
                case ItemType.Armor:
                    targetSlotType = SlotType.Armor;
                    break;
                case ItemType.Accessory:
                    if (item.GetItemData().AccessoryType == AccessoryType.Necklace)
                    {
                        targetSlotType = SlotType.Necklace;
                    }
                    else
                    {
                        if (item.GetItemData().AccessoryType == AccessoryType.Ring)
                        {
                            targetSlotType = SlotType.Ring;
                        }
                    }
                    break;
            }
            inventory.EquipItem(item, targetSlotType);
            slotData.isEmpty = true;
            slotData.item = null;
            UpdateUI();
        }
        else
        {
            inventory.EquipItem(item, slotType);
        }
    }

    public void UnequipItem()
    {
        inventory.UnequipItem(slotType);
        inventoryPanel.UpdateUI();
    }

    public void EquipItemTo(ItemSlot targetSlot)
    {
        var item = GetItem();
        if (item == null)
            return;
        targetSlot.EquipItem(item);
        slotData.isEmpty = true;
        slotData.item = null;
        UpdateUI();
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
        hoverImage.SetBool("subOpen", true);

        if (slotData?.item != null)
        {
            ShowTooltip(slotData.item.GetItemData());
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hoverImage.SetBool("subOpen", false);
        HideTooltip();
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void ShowTooltip(ItemData itemData)
    {
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

    public InventorySlot GetSlotData() => slotData;

    public void SetSlotData(InventorySlot data)
    {
        slotData = data;
        UpdateUI();
    }

    public Item GetItem() => slotData?.item;
    #endregion
}
