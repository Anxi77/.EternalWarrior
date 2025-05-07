using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPanel : Panel
{
    [Serializable]
    class StatInfo
    {
        public StatType statType;
        public TextMeshProUGUI valueText;
    }

    public override PanelType PanelType => PanelType.Inventory;

    #region Variables

    [SerializeField]
    private ItemSlot[] equipmentSlots;

    [SerializeField]
    private StatInfo[] statInfos;

    [SerializeField]
    private TextMeshProUGUI levelText;

    [SerializeField]
    private TextMeshProUGUI atkText;

    [SerializeField]
    private TextMeshProUGUI defText;

    [SerializeField]
    private Transform slotsParent;

    [SerializeField]
    private ItemSlot slotPrefab;

    private Inventory inventory;
    private PlayerStat playerStat;
    private List<ItemSlot> slotUIs = new();

    [SerializeField]
    private ItemTooltip itemTooltipPrefab;

    private ItemTooltip itemTooltip;

    private ItemSlot dragOriginSlot;
    private GameObject dragPreview;

    [SerializeField]
    private RectTransform layoutRoot;

    public bool IsInitialized { get; private set; }
    #endregion

    #region Initialization

    public void SetupInventory(Inventory inventory, PlayerStat playerStat)
    {
        itemTooltip = Instantiate(itemTooltipPrefab, transform);
        this.inventory = inventory;
        this.playerStat = playerStat;
        if (inventory == null)
        {
            Logger.LogError(typeof(InventoryPanel), "Inventory component not found on player!");
            return;
        }

        InitializeUI();
        IsInitialized = true;
    }

    public override void Open()
    {
        itemTooltip.transform.SetParent(UIManager.Instance.transform);
        itemTooltip.Hide();
        itemTooltip.Clear();
        UpdateStatInfos();
        base.Open();
    }

    public override void Close(bool objActive = true)
    {
        base.Close(objActive);
    }

    private void InitializeUI()
    {
        InitializeEquipmentSlots();
        InitializeInventorySlots();
        UpdateStatInfos();
    }

    private void UpdateStatInfos()
    {
        foreach (var statInfo in statInfos)
        {
            statInfo.valueText.text = playerStat.GetStat(statInfo.statType).ToString();
        }
        atkText.text = playerStat.GetStat(StatType.Damage).ToString();
        defText.text = playerStat.GetStat(StatType.Defense).ToString();
        var player = GameManager.Instance.PlayerSystem.Player;
        if (player == null)
        {
            return;
        }
        levelText.text = player.level.ToString();
    }

    private void InitializeEquipmentSlots()
    {
        if (equipmentSlots == null)
        {
            Logger.LogError(typeof(InventoryPanel), "Equipment slots array is null!");
            return;
        }

        foreach (var equipSlot in inventory.GetEquipmentSlots())
        {
            var slotUI = Array.Find(equipmentSlots, slot => slot.slotType == equipSlot.slotType);
            if (slotUI != null)
            {
                slotUI.Initialize(inventory, equipSlot, itemTooltip, this);
            }
        }
    }

    private void InitializeInventorySlots()
    {
        var slots = inventory.GetStorageSlots();
        foreach (var slot in slots)
        {
            var slotUI = Instantiate(slotPrefab, slotsParent);
            slotUI.Initialize(inventory, slot, itemTooltip, this);
            slotUIs.Add(slotUI);
        }
    }

    #endregion

    #region UI Updates
    public void UpdateUI()
    {
        if (!IsInitialized || inventory == null)
        {
            Logger.LogWarning(
                typeof(InventoryPanel),
                "Cannot update UI: Inventory not initialized"
            );
            return;
        }

        UpdateInventorySlots();
        UpdateEquipmentSlots();
        UpdateStatInfos();
    }

    private void UpdateInventorySlots()
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            slotUIs[i].UpdateUI();
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
                    equipSlot.UpdateUI();
                }
            }
        }
    }

    private void UpdateEquipmentSlot(ItemSlot equipSlot)
    {
        if (inventory == null)
        {
            Logger.LogWarning(typeof(InventoryPanel), "Inventory is null");
            return;
        }

        var equipmentSlot = GetEquipmentSlotFromSlotType(equipSlot.slotType);
        if (equipmentSlot == SlotType.Storage)
        {
            Logger.LogWarning(typeof(InventoryPanel), $"Invalid slot type: {equipSlot.slotType}");
            return;
        }

        InventorySlot equippedSlot = inventory.GetEquippedItemSlot(equipSlot.slotType);

        if (equippedSlot != null)
        {
            equipSlot.UpdateUI();
        }
        else
        {
            equipSlot.UpdateUI();
        }
    }

    public void BeginItemDrag(ItemSlot slot, Sprite icon, Vector2 size, Vector3 position)
    {
        dragOriginSlot = slot;
        dragPreview = new GameObject("DragPreview");
        dragPreview.transform.SetParent(transform.root);
        var image = dragPreview.AddComponent<Image>();
        var cg = dragPreview.AddComponent<CanvasGroup>();
        image.sprite = icon;
        image.raycastTarget = false;
        image.preserveAspect = true;
        image.color = new Color(1, 1, 1, 0.8f);
        cg.blocksRaycasts = false;
        dragPreview.GetComponent<RectTransform>().sizeDelta = size;
        dragPreview.transform.position = position;
    }

    public void UpdateItemDrag(Vector3 position)
    {
        if (dragPreview != null)
            dragPreview.transform.position = position;
    }

    public void EndItemDrag(ItemSlot targetSlot)
    {
        if (dragPreview != null)
            Destroy(dragPreview);

        if (dragOriginSlot == targetSlot)
            return;

        if (dragOriginSlot != null && targetSlot != null && dragOriginSlot != targetSlot)
        {
            bool targetIsEquip = targetSlot.GetSlotData().isEquipmentSlot;
            bool originIsEquip = dragOriginSlot.GetSlotData().isEquipmentSlot;

            if (originIsEquip && !targetIsEquip)
            {
                dragOriginSlot.UnequipItem();
            }
            else if (!originIsEquip && targetIsEquip)
            {
                dragOriginSlot.EquipItemTo(targetSlot);
            }
            else if (!originIsEquip && !targetIsEquip)
            {
                SwapInventoryItems(dragOriginSlot, targetSlot);
            }
            else if (originIsEquip && targetIsEquip)
            {
                SwapEquipItems(dragOriginSlot, targetSlot);
            }
        }
        dragOriginSlot = null;
    }

    private void SwapInventoryItems(ItemSlot slotA, ItemSlot slotB)
    {
        var temp = slotA.GetSlotData();
        slotA.SetSlotData(slotB.GetSlotData());
        slotB.SetSlotData(temp);
        slotA.UpdateUI();
        slotB.UpdateUI();
    }

    private void SwapEquipItems(ItemSlot slotA, ItemSlot slotB)
    {
        var itemA = slotA.GetItem();
        var itemB = slotB.GetItem();
        if (itemA != null)
            slotA.UnequipItem();
        if (itemB != null)
            slotB.UnequipItem();
        if (itemA != null)
            slotB.EquipItem(itemA);
        if (itemB != null)
            slotA.EquipItem(itemB);
    }
    #endregion

    #region Utilities
    private SlotType GetEquipmentSlotFromSlotType(SlotType slotType)
    {
        return slotType switch
        {
            SlotType.Weapon => SlotType.Weapon,
            SlotType.Armor => SlotType.Armor,
            SlotType.Ring => SlotType.Ring,
            SlotType.Necklace => SlotType.Necklace,
            _ => SlotType.Storage,
        };
    }

    #endregion
}
