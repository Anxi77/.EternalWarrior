using System;
using System.Collections;
using Michsky.UI.Heat;
using UnityEngine;
using MonsterAnimator = Assets.FantasyMonsters.Common.Scripts.Monster;
using Random = UnityEngine.Random;

public class Monster : Unit
{
    [SerializeField]
    protected MonsterAnimator monsterAnimator;
    protected MonsterData monsterData;
    protected MonsterSetting monsterSetting;
    private Transform target;
    public Transform Target => target;
    public ProgressBar hpBar;
    public Rigidbody2D rb;
    public ParticleSystem attackParticle;
    public Collider2D enemyCollider;
    public PathFinder pathFinder;
    public MonsterAnimator MonsterAnimator => monsterAnimator;
    protected float lastAttackTime;
    public float preferredDistance = 1.0f;
    public bool isInit = false;
    public Animator animator;

    [SerializeField]
    protected float attackPrepareTime = 0.2f;

    protected Coroutine slowEffectCoroutine;
    protected Coroutine stunCoroutine;
    protected Coroutine dotDamageCoroutine;
    protected Coroutine defenseDebuffCoroutine;

    private bool isQuitting = false;

    #region Unity Lifecycle

    public virtual void Initialize(MonsterData monsterData, MonsterSetting monsterSetting)
    {
        this.monsterData = monsterData;
        this.monsterSetting = monsterSetting;
        stat.Initialize(monsterData.statData);
        enemyCollider = GetComponent<Collider2D>();
        InitializeComponents();
        if (GameManager.Instance?.PlayerSystem?.Player != null)
        {
            target = GameManager.Instance.PlayerSystem.Player.transform;
            isInit = true;
        }
        if (
            Application.isPlaying
            && GameManager.Instance != null
            && !GameManager.Instance.Monsters.Contains(this)
        )
        {
            GameManager.Instance.Monsters.Add(this);
        }

        hpBar.maxValue = stat.GetStat(StatType.MaxHp);
        hpBar.SetValue(stat.GetStat(StatType.CurrentHp));
    }

    public bool isAttacking()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("Attack");
    }

    protected virtual void Update()
    {
        if (isInit)
        {
            UpdateVisuals();

            float distanceToPlayer = Vector2.Distance(transform.position, Target.position);

            if (distanceToPlayer <= stat.GetStat(StatType.AttackRange))
            {
                Attack();
            }
        }
    }

    private void FixedUpdate()
    {
        if (isInit && !isAttacking())
        {
            pathFinder.Move();
        }
    }

    protected virtual void OnDisable()
    {
        if (slowEffectCoroutine != null)
        {
            StopCoroutine(slowEffectCoroutine);
            slowEffectCoroutine = null;
        }

        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
            stunCoroutine = null;
        }

        if (dotDamageCoroutine != null)
        {
            StopCoroutine(dotDamageCoroutine);
            dotDamageCoroutine = null;
        }

        if (defenseDebuffCoroutine != null)
        {
            StopCoroutine(defenseDebuffCoroutine);
            defenseDebuffCoroutine = null;
        }
        stat.RemoveAllModifiers();
        isStunned = false;

        if (
            Application.isPlaying
            && !isQuitting
            && GameManager.Instance != null
            && GameManager.Instance.Monsters != null
            && GameManager.Instance.Monsters.Contains(this)
        )
        {
            GameManager.Instance.Monsters.Remove(this);
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }
    #endregion

    protected virtual void InitializeComponents()
    {
        pathFinder.Initialize(this);
    }

    #region Combat

    public override void Die()
    {
        if (monsterSetting.expParticlePrefab != null)
        {
            int expParticleCount = Random.Range(
                monsterSetting.expParticleRange.x,
                monsterSetting.expParticleRange.y + 1
            );

            var dropExp = stat.GetStat(StatType.DropExp);
            float expPerParticle = dropExp / expParticleCount;

            for (int i = 0; i < expParticleCount; i++)
            {
                Vector3 spawnPosition = transform.position;
                ExpParticle expParticle = PoolManager.Instance.Spawn<ExpParticle>(
                    monsterSetting.expParticlePrefab.gameObject,
                    spawnPosition,
                    Quaternion.identity
                );

                if (expParticle != null)
                {
                    expParticle.expValue = expPerParticle;
                }
            }
            DropItems();
        }

        if (GameManager.Instance?.Monsters != null)
        {
            GameManager.Instance.Monsters.Remove(this);
        }

        PoolManager.Instance.Despawn(this);
    }

    protected virtual void DropItems()
    {
        float playerLuck = GameManager
            .Instance.PlayerSystem.Player.GetComponent<StatSystem>()
            .GetStat(StatType.Luck);

        GameManager.Instance.ItemSystem.DropItem(monsterData.type, 1f + playerLuck);
    }

    protected virtual void Attack()
    {
        var attackSpeed = stat.GetStat(StatType.AttackSpeed);
        var damageInterval = 1f / attackSpeed;
        if (Time.time >= lastAttackTime + damageInterval)
        {
            float distanceToTarget = Vector2.Distance(transform.position, Target.position);

            if (distanceToTarget <= stat.GetStat(StatType.AttackRange))
            {
                switch (monsterData.type)
                {
                    case MonsterType.Ogre:
                    case MonsterType.Bat:
                        PerformMeleeAttack();
                        break;
                    case MonsterType.Wasp:
                        PerformRangedAttack();
                        break;
                    default:
                        PerformMeleeAttack();
                        break;
                }
                lastAttackTime = Time.time;
            }
        }
    }

    protected virtual void PerformMeleeAttack() { }

    protected virtual void PerformRangedAttack() { }
    #endregion

    #region Collision
    public virtual void Contact()
    {
        var particle = Instantiate(attackParticle, Target.position, Quaternion.identity);
        particle.Play();
        Destroy(particle.gameObject, 0.3f);
        Attack();
    }
    #endregion

    #region UI
    protected virtual void UpdateHPBar()
    {
        if (hpBar != null)
        {
            hpBar.SetValue(stat.GetStat(StatType.CurrentHp));
        }
    }

    protected virtual void UpdateVisuals()
    {
        UpdateHPBar();
        UpdateSpriteDirection();
    }
    #endregion

    #region Utility
    public virtual void SetCollisionState(bool isOutOfView)
    {
        if (enemyCollider != null)
        {
            enemyCollider.enabled = !isOutOfView;
        }
    }

    public virtual void UpdateSpriteDirection()
    {
        if (Target != null)
        {
            Vector3 scale = transform.localScale;
            scale.x = (Target.position.x > transform.position.x) ? -1 : 1;
            transform.localScale = scale;
        }
    }
    #endregion
}
