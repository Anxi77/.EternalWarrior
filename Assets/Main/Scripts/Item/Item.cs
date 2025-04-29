using System.Linq;
using UnityEngine;

public abstract class Item
{
    protected ItemData itemData;

    public virtual ItemData GetItemData() => itemData;

    public virtual void Initialize(ItemData data)
    {
        itemData = data;
    }

    public virtual void OnEquip(Player player)
    {
        var playerStat = player.GetComponent<PlayerStat>();
        foreach (var stat in itemData.Stats)
        {
            playerStat.AddModifier(stat);
        }
    }

    public virtual void OnUnequip(Player player)
    {
        var playerStat = player.GetComponent<PlayerStat>();
        foreach (var stat in itemData.Stats)
        {
            playerStat.RemoveModifier(stat);
        }
    }
}
