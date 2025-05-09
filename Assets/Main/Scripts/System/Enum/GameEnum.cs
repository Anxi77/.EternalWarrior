using System;

#region Skill

[Serializable]
public enum SkillType
{
    None = 0,
    Projectile,
    Area,
    Passive,
}

[Serializable]
public enum SkillID
{
    None = 100000,

    //Earth
    Vine, // Area
    EarthRift, // Projectile
    GaiasGrace, // Passive

    //Water
    FrostTide, // Area
    FrostHunt, // Projectile
    TidalEssence, // Passive

    //Dark
    ShadowWaltz, // Area
    EventHorizon, // Projectile
    AbyssalExpansion, // Passive

    //Fire
    Flame, // Projectile
    FireRing, // Area
    ThermalElevation, // Passive
}

public enum FireMode
{
    Manual, // 마우스 클릭으로 발사
    Auto, // 자동 발사
    AutoHoming, // 자동 호밍 발사
}

#endregion

#region Item

[Serializable]
public enum EffectType
{
    None,
    DamageBonus,
    CooldownReduction,
    ProjectileSpeed,
    ProjectileRange,
    HomingEffect,
    AreaRadius,
    AreaDuration,
    ElementalPower,
}

[Serializable]
public enum ItemType
{
    None,
    Weapon,
    Armor,
    Accessory,
    Consumable,
    Material,
}

[Serializable]
public enum AccessoryType
{
    None,
    Necklace,
    Ring,
}

[Serializable]
public enum ItemRarity
{
    None,
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
}

#endregion

#region Player

[Serializable]
public enum StatType
{
    #region Player
    None,
    MaxHp,
    CurrentHp,
    Damage,
    Defense,
    MoveSpeed,
    AttackSpeed,
    AttackRange,
    AttackRadius,
    ExpCollectionRadius,
    HpRegenRate,
    CriticalChance,
    CriticalDamage,
    Luck,
    DodgeChance,
    LifeSteal,
    #endregion

    #region Monster

    DropExp,
    MaxDefenseReduction,
    MaxMoveSpeedReduction,
    #endregion
}

[Serializable]
public enum CalcType
{
    Flat,
    Multiply,
}

#endregion

#region System

public enum GameState
{
    Initialize,
    Title,
    Town,
    Stage,
    Paused,
    GameOver,
}

public enum PanelType
{
    None,
    Loading,
    Title,
    PlayerInfo,
    StageTime,
    Inventory,
    GameOver,
    Pause,
    BossWarning,
    Skill,
    Test,
}

public enum MonsterType
{
    None,
    Wasp,
    Bat,
    Ogre,
}
#endregion

[Serializable]
public enum ElementType
{
    None = 0,
    Dark,
    Water,
    Fire,
    Earth,
}

[Serializable]
public enum SlotType
{
    Storage,
    Weapon,
    Armor,
    Ring,
    SecondaryRing,
    Necklace,
    Special,
}
