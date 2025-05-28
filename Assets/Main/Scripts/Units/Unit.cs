using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

public abstract class Unit : MonoBehaviour
{
    public StatSystem stat;
    public List<Skill> skills;
    protected UnitStatus unitStatus = UnitStatus.Alive;
    public UnitStatus UnitStatus => unitStatus;
    public Action<ScaledInt, ScaledInt> OnHpChanged;
    public bool isStunned = false;

    public void ActivateHoming(bool activate)
    {
        foreach (var skill in skills)
        {
            if (skill is ProjectileSkills ProjectileSkills)
            {
                ProjectileSkills.UpdateHomingState(activate);
            }
        }
    }

    public void StartHealthRegeneration()
    {
        if (stat == null)
        {
            Debug.LogError("StatSystem is not initialized for this unit.");
            return;
        }

        if (unitStatus == UnitStatus.Alive)
        {
            HealthRegen().Forget();
        }
    }

    protected async UniTask HealthRegen()
    {
        while (true)
        {
            if (unitStatus != UnitStatus.Alive)
            {
                break;
            }
            if (stat == null || !gameObject.activeInHierarchy)
            {
                break;
            }
            ScaledInt regenAmount = stat.GetStat(StatType.HpRegenRate);
            if (regenAmount > 0)
            {
                TakeHeal(regenAmount, this);
            }

            await UniTask.Delay(1000);
        }
    }

    public abstract void Die();

    public virtual void TakeHeal(ScaledInt value, object source)
    {
        ScaledInt currentHp = stat.GetStat(StatType.CurrentHp);
        ScaledInt maxHp = stat.GetStat(StatType.MaxHp);

        if (currentHp + value > maxHp)
        {
            return;
        }

        stat.AddStat(StatType.CurrentHp, CalcType.Plus, value, source);
        OnHpChanged?.Invoke(currentHp, maxHp);
    }

    public virtual void TakeDamage(ScaledInt value, object source)
    {
        if (unitStatus != UnitStatus.Alive)
            return;

        ScaledInt defense = stat.GetStat(StatType.Defense);

        ScaledInt damageReduction = defense / (defense + 100f);
        ScaledInt finalDamage = value * (1f - damageReduction);

        stat.AddStat(StatType.CurrentHp, CalcType.Minus, value, source);

        OnHpChanged?.Invoke(stat.GetStat(StatType.CurrentHp), stat.GetStat(StatType.MaxHp));

        if (stat.GetStat(StatType.CurrentHp) <= 0)
        {
            Die();
        }
    }

    public virtual void ApplyDebuff(
        ScaledInt amount,
        StatType statType,
        float duration,
        object source
    )
    {
        if (unitStatus == UnitStatus.Dead)
            return;

        if (statType != StatType.Defense)
        {
            ScaledInt currvalue = stat.GetStat(statType);
            if (currvalue - amount < 0)
            {
                return;
            }
        }
        Debuff(amount, statType, duration, source).Forget();
    }

    public virtual async UniTask Debuff(
        ScaledInt amount,
        StatType statType,
        float duration,
        object source
    )
    {
        if (this != null && gameObject.activeInHierarchy)
        {
            if (UnitStatus != UnitStatus.Dead)
            {
                stat.AddStat(statType, CalcType.Minus, amount, source);
            }
        }
        await UniTask.Delay(TimeSpan.FromSeconds(duration));
        if (this != null && gameObject.activeInHierarchy)
        {
            if (UnitStatus != UnitStatus.Dead)
            {
                stat.RemoveStat(statType, CalcType.Minus, amount, source);
            }
        }
    }

    public virtual void ApplyDotDamage(
        ScaledInt damage,
        float duration,
        float tickRate,
        object source
    )
    {
        if (unitStatus == UnitStatus.Dead)
            return;

        DotDamage(damage, duration, source).Forget();
    }

    public virtual async UniTask DotDamage(ScaledInt damage, float duration, object source)
    {
        if (this != null && gameObject.activeInHierarchy)
        {
            if (UnitStatus != UnitStatus.Dead)
            {
                TakeDamage(damage, source);
            }
        }
        await UniTask.Delay(TimeSpan.FromSeconds(duration));
        if (this != null && gameObject.activeInHierarchy)
        {
            if (UnitStatus != UnitStatus.Dead)
            {
                TakeHeal(damage, source);
            }
        }
    }

    public virtual void ApplyStun(float duration)
    {
        if (unitStatus == UnitStatus.Dead || isStunned)
            return;

        Stun(duration).Forget();
    }

    private async UniTask Stun(float duration)
    {
        isStunned = true;
        await UniTask.Delay(TimeSpan.FromSeconds(duration));
        isStunned = false;
    }
}
