public interface ISkillStat
{
    BaseSkillStat baseStat { get; set; }
    SkillType skillType { get; }
}

public interface IContactable
{
    public void Contact();
}

public interface IInitializable
{
    bool IsInitialized { get; }
    void Initialize();
}

public interface ISkillInteractionEffect
{
    void OnSkillCast(Skill skill);
    void OnSkillHit(Skill skill, Monster target);
    void OnSkillKill(Skill skill, Player player, Monster target);
    void ModifySkillStats(Skill skill);
}
