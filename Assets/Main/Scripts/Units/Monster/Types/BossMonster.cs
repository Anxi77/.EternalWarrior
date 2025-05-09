using System.Collections;
using Assets.FantasyMonsters.Common.Scripts;
using UnityEngine;

public class BossMonster : Monster
{
    [Header("Boss Specific Stats")]
    public float enrageThreshold = 0.3f;
    public float enrageDamageMultiplier = 1.5f;
    public float enrageSpeedMultiplier = 1.3f;
    private bool isEnraged = false;

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);

        if (
            !isEnraged
            && stat.GetStat(StatType.CurrentHp) <= stat.GetStat(StatType.MaxHp) * enrageThreshold
        )
        {
            EnterEnragedState();
        }
    }

    private void EnterEnragedState()
    {
        isEnraged = true;
        var damagemodifier = new StatModifier(
            StatType.Damage,
            this,
            CalcType.Multiply,
            enrageDamageMultiplier
        );
        stat.AddModifier(damagemodifier);
        var moveSpeedModifier = new StatModifier(
            StatType.MoveSpeed,
            this,
            CalcType.Multiply,
            enrageSpeedMultiplier
        );
        stat.AddModifier(moveSpeedModifier);

        PlayEnrageEffect();
    }

    private void PlayEnrageEffect() { }

    public override void Die()
    {
        GameManager.Instance.MonsterSystem.OnBossDefeated(transform.position);
        base.Die();
    }

    //private IEnumerator SpecialAttackPattern()
    //{
    //    while (true)
    //    {
    //        // �⺻ ����
    //        yield return new WaitForSeconds(3f);

    //        // ���� ����
    //        if (hp < maxHp * 0.7f)
    //        {
    //            AreaAttack();
    //            yield return new WaitForSeconds(5f);
    //        }

    //        // ��ȯ ����
    //        if (hp < maxHp * 0.5f)
    //        {
    //            SummonMinions();
    //            yield return new WaitForSeconds(10f);
    //        }
    //    }
    //}

    //private void AreaAttack()
    //{
    //    // ���� ���� ����
    //}

    //private void SummonMinions()
    //{
    //    // �ϼ��� ��ȯ ����
    //}
}
