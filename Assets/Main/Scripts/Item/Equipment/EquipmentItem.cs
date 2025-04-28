using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class EquipmentItem : Item, ISkillInteractionEffect
{
    protected SlotType equipmentSlot;
    protected List<ISkillInteractionEffect> skillEffects = new();

    protected EquipmentItem(ItemData itemData)
    {
        this.itemData = itemData;
    }

    public override void Initialize(ItemData data)
    {
        base.Initialize(data);
        ValidateItemType(data.Type);
        InitializeSkillEffects(data);
    }

    protected virtual void InitializeSkillEffects(ItemData data)
    {
        skillEffects = SkillEffectFactory.CreateEffects(data);
    }

    protected void OnEquip(Player player)
    {
        foreach (var skill in player.skills)
        {
            foreach (var effect in skillEffects)
            {
                skill.ApplyItemEffect(effect);
            }
        }
    }

    protected void OnUnequip(Player player)
    {
        foreach (var skill in player.skills)
        {
            foreach (var effect in skillEffects)
            {
                skill.RemoveItemEffect(effect);
            }
        }
    }

    public virtual void OnSkillCast(Skill skill)
    {
        foreach (var effect in skillEffects)
        {
            effect.OnSkillCast(skill);
        }
    }

    public virtual void OnSkillHit(Skill skill, Monster target)
    {
        foreach (var effect in skillEffects)
        {
            effect.OnSkillHit(skill, target);
        }
    }

    public void AddEffect(ISkillInteractionEffect effect)
    {
        if (effect != null)
        {
            skillEffects.Add(effect);
        }
    }

    public void OnSkillKill(Skill skill, Player player, Monster target)
    {
        foreach (var effect in skillEffects)
        {
            effect.OnSkillKill(skill, player, target);
        }
    }

    public void ModifySkillStats(Skill skill)
    {
        foreach (var effect in skillEffects)
        {
            effect.ModifySkillStats(skill);
        }
    }

    protected abstract void ValidateItemType(ItemType type);
}
