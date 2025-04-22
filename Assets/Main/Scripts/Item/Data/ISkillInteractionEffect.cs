public interface ISkillInteractionEffect
{
    void OnSkillCast(Skill skill, Player player);
    void OnSkillHit(Skill skill, Player player, Monster target);
    void OnSkillKill(Skill skill, Player player, Monster target);
    void ModifySkillStats(Skill skill);
}
