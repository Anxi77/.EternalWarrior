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
        IEndDragHandler,
        IDropHandler
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
    private TextMeshProUGUI amountText;

    [Header("Slot Settings")]
    public ItemType slotType = ItemType.None;
    public AccessoryType accessoryType = AccessoryType.None;

    private Inventory inventory;
    private InventorySlot slotData;
    private ItemTooltip tooltip;

    public bool isInventorySlot;
    public Vector2 originalPositon;

    private Vector3 dragStartPosition;
    private Transform originalParent;
    private Canvas canvas;
    #endregion

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();
    }

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

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            HandleRightClick();
            return;
        }

        HandleLeftClick();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // if (slotData?.item == null)
        //     return;

        dragStartPosition = transform.position;
        originalParent = transform.parent;

        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();

        GetComponent<CanvasGroup>().blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // if (slotData?.item == null)
        //     return;

        GetComponent<CanvasGroup>().blocksRaycasts = true;

        var dropSlot = GetDropSlot(eventData);

        if (dropSlot != null)
        {
            return;
        }

        transform.SetParent(originalParent);
        transform.position = dragStartPosition;
    }

    private ItemSlot GetDropSlot(PointerEventData eventData)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            ItemSlot slot = result.gameObject.GetComponent<ItemSlot>();
            if (slot != null && slot != this)
            {
                return slot;
            }
        }

        return null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        ItemSlot draggedSlot = eventData.pointerDrag.GetComponent<ItemSlot>();
        if (draggedSlot != null && draggedSlot != this)
        {
            itemIcon.sprite = draggedSlot.itemIcon.sprite;
            draggedSlot.itemIcon.sprite = null;
        }
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
