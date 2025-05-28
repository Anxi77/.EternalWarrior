using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class Player : Unit
{
    #region Members

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

    public List<float> ExpList
    {
        get { return expList; }
    }

    #endregion

    #region References
    private Vector2 moveInput;
    private Vector2 velocity;
    public Rigidbody2D rb;
    public SPUM_Prefabs animationController;

    public Inventory inventory;
    public PlayerInput playerInput;

    public Action OnLevelUp;
    public Action<float, float> OnExpChanged;
    #endregion

    #endregion

    public bool IsInitialized { get; private set; }

    public void Initialize(StatData saveData, InventoryData inventoryData)
    {
        unitStatus = UnitStatus.Alive;
        gameObject.name = "Player";
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        animationController.Initialize();
        stat.Initialize(saveData);
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
        if (autoAttackCoroutine != null)
        {
            StopCoroutine(autoAttackCoroutine);
            autoAttackCoroutine = null;
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

        unitStatus = UnitStatus.Dead;
        IsInitialized = false;
    }

    public void StartCombatSystems()
    {
        if (unitStatus != UnitStatus.Dead)
        {
            if (stat == null)
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
        velocity = moveInput * stat.GetStat(StatType.MoveSpeed).ToFloat();

        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }

    public void SetMoveInput(Vector2 moveDirection)
    {
        moveInput = moveDirection;
    }

    private void UpdateAnimation()
    {
        if (animationController != null)
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
            return 0;

        float baseExp = (level == 1) ? 0 : expList[level - 2];
        float nextLevelExp = expList[level - 1];
        return Mathf.Clamp(exp - baseExp, 0, nextLevelExp - baseExp);
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

        OnExpChanged?.Invoke(CurrentExp(), GetExpForNextLevel());

        if (level < expList.Count && exp >= expList[level - 1])
        {
            StartCoroutine(ProcessLevelUps());
        }
    }

    private IEnumerator ProcessLevelUps()
    {
        while (level < expList.Count && exp >= expList[level - 1])
        {
            level++;

            SkillPanel skillPanel = UIManager.Instance.OpenPanel(PanelType.Skill) as SkillPanel;

            if (skillPanel != null)
            {
                yield return new WaitUntil(() => !skillPanel.IsOpen);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }

            OnLevelUp?.Invoke();
        }
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

    public override void Die()
    {
        unitStatus = UnitStatus.Dead;
        StopAllCoroutines();

        GameManager.Instance.ChangeState(GameState.GameOver);
    }

    #endregion

    #region Combat
    private Coroutine autoAttackCoroutine;
    private float attackAngle = 120f;

    private void Attack(Monster targetEnemy)
    {
        if (animationController == null)
            return;

        Vector2 directionToTarget = (
            targetEnemy.transform.position - transform.position
        ).normalized;

        animationController.transform.localScale = new Vector3(
            directionToTarget.x > 0 ? -1 : 1,
            1,
            1
        );

        unitStatus = UnitStatus.Attacking;
        animationController.PlayAnimation(PlayerState.ATTACK, 0);

        float attackRange = stat.GetStat(StatType.AttackRange).ToFloat();
        ScaledInt damage = stat.GetStat(StatType.Damage);

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

            if (random <= stat.GetStat(StatType.CriticalChance))
            {
                damage *= stat.GetStat(StatType.CriticalDamage);
            }

            enemy.TakeDamage(damage, this);
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

    public void ResetPassiveEffects()
    {
        var passiveSkills = skills.Where(skill => skill is PassiveSkill).ToList();
        foreach (var skill in passiveSkills)
        {
            stat.RemoveStatFromSource(skill);
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
        var playerInfoPanel = UIManager.Instance.GetPanel(PanelType.PlayerInfo) as PlayerInfoPanel;
        if (playerInfoPanel != null)
        {
            playerInfoPanel.UpdateSkillList();
        }
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
        AutoAttack().Forget();
    }

    private async UniTask AutoAttack()
    {
        while (true)
        {
            if (UnitStatus != UnitStatus.Dead)
            {
                Monster nearestEnemy = FindNearestEnemy();
                if (nearestEnemy != null)
                {
                    float distanceToEnemy = Vector2.Distance(
                        transform.position,
                        nearestEnemy.transform.position
                    );
                    float attackRange = stat.GetStat(StatType.AttackRange);

                    if (distanceToEnemy <= attackRange)
                    {
                        Attack(nearestEnemy);
                    }
                }
            }

            float attackDelay = 1f / stat.GetStat(StatType.AttackSpeed);
            await UniTask.WaitForSeconds(attackDelay);
        }
    }
}
