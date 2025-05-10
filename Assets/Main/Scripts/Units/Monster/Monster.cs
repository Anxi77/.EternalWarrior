using System;
using System.Collections;
using Michsky.UI.Heat;
using UnityEngine;
using MonsterAnimator = Assets.FantasyMonsters.Common.Scripts.Monster;
using Random = UnityEngine.Random;

public class Monster : MonoBehaviour
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
    public StatSystem stat;
    protected float lastAttackTime;
    public float preferredDistance = 1.0f;
    public bool isStunned = false;
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
        pathFinder.Initialize(this, rb);
    }

    #region Combat
    public virtual void TakeDamage(float damage)
    {
        if (!gameObject.activeInHierarchy)
            return;

        var defense = stat.GetStat(StatType.Defense);

        float damageReduction = defense / (defense + 100f);
        float finalDamage = damage * (1f - damageReduction);

        stat.SetCurrentHp(stat.GetStat(StatType.CurrentHp) - finalDamage);

        if (stat.GetStat(StatType.CurrentHp) <= 0)
        {
            if (dotDamageCoroutine != null)
            {
                StopCoroutine(dotDamageCoroutine);
                dotDamageCoroutine = null;
            }
            Die();
        }
    }

    public virtual void Die()
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

    public virtual void ApplyDefenseDebuff(float amount, float duration)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (defenseDebuffCoroutine != null)
        {
            StopCoroutine(defenseDebuffCoroutine);
        }

        defenseDebuffCoroutine = StartCoroutine(DefenseDebuffCoroutine(amount, duration));
    }

    public virtual IEnumerator DefenseDebuffCoroutine(float amount, float duration)
    {
        float actualReduction = Mathf.Min(amount, stat.GetStat(StatType.MaxDefenseReduction));
        actualReduction = -actualReduction;

        var defenseDebuff = new StatModifier(
            StatType.Defense,
            this,
            CalcType.Flat,
            actualReduction
        );

        if (this != null && gameObject.activeInHierarchy)
        {
            stat.AddModifier(defenseDebuff);
        }

        yield return new WaitForSeconds(duration);

        defenseDebuffCoroutine = null;

        if (this != null && gameObject.activeInHierarchy)
        {
            stat.RemoveModifier(defenseDebuff);
        }
    }

    public virtual void ApplySlowEffect(float amount, float duration)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (slowEffectCoroutine != null)
        {
            StopCoroutine(slowEffectCoroutine);
        }

        slowEffectCoroutine = StartCoroutine(SlowEffectCoroutine(amount, duration));
    }

    protected virtual IEnumerator SlowEffectCoroutine(float amount, float duration)
    {
        float movespeedReduction;

        var moveSpeed = stat.GetStat(StatType.MoveSpeed);
        if (moveSpeed - amount > 0)
        {
            movespeedReduction = -amount;
        }
        else
        {
            movespeedReduction = -moveSpeed;
        }

        var moveSpeedDebuff = new StatModifier(
            StatType.MoveSpeed,
            this,
            CalcType.Flat,
            movespeedReduction
        );

        if (this != null && gameObject.activeInHierarchy)
        {
            stat.AddModifier(moveSpeedDebuff);
        }

        yield return new WaitForSeconds(duration);

        if (this != null && gameObject.activeInHierarchy)
        {
            stat.RemoveModifier(moveSpeedDebuff);
        }

        slowEffectCoroutine = null;
    }

    public virtual void ApplyDotDamage(float damagePerTick, float tickInterval, float duration)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (dotDamageCoroutine != null)
        {
            StopCoroutine(dotDamageCoroutine);
        }

        dotDamageCoroutine = StartCoroutine(
            DotDamageCoroutine(damagePerTick, tickInterval, duration)
        );
    }

    protected virtual IEnumerator DotDamageCoroutine(
        float damagePerTick,
        float tickInterval,
        float duration
    )
    {
        float endTime = Time.time + duration;

        while (
            Time.time < endTime
            && stat.GetStat(StatType.CurrentHp) > 0
            && gameObject.activeInHierarchy
        )
        {
            if (this != null && gameObject.activeInHierarchy)
            {
                TakeDamage(damagePerTick);
            }
            yield return new WaitForSeconds(tickInterval);
        }

        dotDamageCoroutine = null;
    }

    public virtual void ApplyStun(float power, float duration)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
        }

        stunCoroutine = StartCoroutine(StunCoroutine(duration));
    }

    protected virtual IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;
        var moveSpeed = stat.GetStat(StatType.MoveSpeed);
        var moveSpeedDebuff = new StatModifier(StatType.MoveSpeed, this, CalcType.Flat, -moveSpeed);

        yield return new WaitForSeconds(duration);

        if (this != null && gameObject.activeInHierarchy)
        {
            isStunned = false;
            stat.RemoveModifier(moveSpeedDebuff);
        }
        stunCoroutine = null;
    }
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
