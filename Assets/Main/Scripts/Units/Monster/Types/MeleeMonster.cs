using System.Collections;
using UnityEngine;

public class MeleeMonster : Monster
{
    public override void Initialize(MonsterData monsterData, MonsterSetting monsterSetting)
    {
        base.Initialize(monsterData, monsterSetting);
        preferredDistance = 1.5f;
    }

    protected override void PerformMeleeAttack()
    {
        StartCoroutine(MeleeAttackCoroutine());
    }

    private IEnumerator MeleeAttackCoroutine()
    {
        monsterAnimator.Attack();
        yield return new WaitForSeconds(attackPrepareTime);

        Logger.Log(typeof(MeleeMonster), "Melee Attack");

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            stat.GetStat(StatType.AttackRange)
        );
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                if (attackParticle != null)
                {
                    var particle = Instantiate(
                        attackParticle,
                        hit.transform.position,
                        Quaternion.identity
                    );
                    particle.Play();
                    Destroy(particle.gameObject, 0.3f);
                }
                hit.GetComponent<Player>()?.TakeDamage(stat.GetStat(StatType.Damage));
            }
        }

        yield return new WaitUntil(() => !isAttacking());
    }
}
