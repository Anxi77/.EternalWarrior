using UnityEngine;

public abstract class SkillIneractionEffect : ISkillInteractionEffect
{
    protected ItemEffectData effectData;
    protected float procChance;
    protected float cooldown;
    protected float lastProcTime;

    public SkillIneractionEffect(
        ItemEffectData effectData,
        float procChance = 1f,
        float cooldown = 0f
    )
    {
        this.effectData = effectData;
        this.procChance = procChance;
        this.cooldown = cooldown;
        this.lastProcTime = 0f;
    }

    protected bool CanTriggerEffect()
    {
        if (Time.time < lastProcTime + cooldown)
            return false;
        if (Random.value > procChance)
            return false;

        lastProcTime = Time.time;
        return true;
    }

    public virtual void OnSkillCast(Skill skill) { }

    public virtual void OnSkillHit(Skill skill, Monster target) { }

    public virtual void OnSkillKill(Skill skill, Player player, Monster target) { }

    public virtual void ModifySkillStats(Skill skill) { }
}
