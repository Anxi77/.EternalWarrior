using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemSystem : MonoBehaviour
{
    private ItemGenerator itemGenerator;

    public void Initialize()
    {
        LoadingManager.Instance.SetLoadingText("Initializing Item Generator...");
        itemGenerator = new GameObject("ItemGenerator").AddComponent<ItemGenerator>();
        itemGenerator.transform.SetParent(this.transform);
    }

    public void DropItem(MonsterType monsterType, float luckMultiplier = 1f)
    {
        var items = GetDropsForEnemy(monsterType, luckMultiplier);

        var playerInfoPanel = UIManager.Instance.GetPanel(PanelType.PlayerInfo) as PlayerInfoPanel;

        foreach (var item in items)
        {
            var player = GameManager.Instance.PlayerSystem.Player;
            if (player == null)
                return;

            player.inventory.AddItem(item);

            playerInfoPanel.ShowItemUI(item.GetItemData().Name);
        }
    }

    public List<Item> GetDropsForEnemy(MonsterType enemyType, float luckMultiplier = 1f)
    {
        var dropTable = ItemDataManager.Instance.GetDropTables()[enemyType];
        if (dropTable == null)
            return new List<Item>();
        return itemGenerator.GenerateDrops(dropTable, luckMultiplier);
    }

    public Item GetItem(Guid itemId)
    {
        if (itemId == Guid.Empty)
        {
            Logger.LogError(
                typeof(ItemSystem),
                "[ItemManager] Attempted to get item with null or empty ID"
            );
            return null;
        }
        if (itemGenerator == null)
        {
            Logger.LogError(typeof(ItemSystem), "[ItemManager] ItemGenerator is not initialized");
            return null;
        }
        var item = itemGenerator.GenerateItem(itemId);
        if (item == null)
        {
            Logger.LogError(
                typeof(ItemSystem),
                $"[ItemManager] Failed to generate item with ID: {itemId} "
            );
            return null;
        }

        Logger.Log(typeof(ItemSystem), $"[ItemManager] Generated item: {item.GetItemData().Name}");
        return item;
    }

    public List<ItemData> GetItemsByType(ItemType type)
    {
        return ItemDataManager
            .Instance.GetAllData()
            .Where(item => item.Type == type)
            .Select(item => item.Clone())
            .ToList();
    }

    public List<ItemData> GetItemsByRarity(ItemRarity rarity)
    {
        return ItemDataManager
            .Instance.GetAllData()
            .Where(item => item.Rarity == rarity)
            .Select(item => item.Clone())
            .ToList();
    }

    public Item TestItem()
    {
        Guid itemId = ItemDataManager.Instance.GetAllData()[0].ID;
        return GetItem(itemId);
    }
}
