using UnityEngine;

public class ExplodingShot : ProjectileSkills
{
    protected override void Fire()
    {
        Projectile proj = PoolManager.Instance.Spawn<Projectile>(
            skillData.ProjectilePrefab,
            transform.position,
            transform.rotation
        );

        if (proj != null)
        {
            proj.Initialize(
                Damage,
                ProjectileSpeed,
                IsHoming,
                ProjectileScale,
                PierceCount,
                AttackRange,
                skillData.Element,
                ElementalPower,
                FindNearestEnemy()?.transform
            );
            proj.SetDirection(fireDir);
            proj.transform.localScale = Vector3.one * ProjectileScale;
        }
    }

    public override string GetDetailedDescription()
    {
        string baseDesc = skillData?.Description ?? "ExplodingShot description";
        if (skillData?.GetSkillStats() != null)
        {
            baseDesc +=
                $"\n\nCurrent Effects:"
                + $"\nDirect Damage: {Damage:F1}"
                + $"\nExplosion Radius: {ExplosionRadius:F1}"
                + $"\nFire Rate: {1 / ShotInterval:F1} shots/s"
                + $"\nRange: {AttackRange:F1}";

            if (IsHoming)
            {
                baseDesc += $"\nHoming Range: {HomingRange:F1}";
            }
        }
        return baseDesc;
    }
}
