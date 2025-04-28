using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Monster : MonoBehaviour
{
    #region Variables
    #region Stats
    public float maxHp;
    public float hp = 10f;
    public float damage = 5f;
    public float moveSpeed = 3f;
    public float mobEXP = 10f;
    public float damageInterval;
    public float originalMoveSpeed;

    public float hpAmount
    {
        get { return hp / maxHp; }
    }

    public float preDamageTime = 0;
    public float attackRange = 1.2f;
    public float preferredDistance = 1.0f;

    public ElementType elementType = ElementType.None;

    [Header("Defense Stats")]
    public float baseDefense = 5f;
    public float currentDefense;
    public float maxDefenseReduction = 0.9f;
    public float defenseDebuffAmount = 0f;
    public float moveSpeedDebuffAmount = 0f;
    public bool isStunned = false;

    [Header("Drop Settings")]
    [SerializeField]
    public ExpParticle expParticlePrefab;

    [SerializeField]
    public int minExpParticles = 3;

    [SerializeField]
    public int maxExpParticles = 6;

    [SerializeField]
    public float dropRadiusMin = 0.5f;

    [SerializeField]
    public float dropRadiusMax = 1.5f;

    [SerializeField]
    public MonsterType monsterType;
    #endregion

    #region References
    public Transform target;
    public Image hpBar;
    public Rigidbody2D rb;
    public ParticleSystem attackParticle;
    public bool isInit = false;
    public Collider2D enemyCollider;
    public PathFinder pathFinder;
    public bool isAttacking = false;
    #endregion

    #region Coroutines
    protected Coroutine slowEffectCoroutine;
    protected Coroutine stunCoroutine;
    protected Coroutine dotDamageCoroutine;
    protected Coroutine defenseDebuffCoroutine;
    #endregion

    private bool isQuitting = false;
    #endregion

    #region Unity Lifecycle

    protected virtual void Initialize()
    {
        maxHp = hp;
        originalMoveSpeed = moveSpeed;
        currentDefense = baseDefense;
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
    }

    protected virtual void Update()
    {
        if (isInit)
        {
            UpdateVisuals();

            float distanceToPlayer = Vector2.Distance(transform.position, target.position);

            if (distanceToPlayer <= attackRange)
            {
                Attack();
            }
            else
            {
                isAttacking = false;
            }
        }
    }

    private void FixedUpdate()
    {
        if (isInit && !isAttacking)
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

        moveSpeedDebuffAmount = 0f;
        defenseDebuffAmount = 0f;
        isStunned = false;
        moveSpeed = originalMoveSpeed;
        currentDefense = baseDefense;

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

        float damageReduction = currentDefense / (currentDefense + 100f);
        float finalDamage = damage * (1f - damageReduction) * (1f + defenseDebuffAmount);

        hp -= finalDamage;

        if (hp <= 0)
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
        if (expParticlePrefab != null)
        {
            int expParticleCount = Random.Range(minExpParticles, maxExpParticles + 1);
            float expPerParticle = mobEXP / expParticleCount;

            for (int i = 0; i < expParticleCount; i++)
            {
                Vector3 spawnPosition = transform.position;
                ExpParticle expParticle = PoolManager.Instance.Spawn<ExpParticle>(
                    expParticlePrefab.gameObject,
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
            .Instance.PlayerSystem.Player.GetComponent<PlayerStat>()
            .GetStat(StatType.Luck);

        Vector2 dropPosition = CalculateDropPosition();
        GameManager.Instance.ItemSystem.DropItem(dropPosition, monsterType, 1f + playerLuck);
    }

    protected virtual Vector2 CalculateDropPosition()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = Random.Range(dropRadiusMin, dropRadiusMax);

        Vector2 offset = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

        return (Vector2)transform.position + offset;
    }

    protected virtual void Attack()
    {
        if (Time.time >= preDamageTime + damageInterval)
        {
            float distanceToTarget = Vector2.Distance(transform.position, target.position);

            if (distanceToTarget <= attackRange)
            {
                isAttacking = true;
                if (this is RangedMonster || this is BossMonster)
                {
                    PerformRangedAttack();
                }
                else
                {
                    PerformMeleeAttack();
                }
            }
            else
            {
                isAttacking = false;
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
        float actualReduction = Mathf.Min(amount, maxDefenseReduction - defenseDebuffAmount);

        defenseDebuffAmount += actualReduction;
        currentDefense = baseDefense * (1f - defenseDebuffAmount);

        yield return new WaitForSeconds(duration);

        if (this != null && gameObject.activeInHierarchy)
        {
            defenseDebuffAmount = Mathf.Max(defenseDebuffAmount - actualReduction, 0f);
            currentDefense = baseDefense * (1f - defenseDebuffAmount);
        }
        defenseDebuffCoroutine = null;
    }

    public virtual void ModifyBaseDefense(float amount)
    {
        baseDefense = Mathf.Max(0, baseDefense + amount);
        UpdateCurrentDefense();
    }

    public virtual void SetBaseDefense(float newDefense)
    {
        baseDefense = Mathf.Max(0, newDefense);
        UpdateCurrentDefense();
    }

    public virtual void UpdateCurrentDefense()
    {
        currentDefense = baseDefense * (1f - defenseDebuffAmount);
    }

    public virtual void ApplySlowEffect(float amount, float duration)
    {
        if (!gameObject.activeInHierarchy)
            return;

        moveSpeedDebuffAmount = Mathf.Min(moveSpeedDebuffAmount + amount, 0.9f);
        UpdateMoveSpeed();

        if (slowEffectCoroutine != null)
        {
            StopCoroutine(slowEffectCoroutine);
        }

        slowEffectCoroutine = StartCoroutine(SlowEffectCoroutine(amount, duration));
    }

    protected virtual IEnumerator SlowEffectCoroutine(float amount, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (this != null && gameObject.activeInHierarchy)
        {
            moveSpeedDebuffAmount = Mathf.Max(moveSpeedDebuffAmount - amount, 0f);
            UpdateMoveSpeed();
        }
        slowEffectCoroutine = null;
    }

    public virtual void UpdateMoveSpeed()
    {
        moveSpeed = originalMoveSpeed * (1f - moveSpeedDebuffAmount);
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

        while (Time.time < endTime && hp > 0 && gameObject.activeInHierarchy)
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
        float originalSpeed = moveSpeed;
        moveSpeed = 0;

        yield return new WaitForSeconds(duration);

        if (this != null && gameObject.activeInHierarchy)
        {
            isStunned = false;
            moveSpeed = originalSpeed * (1f - moveSpeedDebuffAmount);
        }
        stunCoroutine = null;
    }
    #endregion

    #region Collision
    public virtual void Contact()
    {
        var particle = Instantiate(attackParticle, target.position, Quaternion.identity);
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
            hpBar.fillAmount = hpAmount;
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
        if (target != null)
        {
            float currentXPosition = transform.position.x;
            if (currentXPosition != pathFinder.previousXPosition)
            {
                Vector3 scale = transform.localScale;
                scale.x = (currentXPosition - pathFinder.previousXPosition) > 0 ? -1 : 1;
                transform.localScale = scale;
                pathFinder.previousXPosition = currentXPosition;
            }
        }
    }
    #endregion
}
