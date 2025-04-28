using System.Collections.Generic;
using UnityEngine;

public class PathDamageSkill : AreaSkills
{
    [SerializeField]
    private float pathWidth = 2f;

    [SerializeField]
    private float minDistanceBetweenPoints = 0.5f;

    [SerializeField]
    private DamageZone damageZonePrefab;
    private List<Vector2> pathPoints = new List<Vector2>();
    private List<GameObject> activeZones = new List<GameObject>();
    private Vector2 lastRecordedPosition;
    private bool isActive = true;

    public override void Initialize()
    {
        base.Initialize();
        lastRecordedPosition = transform.position;
    }

    public void StartPathDamage()
    {
        isActive = true;
        pathPoints.Clear();
        pathPoints.Add(transform.position);
        lastRecordedPosition = transform.position;

        foreach (var zone in activeZones)
        {
            if (zone != null)
                PoolManager.Instance.Despawn(zone.GetComponent<DamageZone>());
        }
        activeZones.Clear();
    }

    public void StopPathDamage()
    {
        isActive = false;
    }

    private void Update()
    {
        if (!isActive)
            return;

        float distanceFromLast = Vector2.Distance(
            (Vector2)transform.position,
            lastRecordedPosition
        );

        if (distanceFromLast >= minDistanceBetweenPoints)
        {
            pathPoints.Add(transform.position);
            lastRecordedPosition = transform.position;
            CreateDamageArea(transform.position);
        }
    }

    private void CreateDamageArea(Vector2 position)
    {
        DamageZone damageZone = PoolManager.Instance.Spawn<DamageZone>(
            damageZonePrefab.gameObject,
            position,
            Quaternion.identity
        );

        if (damageZone != null)
        {
            damageZone.Initialize(Damage, Duration, TickRate, pathWidth);

            CircleCollider2D collider = damageZone.GetComponent<CircleCollider2D>();
            if (collider != null)
            {
                collider.radius = 0.5f;
                collider.isTrigger = true;
            }

            activeZones.Add(damageZone.gameObject);
            StartCoroutine(DespawnAfterDuration(damageZone.gameObject));
        }
        else
        {
            print("no DamageZone");
        }
    }

    private System.Collections.IEnumerator DespawnAfterDuration(GameObject obj)
    {
        yield return new WaitForSeconds(Duration);
        if (obj != null)
        {
            activeZones.Remove(obj);
            PoolManager.Instance.Despawn<DamageZone>(obj.GetComponent<DamageZone>());
        }
    }

    public override string GetDetailedDescription()
    {
        string baseDesc = "Creates a damaging path behind the player";
        if (skillData?.GetSkillStats() != null)
        {
            baseDesc +=
                $"\n\nCurrent Effects:"
                + $"\nDamage: {Damage:F1}"
                + $"\nPath Width: {pathWidth:F1}"
                + $"\nDuration: {Duration:F1}s"
                + $"\nDamage Interval: {TickRate:F1}s";
        }
        return baseDesc;
    }
}
