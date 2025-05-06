using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemSystem : MonoBehaviour, IInitializable
{
    [SerializeField]
    private WorldDropItem worldDropItemPrefab;
    private ItemGenerator itemGenerator;
    private bool isInitialized;
    public bool IsInitialized => isInitialized;

    public void Initialize()
    {
        if (!IsInitialized)
        {
            try
            {
                itemGenerator = new GameObject("ItemGenerator").AddComponent<ItemGenerator>();
                isInitialized = true;
            }
            catch (Exception e)
            {
                Logger.LogError(
                    typeof(ItemSystem),
                    $"[ItemManager] Error initializing ItemManager: {e.Message}\n{e.StackTrace}"
                );
                isInitialized = false;
            }
        }
    }

    public void DropItem(Vector3 position, MonsterType monsterType, float luckMultiplier = 1f)
    {
        if (worldDropItemPrefab == null)
            return;

        var items = GetDropsForEnemy(monsterType, luckMultiplier);

        foreach (var item in items)
        {
            var worldDropItem = PoolManager.Instance.Spawn<WorldDropItem>(
                worldDropItemPrefab.gameObject,
                position,
                Quaternion.identity
            );
            if (worldDropItem != null)
            {
                worldDropItem.Initialize(item);

                if (worldDropItem.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    Vector2 smallRandomOffset = Random.insideUnitCircle * 0.3f;
                    rb.AddForce(smallRandomOffset, ForceMode2D.Impulse);
                }
            }
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
