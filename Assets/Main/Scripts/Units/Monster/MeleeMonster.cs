using System.Collections;
using UnityEngine;

public class MeleeMonster : Monster
{
    [Header("Melee Attack Settings")]
    [SerializeField]
    private float attackAnimationDuration = 0.5f;

    [SerializeField]
    private float attackPrepareTime = 0.2f;

    [SerializeField]
    private float attackRadius = 1.5f;

    [SerializeField]
    private LayerMask attackLayer;

    private Animator animator;

    public override void Initialize()
    {
        base.Initialize();
        attackRange = 2f;
        preferredDistance = 1.5f;
        animator = GetComponentInChildren<Animator>();
    }

    protected override void PerformMeleeAttack()
    {
        if (!isAttacking)
        {
            StartCoroutine(MeleeAttackCoroutine());
        }
    }

    private IEnumerator MeleeAttackCoroutine()
    {
        isAttacking = true;

        animator?.SetTrigger("Attack");
        yield return new WaitForSeconds(attackPrepareTime);

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            attackRadius,
            attackLayer
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
                hit.GetComponent<Player>()?.TakeDamage(damage);
            }
        }

        preDamageTime = Time.time;
        yield return new WaitForSeconds(attackAnimationDuration - attackPrepareTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}
