using System.Collections.Generic;
using UnityEngine;

public class AccessoryItem : EquipmentItem
{
    private AccessoryType accessoryType;

    public AccessoryItem(ItemData itemData)
        : base(itemData)
    {
        if (itemData.Type != ItemType.Accessory)
        {
            Logger.LogError(
                typeof(AccessoryItem),
                $"Attempted to create AccessoryItem with non-accessory ItemData: {itemData.Type}"
            );
        }
    }

    public override void Initialize(ItemData data)
    {
        base.Initialize(data);
        DetermineAccessoryType(data);
    }

    private void DetermineAccessoryType(ItemData data)
    {
        accessoryType = data.AccessoryType;

        if (accessoryType == AccessoryType.None)
        {
            Logger.LogWarning(
                typeof(AccessoryItem),
                $"AccessoryType not set for item: {data.Name} (ID: {data.ID})"
            );
        }
    }

    protected override void ValidateItemType(ItemType type)
    {
        if (type != ItemType.Accessory)
        {
            Logger.LogError(
                typeof(AccessoryItem),
                $"잘못된 아이템 타입입니다: {type}. AccessoryItem은 ItemType.Accessory이어야 합니다."
            );
        }
    }
}
