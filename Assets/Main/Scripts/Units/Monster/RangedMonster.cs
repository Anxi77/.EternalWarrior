using System.Collections;
using UnityEngine;

public class RangedMonster : Monster
{
    [Header("Ranged Attack Settings")]
    [SerializeField]
    private EnemyProjectile projectilePrefab;

    [SerializeField]
    private float minAttackDistance = 5f;

    [SerializeField]
    private float maxAttackDistance = 15f;

    [SerializeField]
    private float attackAnimationDuration = 0.5f;

    private Animator animator;

    protected override void Initialize()
    {
        base.Initialize();
        attackRange = maxAttackDistance;
        preferredDistance = minAttackDistance;
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

        Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        EnemyProjectile projectile = PoolManager.Instance.Spawn<EnemyProjectile>(
            projectilePrefab.gameObject,
            transform.position,
            Quaternion.Euler(0, 0, angle - 90)
        );

        if (projectile != null)
        {
            projectile.damage = damage;
            projectile.moveSpeed = 10f;
            projectile.maxTravelDistance = maxAttackDistance;
            projectile.SetDirection(direction);
            projectile.gameObject.tag = "EnemyProjectile";

            if (attackParticle != null)
            {
                var particle = Instantiate(attackParticle, transform.position, Quaternion.identity);
                particle.Play();
                Destroy(particle.gameObject, 0.3f);
            }
        }

        preDamageTime = Time.time;
        yield return new WaitForSeconds(attackAnimationDuration);
        isAttacking = false;
    }
}
