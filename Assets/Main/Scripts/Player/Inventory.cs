using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    private List<InventorySlot> inventorySlots = new();
    private List<InventorySlot> equipmentSlots = new();

    [SerializeField]
    private Player player;

    [SerializeField]
    private int gold = 0;
    public const int MAX_SLOTS = 20;
    public bool IsInitialized { get; private set; }
    public int MaxSlots => MAX_SLOTS;

    private InventoryPanel inventoryPanel;

    public void Initialize(Player player)
    {
        if (!IsInitialized)
        {
            this.player = player;

            foreach (SlotType slotType in Enum.GetValues(typeof(SlotType)))
            {
                if (slotType == SlotType.Storage)
                {
                    continue;
                }
                if (slotType == SlotType.Ring)
                {
                    equipmentSlots.Add(
                        new InventorySlot() { slotType = SlotType.Ring, isEquipmentSlot = true }
                    );
                }
                equipmentSlots.Add(
                    new InventorySlot() { slotType = slotType, isEquipmentSlot = true }
                );
            }

            for (int i = 0; i < MAX_SLOTS; i++)
            {
                inventorySlots.Add(
                    new InventorySlot() { slotType = SlotType.Storage, isEquipmentSlot = false }
                );
            }

            LoadInventoryData();

            inventoryPanel = UIManager.Instance.GetPanel(PanelType.Inventory) as InventoryPanel;

            if (inventoryPanel != null)
            {
                inventoryPanel.SetupInventory(this);
            }

            IsInitialized = true;
        }
    }

    private void LoadInventoryData()
    {
        var savedData = PlayerDataManager.Instance.LoadPlayerData();
        if (savedData != null)
        {
            LoadInventoryData(savedData.inventory);
        }
    }

    public List<InventorySlot> GetSlots()
    {
        return new List<InventorySlot>(inventorySlots);
    }

    public InventoryData GetInventoryData()
    {
        return new InventoryData { slots = new List<InventorySlot>(inventorySlots), gold = gold };
    }

    public void LoadInventoryData(InventoryData data)
    {
        if (data == null)
            return;

        gold = data.gold;

        foreach (var slot in data.slots)
        {
            if (slot.isEquipmentSlot)
            {
                EquipItem(slot.item, slot.slotType);
            }
            else
            {
                AddItem(slot.item);
            }
        }
    }

    public void AddItem(Item item)
    {
        if (item == null)
            return;

        ItemData itemData = item.GetItemData();

        var existingSlot = inventorySlots.Find(slot => slot.item.GetItemData().ID == itemData.ID);
        if (existingSlot != null)
        {
            if (existingSlot.AddItem(item))
            {
                return;
            }
            else
            {
                var firstEmptySlot = inventorySlots.FirstOrDefault(slot => slot.item == null);
                if (firstEmptySlot != null)
                {
                    firstEmptySlot.AddItem(item);
                }
            }
        }

        if (inventorySlots.Count < MAX_SLOTS)
        {
            var firstEmptySlot = inventorySlots.FirstOrDefault(slot => slot.item == null);
            if (firstEmptySlot != null)
            {
                firstEmptySlot.AddItem(item);
            }
        }
        else
        {
            Logger.Log(
                typeof(Inventory),
                $"Inventory is full Current Size: {inventorySlots.Count} \n Max Size: {MAX_SLOTS}"
            );
        }
    }

    public InventorySlot GetEquippedItemSlot(
        ItemType itemType,
        AccessoryType accessoryType = AccessoryType.None
    )
    {
        if (itemType != ItemType.Accessory)
        {
            return equipmentSlots.Find(slot => slot.item.GetItemData().Type == itemType);
        }
        else
        {
            return equipmentSlots.Find(slot =>
                slot.item.GetItemData().Type == ItemType.Accessory
                && slot.item.GetItemData().AccessoryType == accessoryType
            );
        }
    }

    public Item GetEquippedItem(ItemType itemType, AccessoryType accessoryType = AccessoryType.None)
    {
        if (itemType != ItemType.Accessory)
        {
            var slot = equipmentSlots
                .Where(slot => slot.item.GetItemData().Type == itemType)
                .FirstOrDefault();

            if (slot != null && slot.item != null)
            {
                return slot.item;
            }
        }
        else
        {
            var slot = equipmentSlots
                .Where(slot =>
                    slot.item.GetItemData().Type == ItemType.Accessory
                    && slot.item.GetItemData().AccessoryType == accessoryType
                )
                .FirstOrDefault();

            if (slot != null && slot.item != null)
            {
                return slot.item;
            }
        }

        return null;
    }

    public void UnequipItem(SlotType slotType)
    {
        var equipSlot = equipmentSlots.Find(slot => slot.slotType == slotType);

        if (equipSlot != null && equipSlot.item != null && equipSlot.isEquipped)
        {
            equipSlot.item.OnUnequip(player);
            equipSlot.RemoveItem();
            var inventorySlot = inventorySlots.FirstOrDefault(slot => slot.item == null);
            if (inventorySlot != null)
            {
                inventorySlot.AddItem(equipSlot.item);
            }
            else
            {
                Logger.LogWarning(typeof(Inventory), "Inventory is full");
            }
        }
    }

    public void EquipItem(Item item, SlotType slotType)
    {
        if (item == null)
        {
            Logger.LogError(typeof(Inventory), "Attempted to equip Null Item");
            return;
        }

        Logger.Log(
            typeof(Inventory),
            $"Attempting to equip {item.GetItemData().Name} to slot {slotType}"
        );

        if (equipmentSlots.Find(slot => slot.slotType == slotType) != null)
        {
            UnequipItem(slotType);
        }

        if (item != null)
        {
            equipmentSlots.Find(slot => slot.slotType == slotType).AddItem(item);

            item.OnEquip(player);

            var inventorySlot = inventorySlots.Find(s => s.item == item);

            if (inventorySlot != null)
            {
                inventorySlot.RemoveItem();
            }

            var inventoryPanel = UIManager.Instance.GetPanel(PanelType.Inventory) as InventoryPanel;

            if (inventoryPanel != null)
            {
                inventoryPanel.UpdateUI();
            }

            Logger.Log(
                typeof(Inventory),
                $"Successfully equipped {item.GetItemData().Name} to slot {slotType}"
            );
        }
        else
        {
            Logger.LogError(
                typeof(Inventory),
                $"Failed to create equipment item for {item.GetItemData().Name}"
            );
        }
    }

    public void SaveInventoryState()
    {
        PlayerDataManager.Instance.SaveInventoryData(GetInventoryData());
    }

    public void ClearInventory()
    {
        foreach (var slot in equipmentSlots)
        {
            UnequipItem(slot.slotType);
        }
        foreach (var slot in inventorySlots)
        {
            RemoveItem(slot.item.GetItemData().ID);
        }
        gold = 0;
    }

    public void RemoveItem(Guid itemId)
    {
        var slot = inventorySlots.Find(s => s.item.GetItemData().ID == itemId);
        if (slot != null)
        {
            slot.item = null;
            slot.amount = 0;
        }
    }
}
