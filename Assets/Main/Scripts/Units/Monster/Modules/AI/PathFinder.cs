using System.Collections.Generic;
using UnityEngine;

public class PathFinder : MonoBehaviour
{
    public List<Vector2> currentPath { get; private set; }
    protected float lastPathUpdateTime;

    [SerializeField]
    private Rigidbody2D rb;

    [SerializeField]
    private Monster monster;

    public void Initialize(Monster monster, Rigidbody2D rb)
    {
        this.monster = monster;
        this.rb = rb;
        currentPath = null;
        lastPathUpdateTime = 0f;
    }

    public void OnValidate()
    {
        monster = GetComponent<Monster>();
    }

    public void Move()
    {
        if (monster.isStunned || monster.stat.GetStat(StatType.MoveSpeed) <= 0)
            return;
        if (monster.Target == null)
            return;

        float attackRange = monster.stat.GetStat(StatType.AttackRange);
        float distanceToTarget = Vector2.Distance(transform.position, monster.Target.position);

        if (distanceToTarget > attackRange)
        {
            if (currentPath == null || currentPath.Count == 0)
            {
                currentPath = GameManager.Instance.PathFindingSystem.FindPath(
                    transform.position,
                    monster.Target.position
                );
            }
            FollowPath();
        }
        else
        {
            currentPath = null;
        }
    }

    private bool HasObstacle()
    {
        float checkRadius = 1f;
        int obstacleLayer = LayerMask.GetMask("Obstacle");
        Collider2D hit = Physics2D.OverlapCircle(transform.position, checkRadius, obstacleLayer);
        return hit != null;
    }

    private void FollowPath()
    {
        if (currentPath == null || currentPath.Count == 0)
            return;

        Vector2 currentPos = transform.position;
        Vector2 nextWaypoint = currentPath[0];
        float dist = Vector2.Distance(currentPos, nextWaypoint);

        float speed = monster.stat.GetStat(StatType.MoveSpeed);
        float maxStep = speed * Time.fixedDeltaTime;
        float nodeSize = GameManager.Instance.PathFindingSystem.nodeSize;
        float reachThreshold = Mathf.Max(maxStep, nodeSize * 0.5f);

        if (dist <= reachThreshold)
        {
            currentPath.RemoveAt(0);
            return;
        }

        Vector2 dir = (nextWaypoint - currentPos).normalized;
        Vector2 newPosition = currentPos + dir * maxStep;
        transform.position = newPosition;
    }

    #region Gizmos
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || monster == null || monster.Target == null)
            return;

        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = new Color(0, 1, 0, 0.8f);
            Gizmos.DrawLine(transform.position, currentPath[0]);
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.color = new Color(0, 0.7f, 1f, 0.8f);
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
                Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.8f);
                Gizmos.DrawSphere(currentPath[i], 0.1f);
            }
            if (currentPath.Count > 0)
            {
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
                Gizmos.DrawSphere(currentPath[currentPath.Count - 1], 0.15f);
            }
        }

        Vector2 moveTarget;
        if (currentPath != null && currentPath.Count > 0)
        {
            moveTarget = currentPath[currentPath.Count - 1];
        }
        else
        {
            moveTarget = monster.Target.position;
        }
        Gizmos.color = new Color(1f, 1f, 0f, 0.7f);
        Gizmos.DrawLine(transform.position, moveTarget);
        Gizmos.DrawSphere(moveTarget, 0.18f);

        if (monster.Target != null)
        {
            Vector2 targetPos = monster.Target.position;
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawWireSphere(targetPos, 0.3f);
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);
            Gizmos.DrawWireSphere(monster.Target.position, monster.preferredDistance);
        }
    }
    #endregion
}
