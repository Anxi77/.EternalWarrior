using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PassiveSkill : Skill
{
    #region Runtime Stats

    [Header("Base Stats")]
    [SerializeField]
    protected float _damage = 10f;

    [SerializeField]
    protected float _elementalPower = 1f;

    [Header("Passive Effect Stats")]
    [SerializeField]
    protected float _effectDuration = 5f;

    [SerializeField]
    protected float _cooldown = 10f;

    [SerializeField]
    protected float _triggerChance = 100f;

    [SerializeField]
    protected float _damageIncrease = 0f;

    [SerializeField]
    protected float _defenseIncrease = 0f;

    [SerializeField]
    protected float _expAreaIncrease = 0f;

    [SerializeField]
    protected bool _homingActivate = false;

    [SerializeField]
    protected float _hpIncrease = 0f;

    [SerializeField]
    protected float _moveSpeedIncrease = 0f;

    [SerializeField]
    protected float _attackSpeedIncrease = 0f;

    [SerializeField]
    protected float _attackRangeIncrease = 0f;

    [SerializeField]
    protected float _hpRegenIncrease = 0f;

    [SerializeField]
    protected bool _isPermanent = false;
    public bool IsPermanent => _isPermanent;
    #endregion

    public PassiveSkillStat TypeStat
    {
        get
        {
            var stats = skillData?.GetStatsForLevel(currentLevel) as PassiveSkillStat;
            if (stats == null)
            {
                stats = new PassiveSkillStat
                {
                    baseStat = new BaseSkillStat
                    {
                        damage = _damage,
                        skillLevel = currentLevel,
                        maxSkillLevel = 5,
                        element = ElementType.None,
                        elementalPower = _elementalPower,
                    },
                    moveSpeedIncrease = _moveSpeedIncrease,
                    effectDuration = _effectDuration,
                    cooldown = _cooldown,
                    triggerChance = _triggerChance,
                    damageIncrease = _damageIncrease,
                    defenseIncrease = _defenseIncrease,
                    expAreaIncrease = _expAreaIncrease,
                    homingActivate = _homingActivate,
                    hpIncrease = _hpIncrease,
                };
                skillData?.SetStatsForLevel(currentLevel, stats);
            }
            return stats;
        }
    }

    public override void Initialize()
    {
        if (skillData == null)
            return;

        var playerStat = GameManager.Instance.PlayerSystem.Player.GetComponent<StatSystem>();
        if (playerStat != null)
        {
            float currentHpRatio = (
                playerStat.GetStat(StatType.CurrentHp) / playerStat.GetStat(StatType.MaxHp)
            ).ToFloat();

            InitializeSkillData();

            if (skillData.GetSkillStats() is PassiveSkillStat passiveSkillStat)
            {
                if (!passiveSkillStat.isPermanent)
                {
                    StartCoroutine(PassiveEffectCoroutine());
                }
                else
                {
                    ApplyPassiveEffect();
                }
            }
        }
        else
        {
            Logger.LogError(
                typeof(PassiveSkill),
                $"PlayerStatSystem not found for {skillData.Name}"
            );
        }
    }

    protected override void InitializeSkillData()
    {
        if (skillData == null)
            return;

        PassiveSkillStat statData = skillData.GetStatsForLevel(currentLevel) as PassiveSkillStat;

        if (statData != null)
        {
            UpdateInspectorValues(statData);
            skillData.SetStatsForLevel(currentLevel, statData);
        }
        else
        {
            Logger.LogWarning(typeof(PassiveSkill), $"No Stat data found for {skillData.Name}");
        }
    }

    protected virtual IEnumerator PassiveEffectCoroutine()
    {
        while (true)
        {
            if (Random.Range(0f, 100f) <= _triggerChance)
            {
                ApplyPassiveEffect();
            }
            yield return new WaitForSeconds(_cooldown);
        }
    }

    protected virtual void ApplyPassiveEffect()
    {
        if (_isPermanent)
        {
            ApplyPermanentEffect(owner);
        }
        else
        {
            ApplyTemporaryEffects(owner).Forget();
        }
    }

    protected void ApplyPermanentEffect(Unit owner)
    {
        var playerStat = owner.GetComponent<StatSystem>();
        if (playerStat == null)
            return;

        ApplyStatModifier(playerStat, StatType.Damage, _damageIncrease);
        ApplyStatModifier(playerStat, StatType.Defense, _defenseIncrease);
    }

    protected async UniTask ApplyTemporaryEffects(Unit owner)
    {
        var stat = owner.GetComponent<StatSystem>();
        if (stat == null)
            return;

        bool anyEffectApplied = false;

        if (_damageIncrease > 0)
        {
            ApplyStatModifier(stat, StatType.Damage, _damageIncrease);
            anyEffectApplied = true;
        }

        if (_defenseIncrease > 0)
        {
            ApplyStatModifier(stat, StatType.Defense, _defenseIncrease);
            anyEffectApplied = true;
        }

        if (_expAreaIncrease > 0)
        {
            ApplyStatModifier(stat, StatType.ExpCollectionRadius, _expAreaIncrease);
            anyEffectApplied = true;
        }

        if (_homingActivate)
        {
            owner.ActivateHoming(true);
            anyEffectApplied = true;
        }

        if (_hpIncrease > 0)
        {
            ApplyStatModifier(stat, StatType.MaxHp, _hpIncrease);
            anyEffectApplied = true;
        }

        if (_moveSpeedIncrease > 0)
        {
            ApplyStatModifier(stat, StatType.MoveSpeed, _moveSpeedIncrease);
            anyEffectApplied = true;
        }

        if (_attackSpeedIncrease > 0)
        {
            ApplyStatModifier(stat, StatType.AttackSpeed, _attackSpeedIncrease);
            anyEffectApplied = true;
        }

        if (_attackRangeIncrease > 0)
        {
            ApplyStatModifier(stat, StatType.AttackRange, _attackRangeIncrease);
            anyEffectApplied = true;
        }

        if (_hpRegenIncrease > 0)
        {
            ApplyStatModifier(stat, StatType.HpRegenRate, _hpRegenIncrease);
            anyEffectApplied = true;
        }

        if (anyEffectApplied)
        {
            await UniTask.WaitForSeconds(_effectDuration);

            owner.stat.RemoveStatFromSource(this);
        }
    }

    protected override void UpdateSkillTypeStats(ISkillStat newStats)
    {
        if (newStats is PassiveSkillStat passiveStats)
        {
            UpdateInspectorValues(passiveStats);
        }
    }

    protected virtual void UpdateInspectorValues(PassiveSkillStat stats)
    {
        if (stats == null || stats.baseStat == null)
        {
            Logger.LogError(
                typeof(PassiveSkill),
                $"Invalid stats passed to UpdateInspectorValues for {GetType().Name}"
            );
            return;
        }

        currentLevel = stats.baseStat.skillLevel;
        _damage = stats.baseStat.damage;
        _elementalPower = stats.baseStat.elementalPower;
        _effectDuration = stats.effectDuration;
        _cooldown = stats.cooldown;
        _triggerChance = stats.triggerChance;
        _damageIncrease = stats.damageIncrease;
        _defenseIncrease = stats.defenseIncrease;
        _expAreaIncrease = stats.expAreaIncrease;
        _homingActivate = stats.homingActivate;
        _hpIncrease = stats.hpIncrease;
        _moveSpeedIncrease = stats.moveSpeedIncrease;
        _attackSpeedIncrease = stats.attackSpeedIncrease;
        _attackRangeIncrease = stats.attackRangeIncrease;
        _hpRegenIncrease = stats.hpRegenIncrease;
    }

    protected void ApplyStatModifier(
        StatSystem playerStat,
        StatType statType,
        float percentageIncrease
    )
    {
        if (percentageIncrease <= 0)
            return;

        float currentStat = playerStat.GetStat(statType);
        float increase = currentStat * (percentageIncrease / 100f);
        playerStat.AddStat(statType, CalcType.Plus, increase, this);
        Logger.Log(
            typeof(PassiveSkill),
            $"Applied {statType} increase: Current({currentStat}) + {percentageIncrease}% = {currentStat + increase}"
        );
    }

    protected virtual void OnDisable()
    {
        if (GameManager.Instance?.PlayerSystem?.Player != null)
        {
            Player player = GameManager.Instance.PlayerSystem.Player;
            var playerStat = player.GetComponent<StatSystem>();

            if (playerStat != null)
            {
                playerStat.RemoveStatFromSource(this);
            }
            else
            {
                Logger.LogError(
                    typeof(PassiveSkill),
                    $"PlayerStatSystem not found for {skillData.Name} on destroy"
                );
            }
        }
    }
}
