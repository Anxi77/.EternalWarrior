using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class Player : MonoBehaviour
{
    #region Members

    #region Status
    public enum Status
    {
        Alive = 1,
        Dead,
        Attacking,
    }

    private Status _playerStatus;
    public Status playerStatus
    {
        get { return _playerStatus; }
        set { _playerStatus = value; }
    }
    #endregion

    #region Level & Experience
    [Header("Level Related")]
    [SerializeField]
    public int level = 1;
    public float exp = 0f;

    private List<float> expList = new List<float>
    {
        100,
        250,
        450,
        700,
        1000,
        1350,
        1750,
        2200,
        2700,
        3300,
    };

    public List<float> _expList
    {
        get { return expList; }
    }

    #endregion

    #region References
    private Vector2 moveInput;
    private Vector2 velocity;
    public PlayerStat playerStat;
    public Rigidbody2D rb;
    public SPUM_Prefabs animationController;
    public List<Skill> skills;
    public Inventory inventory;
    public PlayerInput playerInput;

    public Action<float, float> OnHpChanged;
    public Action<float, float> OnExpChanged;
    #endregion

    #endregion

    public bool IsInitialized { get; private set; }

    public void Initialize(StatData saveData, InventoryData inventoryData)
    {
        gameObject.name = "Player";
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        animationController.Initialize();
        playerStat.Initialize(saveData);
        inventory.Initialize(this, inventoryData);
        playerInput.Initialize(this);
        StartCombatSystems();
        IsInitialized = true;
    }

    private void OnDisable()
    {
        CleanupPlayer();
    }

    private void CleanupPlayer()
    {
        StopAllCoroutines();

        if (autoAttackCoroutine != null)
        {
            StopCoroutine(autoAttackCoroutine);
            autoAttackCoroutine = null;
        }

        if (healthRegenCoroutine != null)
        {
            StopCoroutine(healthRegenCoroutine);
            healthRegenCoroutine = null;
        }

        if (skills != null)
        {
            foreach (var skill in skills)
            {
                if (skill != null)
                {
                    Destroy(skill.gameObject);
                }
            }
            skills.Clear();
        }

        playerInput.Cleanup();

        playerStatus = Status.Dead;
        IsInitialized = false;
    }

    public void StartCombatSystems()
    {
        if (playerStatus != Status.Dead)
        {
            if (playerStat == null)
            {
                Logger.LogError(typeof(Player), "PlayerStat is null!");
                return;
            }

            StartHealthRegeneration();
            StartAutoAttack();
        }
    }

    private void Update()
    {
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        Move();
    }

    #region Methods

    #region Move&Skills
    public void Move()
    {
        float moveSpeed = playerStat.GetStat(StatType.MoveSpeed);
        velocity = moveInput * moveSpeed;

        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }

    public void SetMoveInput(Vector2 moveDirection)
    {
        moveInput = moveDirection;
    }

    private void UpdateAnimation()
    {
        if (animationController != null && playerStatus != Status.Attacking)
        {
            if (velocity != Vector2.zero)
            {
                animationController.transform.localScale = new Vector3(
                    moveInput.x > 0
                        ? -1
                        : (moveInput.x < 0 ? 1 : animationController.transform.localScale.x),
                    1,
                    1
                );
                animationController.PlayAnimation(PlayerState.MOVE, 0);
            }
            else
            {
                animationController.PlayAnimation(PlayerState.IDLE, 0);
            }
        }
    }

    #endregion

    #region Level & EXP
    public float CurrentExp()
    {
        if (level >= expList.Count)
        {
            return 0;
        }

        if (level == 1)
        {
            return exp;
        }
        else
        {
            return exp - expList[level - 2];
        }
    }

    public float GetExpForNextLevel()
    {
        if (level >= expList.Count)
        {
            return 99999f;
        }

        if (level == 1)
        {
            return expList[0];
        }
        else
        {
            return expList[level - 1] - expList[level - 2];
        }
    }

    public void GainExperience(float amount)
    {
        if (level >= expList.Count)
            return;

        exp += amount;

        OnExpChanged?.Invoke(exp, GetExpForNextLevel());

        if (level < expList.Count && exp >= expList[level - 1])
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        level++;

        float currentHpRatio =
            playerStat.GetStat(StatType.CurrentHp) / playerStat.GetStat(StatType.MaxHp);

        playerStat.UpdateStatsForLevel(level);

        float maxHp = playerStat.GetStat(StatType.MaxHp);

        playerStat.SetCurrentHp(maxHp * currentHpRatio);

        Logger.Log(
            typeof(Player),
            $"Level Up! Level: {level}, New MaxHP: {maxHp}, Current HP: {playerStat.GetStat(StatType.CurrentHp)}"
        );
    }
    #endregion

    #region Interactions

    public void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.TryGetComponent<IContactable>(out var contact))
        {
            contact.Contact();
        }

        if (other.gameObject.CompareTag("Enemy"))
        {
            rb.constraints = RigidbodyConstraints2D.FreezePosition;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.TryGetComponent<IContactable>(out var contact))
        {
            contact.Contact();
        }
    }

    public void TakeHeal(float heal)
    {
        float currentHp = playerStat.GetStat(StatType.CurrentHp);
        float maxHp = playerStat.GetStat(StatType.MaxHp);

        currentHp = Mathf.Min(currentHp + heal, maxHp);
        playerStat.SetCurrentHp(currentHp);
        OnHpChanged?.Invoke(currentHp, maxHp);
    }

    public void TakeDamage(float damage)
    {
        float defense = playerStat.GetStat(StatType.Defense);
        float actualDamage = Mathf.Max(1, damage - defense);
        float currentHp = playerStat.GetStat(StatType.CurrentHp);
        float maxHp = playerStat.GetStat(StatType.MaxHp);
        currentHp -= actualDamage;
        playerStat.SetCurrentHp(currentHp);

        OnHpChanged?.Invoke(currentHp, maxHp);

        if (currentHp <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        playerStatus = Status.Dead;
        StopAllCoroutines();

        GameManager.Instance.ChangeState(GameState.GameOver);
    }

    #endregion

    #region Combat
    private Coroutine autoAttackCoroutine;
    private float attackAngle = 120f;

    private IEnumerator PerformAttack(Monster targetEnemy)
    {
        if (animationController == null)
            yield break;

        Vector2 directionToTarget = (
            targetEnemy.transform.position - transform.position
        ).normalized;

        animationController.transform.localScale = new Vector3(
            directionToTarget.x > 0 ? -1 : 1,
            1,
            1
        );

        playerStatus = Status.Attacking;
        animationController.PlayAnimation(PlayerState.ATTACK, 0);

        float attackRange = playerStat.GetStat(StatType.AttackRange);
        float damage = playerStat.GetStat(StatType.Damage);

        var enemiesInRange = GameManager
            .Instance.Monsters.Where(enemy => enemy != null)
            .Where(enemy =>
            {
                Vector2 directionToEnemy = enemy.transform.position - transform.position;
                float distanceToEnemy = directionToEnemy.magnitude;
                float angle = Vector2.Angle(directionToTarget, directionToEnemy);

                return distanceToEnemy <= attackRange && angle <= attackAngle / 2f;
            })
            .ToList();

        foreach (Monster enemy in enemiesInRange)
        {
            float random = Random.Range(0f, 100f);

            if (random <= playerStat.GetStat(StatType.CriticalChance))
            {
                damage *= playerStat.GetStat(StatType.CriticalDamage);
            }

            enemy.TakeDamage(damage);
            playerStat.SetCurrentHp(
                playerStat.GetStat(StatType.CurrentHp)
                    + playerStat.GetStat(StatType.LifeSteal) / 100 * damage
            );
        }
    }

    private Monster FindNearestEnemy()
    {
        return GameManager
            .Instance.Monsters?.Where(enemy => enemy != null)
            .OrderBy(enemy => Vector2.Distance(transform.position, enemy.transform.position))
            .FirstOrDefault();
    }

    #endregion

    #region Passive Skill Effects
    public void ActivateHoming(bool activate)
    {
        foreach (var skill in skills)
        {
            if (skill is ProjectileSkills ProjectileSkills)
            {
                ProjectileSkills.UpdateHomingState(activate);
            }
        }
    }

    public void ResetPassiveEffects()
    {
        var passiveSkills = skills.Where(skill => skill is PassiveSkill).ToList();
        foreach (var skill in passiveSkills)
        {
            var passiveSkill = skill as PassiveSkill;
            foreach (var modifier in passiveSkill.statModifiers)
            {
                playerStat.RemoveModifier(modifier);
            }
        }
    }
    #endregion

    #region Health Regeneration
    private Coroutine healthRegenCoroutine;
    private const float REGEN_TICK_RATE = 1f;

    private void StartHealthRegeneration()
    {
        if (healthRegenCoroutine != null)
            StopCoroutine(healthRegenCoroutine);

        healthRegenCoroutine = StartCoroutine(HealthRegenCoroutine());
    }

    private IEnumerator HealthRegenCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(REGEN_TICK_RATE);

        while (true)
        {
            if (playerStat != null)
            {
                float regenAmount = playerStat.GetStat(StatType.HpRegenRate);
                if (regenAmount > 0)
                {
                    TakeHeal(regenAmount);
                }
            }
            yield return wait;
        }
    }
    #endregion

    #endregion

    #region Skills
    public bool AddOrUpgradeSkill(SkillData skillData)
    {
        if (skillData == null)
            return false;
        GameManager.Instance.SkillSystem.AddOrUpgradeSkill(skillData);
        return true;
    }

    public void RemoveSkill(SkillID skillID)
    {
        var skillToRemove = skills.Find(s => s.skillData.ID == skillID);
        if (skillToRemove != null)
        {
            skills.Remove(skillToRemove);
            Destroy(skillToRemove.gameObject);
        }
    }
    #endregion

    private void StartAutoAttack()
    {
        if (autoAttackCoroutine != null)
        {
            StopCoroutine(autoAttackCoroutine);
            autoAttackCoroutine = null;
        }
        autoAttackCoroutine = StartCoroutine(AutoAttackCoroutine());
    }

    private IEnumerator AutoAttackCoroutine()
    {
        while (true)
        {
            if (playerStatus != Status.Dead)
            {
                Monster nearestEnemy = FindNearestEnemy();
                if (nearestEnemy != null)
                {
                    float distanceToEnemy = Vector2.Distance(
                        transform.position,
                        nearestEnemy.transform.position
                    );
                    float attackRange = playerStat.GetStat(StatType.AttackRange);

                    if (distanceToEnemy <= attackRange)
                    {
                        yield return StartCoroutine(PerformAttack(nearestEnemy));
                    }
                }
            }

            float attackDelay = 1f / playerStat.GetStat(StatType.AttackSpeed);
            yield return new WaitForSeconds(attackDelay);
        }
    }
}
