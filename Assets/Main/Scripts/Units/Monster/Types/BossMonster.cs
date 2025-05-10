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

    protected override void PerformMeleeAttack()
    {
        StartCoroutine(MeleeAttackCoroutine());
    }

    private IEnumerator MeleeAttackCoroutine()
    {
        monsterAnimator.Attack();
        yield return new WaitForSeconds(attackPrepareTime);

        Logger.Log(typeof(BossMonster), "Melee Attack");

        if (attackParticle != null)
        {
            var particle = Instantiate(
                attackParticle,
                Target.transform.position,
                Quaternion.identity
            );
            particle.Play();
            Destroy(particle.gameObject, 0.3f);
        }
        Target.GetComponent<Player>()?.TakeDamage(stat.GetStat(StatType.Damage));

        yield return new WaitUntil(() => !isAttacking());
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
