using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryPanel : Panel
{
    public override PanelType PanelType => PanelType.Inventory;

    #region Variables
    [Header("Settings")]
    [SerializeField]
    private GameObject inventoryPanel;

    [SerializeField]
    private ItemSlot[] equipmentSlots;

    [SerializeField]
    private Transform slotsParent;

    [SerializeField]
    private ItemSlot slotPrefab;

    private Inventory inventory;
    private List<ItemSlot> slotUIs = new();

    [SerializeField]
    private ItemTooltip itemTooltip;

    public bool IsInitialized { get; private set; }
    #endregion

    #region Initialization

    public void SetupInventory(Inventory inventory)
    {
        this.inventory = inventory;
        if (inventory == null)
        {
            Debug.LogError("Inventory component not found on player!");
            return;
        }

        InitializeUI();
        inventoryPanel.SetActive(false);
        IsInitialized = true;
    }

    public override void Open()
    {
        base.Open();
    }

    public override void Close(bool objActive = true)
    {
        base.Close(objActive);
    }

    private void InitializeUI()
    {
        itemTooltip = Resources.Load<ItemTooltip>("UI/Elements/ItemTooltip");
        InitializeEquipmentSlots();
        InitializeInventorySlots();
    }

    private void InitializeEquipmentSlots()
    {
        if (equipmentSlots == null)
        {
            Debug.LogError("Equipment slots array is null!");
            return;
        }

        foreach (var equipSlot in equipmentSlots)
        {
            if (equipSlot != null)
            {
                equipSlot.Initialize(inventory, itemTooltip);
            }
        }
    }

    private void InitializeInventorySlots()
    {
        for (int i = 0; i < inventory.MaxSlots; i++)
        {
            var slotUI = Instantiate(slotPrefab, slotsParent);
            slotUI.Initialize(inventory, itemTooltip);
            slotUIs.Add(slotUI);
            slotUI.isInventorySlot = true;
        }
    }
    #endregion

    #region UI Updates
    public void UpdateUI()
    {
        if (!IsInitialized || inventory == null)
        {
            Debug.LogWarning("Cannot update UI: Inventory not initialized");
            return;
        }

        try
        {
            UpdateInventorySlots();
            UpdateEquipmentSlots();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error updating inventory UI: {e.Message}\n{e.StackTrace}");
        }
    }

    private void UpdateInventorySlots()
    {
        var slots = inventory.GetSlots();
        for (int i = 0; i < slotUIs.Count; i++)
        {
            slotUIs[i].UpdateUI(i < slots.Count ? slots[i] : null);
        }
    }

    private void UpdateEquipmentSlots()
    {
        if (equipmentSlots != null)
        {
            foreach (var equipSlot in equipmentSlots)
            {
                if (equipSlot != null)
                {
                    UpdateEquipmentSlot(equipSlot);
                }
            }
        }
    }

    private void UpdateEquipmentSlot(ItemSlot equipSlot)
    {
        try
        {
            if (inventory == null)
            {
                Debug.LogWarning("Inventory is null");
                return;
            }

            var equipmentSlot = GetEquipmentSlotFromSlotType(equipSlot.slotType);
            if (equipmentSlot == SlotType.Storage)
            {
                Debug.LogWarning($"Invalid slot type: {equipSlot.slotType}");
                return;
            }

            InventorySlot equippedSlot = null;
            switch (equipmentSlot)
            {
                case SlotType.Weapon:
                    equippedSlot = inventory.GetEquippedItemSlot(ItemType.Weapon);
                    break;
                case SlotType.Armor:
                    equippedSlot = inventory.GetEquippedItemSlot(ItemType.Armor);
                    break;
                case SlotType.Ring:
                    equippedSlot = inventory.GetEquippedItemSlot(
                        ItemType.Accessory,
                        AccessoryType.Ring
                    );
                    break;
                case SlotType.Necklace:
                    equippedSlot = inventory.GetEquippedItemSlot(
                        ItemType.Accessory,
                        AccessoryType.Necklace
                    );
                    break;
                default:
                    break;
            }

            if (equippedSlot != null)
            {
                equipSlot.UpdateUI(equippedSlot);
            }
            else
            {
                equipSlot.UpdateUI(null);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating equipment slot: {e.Message}\n{e.StackTrace}");
        }
    }
    #endregion

    #region Utilities
    private SlotType GetEquipmentSlotFromSlotType(ItemType itemType)
    {
        return itemType switch
        {
            ItemType.Weapon => SlotType.Weapon,
            ItemType.Armor => SlotType.Armor,
            ItemType.Accessory => GetFirstEmptyAccessorySlot(),
            _ => SlotType.Storage,
        };
    }

    private SlotType GetFirstEmptyAccessorySlot()
    {
        if (inventory.GetEquippedItem(ItemType.Accessory, AccessoryType.Ring) == null)
        {
            return SlotType.Ring;
        }
        else if (inventory.GetEquippedItem(ItemType.Accessory, AccessoryType.Necklace) == null)
        {
            return SlotType.Necklace;
        }
        else
        {
            return SlotType.Storage;
        }
    }

    #endregion
}
