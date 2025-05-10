using System.Collections;
using UnityEngine;

public class RangedMonster : Monster
{
    [SerializeField]
    public EnemyProjectile projectilePrefab;

    public override void Initialize(MonsterData monsterData, MonsterSetting monsterSetting)
    {
        base.Initialize(monsterData, monsterSetting);
    }

    protected override void PerformRangedAttack()
    {
        StartCoroutine(RangedAttackCoroutine());
    }

    private IEnumerator RangedAttackCoroutine()
    {
        monsterAnimator.Attack();

        Vector2 direction = ((Vector2)Target.position - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        EnemyProjectile projectile = PoolManager.Instance.Spawn<EnemyProjectile>(
            projectilePrefab.gameObject,
            transform.position,
            Quaternion.Euler(0, 0, angle - 90)
        );

        if (projectile != null)
        {
            projectile.Initialize(
                stat.GetStat(StatType.Damage),
                10f,
                false,
                1f,
                1,
                stat.GetStat(StatType.AttackRange),
                ElementType.None,
                0f,
                Target
            );
            projectile.SetDirection(direction);
            projectile.gameObject.tag = "EnemyProjectile";
        }

        var clips = animator.GetCurrentAnimatorClipInfo(0);

        yield return new WaitUntil(() => !isAttacking());
    }
}
