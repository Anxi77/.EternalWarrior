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
        var playerStat = player.GetComponent<StatSystem>();
        foreach (var stat in itemData.Stats)
        {
            playerStat.AddStat(stat.Type, stat.CalcType, stat.Value, this);
        }
    }

    public virtual void OnUnequip(Player player)
    {
        var playerStat = player.GetComponent<StatSystem>();
        foreach (var stat in itemData.Stats)
        {
            playerStat.RemoveStat(stat.Type, stat.CalcType, stat.Value, this);
        }
    }
}
