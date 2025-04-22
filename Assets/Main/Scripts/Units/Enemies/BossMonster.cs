using System.Collections;
using Assets.FantasyMonsters.Common.Scripts;
using UnityEngine;

public class BossMonster : Monster
{
    [Header("Boss Specific Stats")]
    public float enrageThreshold = 0.3f;
    public float enrageDamageMultiplier = 1.5f;
    public float enrageSpeedMultiplier = 1.3f;

    public Assets.FantasyMonsters.Common.Scripts.Monster monster;

    private bool isEnraged = false;
    private Vector3 startPosition;
    private Animator animator;

    protected override void Start()
    {
        base.Start();
        startPosition = transform.position;
        animator = GetComponentInChildren<Animator>();
        InitializeBossStats();
    }

    private void InitializeBossStats()
    {
        hp *= 5f;
        damage *= 2f;
        moveSpeed *= 0.8f;
        baseDefense *= 2f;
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);

        if (!isEnraged && hp <= maxHp * enrageThreshold)
        {
            EnterEnragedState();
        }
    }

    public override void Move()
    {
        base.Move();
        monster.SetState(MonsterState.Run);
    }

    private void EnterEnragedState()
    {
        isEnraged = true;
        damage *= enrageDamageMultiplier;
        moveSpeed *= enrageSpeedMultiplier;

        // �ݳ� ����Ʈ ���
        PlayEnrageEffect();
    }

    private void PlayEnrageEffect() { }

    public override void Die()
    {
        MonsterManager.Instance.OnBossDefeated(transform.position);
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
