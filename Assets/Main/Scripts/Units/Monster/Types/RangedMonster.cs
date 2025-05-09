using System.Collections;
using UnityEngine;

public class RangedMonster : Monster
{
    [SerializeField]
    public EnemyProjectile projectilePrefab;

    [SerializeField]
    private float attackAnimationDuration = 0.5f;

    private Animator animator;

    public override void Initialize(MonsterData monsterData, MonsterSetting monsterSetting)
    {
        base.Initialize(monsterData, monsterSetting);

        animator = GetComponentInChildren<Animator>();
    }

    protected override void PerformRangedAttack()
    {
        if (!isAttacking)
        {
            StartCoroutine(RangedAttackCoroutine());
        }
    }

    private IEnumerator RangedAttackCoroutine()
    {
        isAttacking = true;

        animator?.SetTrigger("Attack");

        Vector2 direction = ((Vector2)Target.position - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        EnemyProjectile projectile = PoolManager.Instance.Spawn<EnemyProjectile>(
            projectilePrefab.gameObject,
            transform.position,
            Quaternion.Euler(0, 0, angle - 90)
        );

        if (projectile != null)
        {
            projectile.damage = stat.GetStat(StatType.Damage);
            projectile.moveSpeed = 10f;
            projectile.maxTravelDistance = stat.GetStat(StatType.AttackRange);
            projectile.SetDirection(direction);
            projectile.gameObject.tag = "EnemyProjectile";

            if (attackParticle != null)
            {
                var particle = Instantiate(attackParticle, transform.position, Quaternion.identity);
                particle.Play();
                Destroy(particle.gameObject, 0.3f);
            }
        }

        yield return new WaitForSeconds(attackAnimationDuration);
        isAttacking = false;
    }
}
