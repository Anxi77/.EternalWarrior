using System.Collections;
using System.Collections.Generic;
using UnityEditor.Callbacks;
using UnityEngine;

public class PathFinder : MonoBehaviour
{
    #region Pathfinding
    public List<Vector2> currentPath { get; private set; }
    protected float pathUpdateTime = 0.2f;
    protected float lastPathUpdateTime;
    protected float obstaclePathUpdateDelay = 0.1f;
    protected float lastObstacleAvoidanceTime;
    protected float stuckTimer = 0f;
    protected Vector2 lastPosition;
    #endregion

    #region Constants
    protected const float STUCK_THRESHOLD = 0.1f;
    protected const float STUCK_CHECK_TIME = 0.5f;
    protected const float CORNER_CHECK_DISTANCE = 0.5f;
    protected const float WALL_AVOIDANCE_DISTANCE = 1.5f;
    protected const float MIN_CIRCLE_DISTANCE = 1f;
    #endregion

    #region Movement
    protected Vector2 previousMoveDir;
    protected bool isCirclingPlayer = false;
    protected float circlingRadius = 3f;
    protected float circlingAngle = 0f;
    public float previousXPosition;
    #endregion

    #region Formation Variables
    private const float FORMATION_SPACING = 1.2f;
    private const float COHESION_WEIGHT = 0.3f;
    private const float ALIGNMENT_WEIGHT = 0.5f;
    private const float SEPARATION_WEIGHT = 0.8f;
    private const float FORMATION_RADIUS = 5f;
    private Vector2 formationOffset;
    private Rigidbody2D rb;

    #endregion

    [SerializeField]
    private Monster monster;

    public void Initialize(Monster monster, Rigidbody2D rb)
    {
        this.monster = monster;
        this.rb = rb;
        CalculateFormationOffset();
    }

    protected virtual void CalculateFormationOffset()
    {
        if (GameManager.Instance == null)
            return;

        int totalEnemies = GameManager.Instance.Monsters.Count;
        if (totalEnemies == 0)
        {
            formationOffset = Vector2.zero;
            return;
        }

        int index = GameManager.Instance.Monsters.IndexOf(monster);
        int rowSize = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(totalEnemies)));

        int row = index / rowSize;
        int col = index % rowSize;

        formationOffset = new Vector2(
            (col - rowSize / 2f) * FORMATION_SPACING,
            (row - rowSize / 2f) * FORMATION_SPACING
        );
    }

    private Vector2 GetTargetPosition(Transform target)
    {
        if (target == null)
            return transform.position;

        Vector2 directionToTarget = (
            (Vector2)target.position - (Vector2)transform.position
        ).normalized;
        float distanceToTarget = Vector2.Distance(transform.position, target.position);

        if (distanceToTarget > monster.attackRange)
        {
            return (Vector2)target.position - directionToTarget * monster.preferredDistance;
        }
        else if (distanceToTarget < monster.preferredDistance)
        {
            return (Vector2)target.position + directionToTarget * monster.preferredDistance;
        }
        else
        {
            return (Vector2)transform.position
                + new Vector2(Mathf.Sin(Time.time * 2f), Mathf.Cos(Time.time * 2f)) * 0.5f;
        }
    }

    public void Move()
    {
        if (monster.isStunned || monster.moveSpeed <= 0)
            return;

        Node currentNode = GameManager.Instance.PathFindingSystem.GetNodeFromWorldPosition(
            transform.position
        );
        if (currentNode != null && !currentNode.walkable)
        {
            Vector2 safePosition = FindNearestSafePosition(transform.position);
            rb.MovePosition(
                (Vector2)transform.position + safePosition * monster.moveSpeed * 2f * Time.deltaTime
            );
            return;
        }
        if (!GameManager.Instance.PathFindingSystem.IsPositionInGrid(transform.position))
        {
            Vector2 clampedPosition = GameManager.Instance.PathFindingSystem.ClampToGrid(
                transform.position
            );
            rb.MovePosition(clampedPosition);
            return;
        }

        Vector2 moveToPosition =
            (Vector2)monster.target.position
            - ((Vector2)monster.target.position - (Vector2)transform.position).normalized
                * monster.preferredDistance;
        if (Vector2.Distance(moveToPosition, monster.target.position) < monster.attackRange)
        {
            HandleCirclingBehavior(Vector2.Distance(moveToPosition, monster.target.position));
        }
        else
        {
            MoveToPosition(moveToPosition);
        }
    }

    private void MoveToPosition(Vector2 targetPosition)
    {
        if (ShouldUpdatePath())
        {
            List<Vector2> newPath = GameManager.Instance.PathFindingSystem.FindPath(
                transform.position,
                targetPosition
            );
            if (newPath != null && newPath.Count > 0)
            {
                bool isValidPath = true;
                foreach (Vector2 pathPoint in newPath)
                {
                    Node node = GameManager.Instance.PathFindingSystem.GetNodeFromWorldPosition(
                        pathPoint
                    );
                    if (node != null && !node.walkable)
                    {
                        isValidPath = false;
                        break;
                    }
                }

                if (isValidPath)
                {
                    currentPath = newPath;
                    lastPathUpdateTime = Time.time;
                    stuckTimer = 0f;
                }
                else
                {
                    Vector2 safePosition = FindSafePosition(targetPosition);
                    currentPath = GameManager.Instance.PathFindingSystem.FindPath(
                        transform.position,
                        safePosition
                    );
                }
            }
        }

        FollowPath();
    }

    private Vector2 FindSafePosition(Vector2 targetPosition)
    {
        float checkRadius = 2f;
        float angleStep = 45f;

        for (float angle = 0; angle < 360; angle += angleStep)
        {
            float radian = angle * Mathf.Deg2Rad;
            Vector2 checkPosition =
                targetPosition
                + new Vector2(Mathf.Cos(radian) * checkRadius, Mathf.Sin(radian) * checkRadius);

            Node node = GameManager.Instance.PathFindingSystem.GetNodeFromWorldPosition(
                checkPosition
            );
            if (node != null && node.walkable)
            {
                return checkPosition;
            }
        }

        return transform.position;
    }

    private bool HandleCirclingBehavior(float distanceToPlayer)
    {
        if (distanceToPlayer < MIN_CIRCLE_DISTANCE)
        {
            isCirclingPlayer = true;
            CircleAroundPlayer();
            return true;
        }
        isCirclingPlayer = false;
        return false;
    }

    private void CircleAroundPlayer()
    {
        if (monster.target == null)
            return;

        UpdateCirclingParameters();
        Vector2 targetPosition = CalculateCirclingPosition(monster.target);

        Node targetNode = GameManager.Instance.PathFindingSystem.GetNodeFromWorldPosition(
            targetPosition
        );
        if (targetNode != null && !targetNode.walkable)
        {
            targetPosition = FindSafeCirclingPosition(targetPosition);
        }

        ApplyCirclingMovement(targetPosition);
    }

    private Vector2 FindSafeCirclingPosition(Vector2 originalPosition)
    {
        float[] checkAngles = { 45f, -45f, 90f, -90f, 135f, -135f, 180f };

        foreach (float angleOffset in checkAngles)
        {
            float newAngle = circlingAngle + angleOffset;
            Vector2 checkPosition =
                (Vector2)monster.target.position
                + new Vector2(
                    Mathf.Cos(newAngle * Mathf.Deg2Rad),
                    Mathf.Sin(newAngle * Mathf.Deg2Rad)
                ) * circlingRadius;

            Node node = GameManager.Instance.PathFindingSystem.GetNodeFromWorldPosition(
                checkPosition
            );
            if (node != null && node.walkable)
            {
                return checkPosition;
            }
        }

        return originalPosition;
    }

    private void UpdateCirclingParameters()
    {
        int enemyCount = GameManager.Instance.Monsters.Count;
        circlingRadius = Mathf.Max(2.0f, Mathf.Min(3.0f, enemyCount * 0.5f));

        float baseAngle = Time.time * 20f;
        int myIndex = GameManager.Instance.Monsters.IndexOf(monster);
        float angleStep = 360f / Mathf.Max(1, enemyCount);
        float targetAngle = baseAngle + (myIndex * angleStep);

        circlingAngle = Mathf.LerpAngle(circlingAngle, targetAngle, Time.deltaTime * 5f);
    }

    private Vector2 CalculateCirclingPosition(Transform target)
    {
        Vector2 offset =
            new Vector2(
                Mathf.Cos(circlingAngle * Mathf.Deg2Rad),
                Mathf.Sin(circlingAngle * Mathf.Deg2Rad)
            ) * circlingRadius;

        return (Vector2)target.position + offset;
    }

    private void ApplyCirclingMovement(Vector2 targetPosition)
    {
        Vector2 moveDirection = CalculateAvoidanceDirection(transform.position, targetPosition);
        Vector2 separationForce = CalculateSeparationForce(transform.position);
        moveDirection = (moveDirection + separationForce * 0.1f).normalized;
        Vector2 targetVelocity = moveDirection * monster.moveSpeed * 1.2f;
        rb.MovePosition(rb.position + targetVelocity * Time.deltaTime);
    }

    private void UpdatePath()
    {
        if (ShouldUpdatePath())
        {
            List<Vector2> newPath = GameManager.Instance.PathFindingSystem.FindPath(
                transform.position,
                monster.target.position
            );
            if (newPath != null && newPath.Count > 0)
            {
                currentPath = newPath;
                lastPathUpdateTime = Time.time;
                stuckTimer = 0f;
            }
            else
            {
                MoveDirectlyTowardsTarget();
            }
        }
    }

    private bool ShouldUpdatePath()
    {
        if (currentPath == null || currentPath.Count == 0)
            return true;

        if (Time.time >= lastPathUpdateTime + pathUpdateTime)
        {
            Vector2 finalDestination = currentPath[currentPath.Count - 1];
            float distanceToFinalDestination = Vector2.Distance(
                finalDestination,
                monster.target.position
            );
            return distanceToFinalDestination > PathFindingSystem.NODE_SIZE * 2;
        }
        return false;
    }

    private void FollowPath()
    {
        if (currentPath == null || currentPath.Count == 0)
            return;

        Vector2 currentPos = transform.position;
        Vector2 nextWaypoint = currentPath[0];

        HandleStuckCheck(currentPos);
        ProcessWaypoint(currentPos, nextWaypoint);
        ApplyMovement(currentPos, nextWaypoint);
    }

    private void ProcessWaypoint(Vector2 currentPos, Vector2 nextWaypoint)
    {
        if (HasReachedWaypoint(currentPos, nextWaypoint))
        {
            if (currentPath != null && currentPath.Count > 0)
            {
                UpdateWaypoint();
                if (currentPath == null || currentPath.Count == 0)
                {
                    MoveDirectlyTowardsTarget();
                }
            }
        }
    }

    private Vector2 FindNearestSafePosition(Vector2 currentPosition)
    {
        float checkRadius = 1f;
        int maxAttempts = 8;
        float angleStep = 360f / maxAttempts;

        for (int i = 0; i < maxAttempts; i++)
        {
            float angle = i * angleStep;
            float radian = angle * Mathf.Deg2Rad;
            Vector2 checkPosition =
                currentPosition
                + new Vector2(Mathf.Cos(radian) * checkRadius, Mathf.Sin(radian) * checkRadius);

            Node node = GameManager.Instance.PathFindingSystem.GetNodeFromWorldPosition(
                checkPosition
            );
            if (node != null && node.walkable)
            {
                return checkPosition;
            }
        }

        return FindNearestSafePosition(currentPosition + Vector2.one * checkRadius);
    }

    private void MoveDirectlyTowardsTarget()
    {
        if (monster.target == null)
            return;

        Vector2 currentPos = transform.position;
        Vector2 targetPos = GetTargetPosition(monster.target);

        Vector2 flockingForce = CalculateFlockingForce(currentPos);

        Vector2 formationPos = (Vector2)monster.target.position + formationOffset;
        Vector2 formationDir = (formationPos - currentPos).normalized;

        Vector2 moveDir = (
            (targetPos - currentPos).normalized + flockingForce + formationDir
        ).normalized;
        moveDir = CalculateAvoidanceDirection(currentPos, currentPos + moveDir);

        Vector2 separationForce = CalculateSeparationForce(currentPos);
        moveDir = (moveDir + separationForce * 0.2f).normalized;

        Vector2 targetVelocity = moveDir * monster.moveSpeed;

        rb.MovePosition(rb.position + targetVelocity * Time.deltaTime);
    }

    private Vector2 CalculateSeparationForce(Vector2 currentPos)
    {
        Vector2 separationForce = Vector2.zero;
        float separationRadius = isCirclingPlayer ? 0.8f : 1.2f;

        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(
            currentPos,
            separationRadius,
            LayerMask.GetMask("Enemy")
        );
        foreach (Collider2D enemyCollider in nearbyEnemies)
        {
            if (enemyCollider.gameObject != gameObject)
            {
                Vector2 diff = currentPos - (Vector2)enemyCollider.transform.position;
                float distance = diff.magnitude;
                if (distance < separationRadius)
                {
                    float strength = isCirclingPlayer ? 0.5f : 1f;
                    separationForce +=
                        diff.normalized * (1 - distance / separationRadius) * strength;
                }
            }
        }

        return separationForce.normalized * (isCirclingPlayer ? 0.3f : 0.5f);
    }

    private void ApplyMovement(Vector2 currentPos, Vector2 nextWaypoint)
    {
        Vector2 moveDirection = CalculateAvoidanceDirection(currentPos, nextWaypoint);

        Vector2 separationForce = CalculateSeparationForce(currentPos);
        moveDirection = (moveDirection + separationForce * 0.2f).normalized;

        Vector2 targetVelocity = moveDirection * monster.moveSpeed;
        rb.velocity = Vector2.Lerp(rb.velocity, targetVelocity, Time.deltaTime * 5f);
    }

    private Vector2 CalculateAvoidanceDirection(Vector2 currentPosition, Vector2 targetPosition)
    {
        Vector2 moveDir = (targetPosition - currentPosition).normalized;
        Vector2 finalMoveDir = moveDir;

        Vector2 dirToTarget = (Vector2)monster.target.position - currentPosition;
        bool isVerticalAligned = Mathf.Abs(dirToTarget.x) < 0.1f;
        bool isHorizontalAligned = Mathf.Abs(dirToTarget.y) < 0.1f;

        if (
            (isVerticalAligned || isHorizontalAligned)
            && Physics2D.Raycast(
                currentPosition,
                moveDir,
                WALL_AVOIDANCE_DISTANCE,
                LayerMask.GetMask("Obstacle")
            )
        )
        {
            Vector2 alternativeDir = isVerticalAligned ? new Vector2(1f, 0f) : new Vector2(0f, 1f);
            if (
                !Physics2D.Raycast(
                    currentPosition,
                    alternativeDir,
                    WALL_AVOIDANCE_DISTANCE,
                    LayerMask.GetMask("Obstacle")
                )
            )
            {
                return alternativeDir;
            }
            if (
                !Physics2D.Raycast(
                    currentPosition,
                    -alternativeDir,
                    WALL_AVOIDANCE_DISTANCE,
                    LayerMask.GetMask("Obstacle")
                )
            )
            {
                return -alternativeDir;
            }
        }

        var obstacles = CheckObstacles(currentPosition, moveDir);
        if (HasObstacles(obstacles))
        {
            HandleObstacleAvoidance(obstacles);
            finalMoveDir = CalculateAvoidanceVector(obstacles);
        }

        return SmoothDirection(finalMoveDir);
    }

    private void HandleStuckCheck(Vector2 currentPos)
    {
        if (Vector2.Distance(currentPos, lastPosition) < STUCK_THRESHOLD)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > STUCK_CHECK_TIME)
            {
                ResetPath();
            }
        }
        else
        {
            stuckTimer = 0f;
        }
        lastPosition = currentPos;
    }

    private bool HasReachedWaypoint(Vector2 currentPos, Vector2 waypoint)
    {
        return Vector2.Distance(currentPos, waypoint) < PathFindingSystem.NODE_SIZE * 0.5f;
    }

    private void UpdateWaypoint()
    {
        if (currentPath != null && currentPath.Count > 0)
        {
            currentPath.RemoveAt(0);
        }
    }

    private void ResetPath()
    {
        currentPath = null;
        stuckTimer = 0f;
    }

    private (RaycastHit2D front, RaycastHit2D right, RaycastHit2D left) CheckObstacles(
        Vector2 position,
        Vector2 direction
    )
    {
        Vector2 rightCheck = Quaternion.Euler(0, 0, 30) * direction;
        Vector2 leftCheck = Quaternion.Euler(0, 0, -30) * direction;

        return (
            Physics2D.Raycast(
                position,
                direction,
                WALL_AVOIDANCE_DISTANCE,
                LayerMask.GetMask("Obstacle")
            ),
            Physics2D.Raycast(
                position,
                rightCheck,
                CORNER_CHECK_DISTANCE,
                LayerMask.GetMask("Obstacle")
            ),
            Physics2D.Raycast(
                position,
                leftCheck,
                CORNER_CHECK_DISTANCE,
                LayerMask.GetMask("Obstacle")
            )
        );
    }

    private bool HasObstacles((RaycastHit2D front, RaycastHit2D right, RaycastHit2D left) obstacles)
    {
        return obstacles.front.collider != null
            || obstacles.right.collider != null
            || obstacles.left.collider != null;
    }

    private void HandleObstacleAvoidance(
        (RaycastHit2D front, RaycastHit2D right, RaycastHit2D left) obstacles
    )
    {
        if (currentPath != null && Time.time >= lastObstacleAvoidanceTime + obstaclePathUpdateDelay)
        {
            ResetPathForObstacle();
        }
    }

    private void ResetPathForObstacle()
    {
        currentPath = null;
        lastPathUpdateTime = Time.time - pathUpdateTime;
        lastObstacleAvoidanceTime = Time.time;
    }

    private Vector2 CalculateAvoidanceVector(
        (RaycastHit2D front, RaycastHit2D right, RaycastHit2D left) obstacles
    )
    {
        Vector2 avoidDir = Vector2.zero;

        if (obstacles.front.collider != null)
        {
            avoidDir += -obstacles.front.normal * 3f;
        }
        if (obstacles.right.collider != null)
        {
            avoidDir += Vector2.Perpendicular(obstacles.right.normal) * 2f;
        }
        if (obstacles.left.collider != null)
        {
            avoidDir += -Vector2.Perpendicular(obstacles.left.normal) * 2f;
        }

        return avoidDir != Vector2.zero ? avoidDir.normalized : (Vector2)transform.right;
    }

    private Vector2 SmoothDirection(Vector2 finalMoveDir)
    {
        if (previousMoveDir != Vector2.zero)
        {
            finalMoveDir = Vector2.Lerp(previousMoveDir, finalMoveDir, Time.deltaTime * 20f);
        }
        previousMoveDir = finalMoveDir;
        return finalMoveDir;
    }

    private Vector2 CalculateFlockingForce(Vector2 currentPos)
    {
        Vector2 cohesion = Vector2.zero;
        Vector2 alignment = Vector2.zero;
        Vector2 separation = Vector2.zero;
        int neighborCount = 0;

        foreach (Monster monster in GameManager.Instance.Monsters)
        {
            if (monster == this)
                continue;

            float distance = Vector2.Distance(currentPos, monster.transform.position);
            if (distance < FORMATION_RADIUS)
            {
                cohesion += (Vector2)monster.transform.position;

                alignment += monster.rb.velocity;

                Vector2 diff = currentPos - (Vector2)monster.transform.position;
                separation += diff.normalized / Mathf.Max(distance, 0.1f);

                neighborCount++;
            }
        }

        if (neighborCount > 0)
        {
            cohesion = (cohesion / neighborCount - currentPos) * COHESION_WEIGHT;
            alignment = (alignment / neighborCount) * ALIGNMENT_WEIGHT;
            separation = separation * SEPARATION_WEIGHT;
        }

        return (cohesion + alignment + separation).normalized;
    }
}
