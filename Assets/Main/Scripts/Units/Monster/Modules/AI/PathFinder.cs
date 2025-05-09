using System.Collections;
using System.Collections.Generic;
using UnityEditor.Callbacks;
using UnityEngine;

/// <summary>
/// 몬스터의 이동 및 경로 찾기를 관리하는 클래스
/// </summary>
public class PathFinder : MonoBehaviour
{
    #region Pathfinding
    /// <summary>현재 계산된 경로</summary>
    public List<Vector2> currentPath { get; private set; }

    /// <summary>경로 업데이트 간격</summary>
    protected float pathUpdateTime = 0.2f;

    /// <summary>마지막 경로 업데이트 시간</summary>
    protected float lastPathUpdateTime;

    /// <summary>장애물 회피 후 재계산 지연 시간</summary>
    protected float obstaclePathUpdateDelay = 0.1f;

    /// <summary>마지막 장애물 회피 시간</summary>
    protected float lastObstacleAvoidanceTime;

    /// <summary>정체 시간 측정</summary>
    protected float stuckTimer = 0f;

    /// <summary>이전 위치</summary>
    protected Vector2 lastPosition;
    #endregion

    #region Constants
    /// <summary>정체 간주 최소 이동거리</summary>
    protected const float STUCK_THRESHOLD = 0.1f;

    /// <summary>정체 확인 주기</summary>
    protected const float STUCK_CHECK_TIME = 0.5f;

    /// <summary>코너 확인 거리</summary>
    protected const float CORNER_CHECK_DISTANCE = 0.5f;

    /// <summary>벽 회피 감지 거리</summary>
    protected const float WALL_AVOIDANCE_DISTANCE = 1.5f;

    /// <summary>최소 원형 이동 거리</summary>
    protected const float MIN_CIRCLE_DISTANCE = 1f;
    #endregion

    #region Movement
    /// <summary>이전 이동 방향</summary>
    protected Vector2 previousMoveDir;

    /// <summary>플레이어 주변 원형 이동 여부</summary>
    protected bool isCirclingPlayer = false;

    /// <summary>원형 이동 반경</summary>
    protected float circlingRadius = 3f;

    /// <summary>원형 이동 각도</summary>
    protected float circlingAngle = 0f;

    /// <summary>이전 X 위치(방향 전환용)</summary>
    public float previousXPosition { get; set; }
    #endregion

    #region Formation Variables
    /// <summary>대형 간격</summary>
    private const float FORMATION_SPACING = 1.2f;

    /// <summary>응집 가중치(더 높을수록 몬스터들끼리 뭉치려는 경향)</summary>
    private const float COHESION_WEIGHT = 0.3f;

    /// <summary>정렬 가중치(더 높을수록 같은 방향으로 이동하려는 경향)</summary>
    private const float ALIGNMENT_WEIGHT = 0.5f;

    /// <summary>분리 가중치(더 높을수록 서로 멀어지려는 경향)</summary>
    private const float SEPARATION_WEIGHT = 0.8f;

    /// <summary>대형 감지 반경</summary>
    private const float FORMATION_RADIUS = 5f;

    /// <summary>대형 내 상대적 위치 오프셋</summary>
    private Vector2 formationOffset;

    /// <summary>몬스터의 물리 컴포넌트</summary>
    private Rigidbody2D rb;
    #endregion

    /// <summary>연결된 몬스터 컴포넌트</summary>
    [SerializeField]
    private Monster monster;

    /// <summary>
    /// 경로 탐색기 초기화
    /// </summary>
    /// <param name="monster">몬스터 참조</param>
    /// <param name="rb">물리 컴포넌트</param>
    public void Initialize(Monster monster, Rigidbody2D rb)
    {
        this.monster = monster;
        this.rb = rb;
        this.previousXPosition = transform.position.x;
        this.lastPosition = transform.position;
        this.previousMoveDir = Vector2.zero;

        // 대형 내 위치 계산
        CalculateFormationOffset();

        // 경로 초기화
        currentPath = null;
        lastPathUpdateTime = 0f;
        lastObstacleAvoidanceTime = 0f;
        stuckTimer = 0f;

        // 원형 이동 초기화
        isCirclingPlayer = false;
        circlingRadius = 3f;
        circlingAngle = Random.Range(0f, 360f);
    }

    public void OnValidate()
    {
        monster = GetComponent<Monster>();
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

        if (distanceToTarget > monster.stat.GetStat(StatType.AttackRange))
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
        if (monster.isStunned || monster.stat.GetStat(StatType.MoveSpeed) <= 0)
            return;

        if (!IsInValidPosition())
            return;

        Vector2 targetPosition = CalculateTargetPosition();
        float distanceToTarget = Vector2.Distance(targetPosition, monster.Target.position);

        if (distanceToTarget < monster.stat.GetStat(StatType.AttackRange))
        {
            if (HandleCirclingBehavior(distanceToTarget))
                return;
        }

        MoveToPosition(targetPosition);
    }

    private bool IsInValidPosition()
    {
        // 현재 위치가 유효한지 확인
        Node currentNode = GameManager.Instance.PathFindingSystem.GetNodeFromWorldPosition(
            transform.position
        );
        if (currentNode != null && !currentNode.walkable)
        {
            Vector2 safePosition = FindNearestSafePosition(transform.position);
            rb.MovePosition(
                (Vector2)transform.position
                    + safePosition * monster.stat.GetStat(StatType.MoveSpeed) * 2f * Time.deltaTime
            );
            return false;
        }

        // 그리드 내에 있는지 확인
        if (!GameManager.Instance.PathFindingSystem.IsPositionInGrid(transform.position))
        {
            Vector2 clampedPosition = GameManager.Instance.PathFindingSystem.ClampToGrid(
                transform.position
            );
            rb.MovePosition(clampedPosition);
            return false;
        }

        return true;
    }

    private Vector2 CalculateTargetPosition()
    {
        // 몬스터가 타겟을 향해 이동할 위치 계산
        return (Vector2)monster.Target.position
            - ((Vector2)monster.Target.position - (Vector2)transform.position).normalized
                * monster.preferredDistance;
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
                if (IsPathValid(newPath))
                {
                    currentPath = newPath;
                    lastPathUpdateTime = Time.time;
                    stuckTimer = 0f;
                }
                else
                {
                    FindAlternativePath(targetPosition);
                }
            }
        }

        FollowPath();
    }

    private bool ShouldUpdatePath()
    {
        // 경로가 없거나 비어있을 경우
        if (currentPath == null || currentPath.Count == 0)
            return true;

        // 경로 업데이트 주기가 지났는지 확인
        if (Time.time >= lastPathUpdateTime + pathUpdateTime)
        {
            // 현재 경로의 최종 목적지와 타겟의 현재 위치 간의 거리가 충분히 먼 경우
            Vector2 finalDestination = currentPath[currentPath.Count - 1];
            float distanceToFinalDestination = Vector2.Distance(
                finalDestination,
                monster.Target.position
            );
            return distanceToFinalDestination > PathFindingSystem.NODE_SIZE * 2;
        }
        return false;
    }

    private bool IsPathValid(List<Vector2> path)
    {
        foreach (Vector2 pathPoint in path)
        {
            Node node = GameManager.Instance.PathFindingSystem.GetNodeFromWorldPosition(pathPoint);
            if (node != null && !node.walkable)
            {
                return false;
            }
        }
        return true;
    }

    private void FindAlternativePath(Vector2 targetPosition)
    {
        Vector2 safePosition = FindSafePosition(targetPosition);
        currentPath = GameManager.Instance.PathFindingSystem.FindPath(
            transform.position,
            safePosition
        );
    }

    private Vector2 FindNearestSafePosition(Vector2 currentPosition)
    {
        float checkRadius = 1f;
        int maxAttempts = 8;
        float angleStep = 360f / maxAttempts;
        int maxLayers = 3; // 최대 확장 레이어 수

        // 여러 레이어에 걸쳐 확장하며 검색
        for (int layer = 1; layer <= maxLayers; layer++)
        {
            float currentCheckRadius = checkRadius * layer;

            for (int i = 0; i < maxAttempts; i++)
            {
                float angle = i * angleStep;
                float radian = angle * Mathf.Deg2Rad;
                Vector2 checkDirection = new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
                Vector2 checkPosition = currentPosition + checkDirection * currentCheckRadius;

                Node node = GameManager.Instance.PathFindingSystem.GetNodeFromWorldPosition(
                    checkPosition
                );
                if (node != null && node.walkable)
                {
                    return checkDirection; // 안전한 방향 반환
                }
            }
        }

        // 안전한 위치를 찾지 못한 경우 정반대 방향 반환
        Vector2 directionToTarget = ((Vector2)monster.Target.position - currentPosition).normalized;
        return -directionToTarget;
    }

    private void FollowPath()
    {
        if (currentPath == null || currentPath.Count == 0)
            return;

        Vector2 currentPos = transform.position;
        Vector2 nextWaypoint = currentPath[0];

        HandleStuckCheck(currentPos);

        if (HasReachedWaypoint(currentPos, nextWaypoint))
        {
            UpdateWaypoint();
            if (currentPath == null || currentPath.Count == 0)
            {
                MoveDirectlyTowardsTarget();
                return;
            }
            nextWaypoint = currentPath[0];
        }

        ApplyMovement(currentPos, nextWaypoint);
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

    private void ApplyMovement(Vector2 currentPos, Vector2 nextWaypoint)
    {
        Vector2 moveDirection = CalculateAvoidanceDirection(currentPos, nextWaypoint);
        Vector2 separationForce = CalculateSeparationForce(currentPos);

        // 분리력을 가중치를 적용하여 이동 방향에 결합
        moveDirection = (moveDirection + separationForce * 0.2f).normalized;

        // 최종 속도 계산
        Vector2 targetVelocity = moveDirection * monster.stat.GetStat(StatType.MoveSpeed);

        // 현재 속도에서 목표 속도로 부드럽게 전환
        rb.velocity = Vector2.Lerp(rb.velocity, targetVelocity, Time.deltaTime * 5f);
    }

    private Vector2 CalculateSeparationForce(Vector2 currentPos)
    {
        Vector2 separationForce = Vector2.zero;
        // 원형 이동 시 더 작은 분리 반경 사용
        float separationRadius = isCirclingPlayer ? 0.8f : 1.2f;

        // 주변 몬스터 탐색
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(
            currentPos,
            separationRadius,
            LayerMask.GetMask("Enemy")
        );

        foreach (Collider2D enemyCollider in nearbyEnemies)
        {
            if (enemyCollider.gameObject != gameObject)
            {
                // 다른 몬스터로부터의 방향 및 거리 계산
                Vector2 diff = currentPos - (Vector2)enemyCollider.transform.position;
                float distance = diff.magnitude;

                if (distance < separationRadius)
                {
                    // 원형 이동 시 분리력 감소
                    float strength = isCirclingPlayer ? 0.5f : 1f;
                    // 거리가 가까울수록 더 강한 분리력 적용
                    separationForce +=
                        diff.normalized * (1 - distance / separationRadius) * strength;
                }
            }
        }

        // 원형 이동 시 분리력 가중치 감소
        return separationForce.normalized * (isCirclingPlayer ? 0.3f : 0.5f);
    }

    private Vector2 CalculateAvoidanceDirection(Vector2 currentPosition, Vector2 targetPosition)
    {
        Vector2 moveDir = (targetPosition - currentPosition).normalized;

        // 특수 케이스 처리: 수직/수평 정렬 시 직접 충돌 방지
        if (ShouldAvoidDirectAlignment(currentPosition, moveDir))
        {
            Vector2 alternativeDir = FindAlternativeDirection(currentPosition);
            if (alternativeDir != Vector2.zero)
            {
                return alternativeDir;
            }
        }

        // 장애물 검사
        var obstacles = CheckObstacles(currentPosition, moveDir);
        if (HasObstacles(obstacles))
        {
            HandleObstacleAvoidance(obstacles);
            moveDir = CalculateAvoidanceVector(obstacles);
        }

        return SmoothDirection(moveDir);
    }

    private bool ShouldAvoidDirectAlignment(Vector2 currentPosition, Vector2 moveDir)
    {
        Vector2 dirToTarget = (Vector2)monster.Target.position - currentPosition;
        bool isVerticalAligned = Mathf.Abs(dirToTarget.x) < 0.1f;
        bool isHorizontalAligned = Mathf.Abs(dirToTarget.y) < 0.1f;

        return (isVerticalAligned || isHorizontalAligned)
            && Physics2D.Raycast(
                currentPosition,
                moveDir,
                WALL_AVOIDANCE_DISTANCE,
                LayerMask.GetMask("Obstacle")
            );
    }

    private Vector2 FindAlternativeDirection(Vector2 currentPosition)
    {
        Vector2 dirToTarget = (Vector2)monster.Target.position - currentPosition;
        bool isVerticalAligned = Mathf.Abs(dirToTarget.x) < 0.1f;

        Vector2 alternativeDir = isVerticalAligned ? new Vector2(1f, 0f) : new Vector2(0f, 1f);

        // 대체 방향 찾기
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

        // 반대 방향 확인
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

        return Vector2.zero;
    }

    private (RaycastHit2D front, RaycastHit2D right, RaycastHit2D left) CheckObstacles(
        Vector2 position,
        Vector2 direction
    )
    {
        // 정면 및 각도별 레이캐스트 확인
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
        // 장애물 감지 시 경로 재계산 필요
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

        // 장애물 위치에 따른 회피 방향 계산
        if (obstacles.front.collider != null)
        {
            avoidDir += -obstacles.front.normal * 3f; // 정면 장애물은 더 강한 회피력
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
        // 이동 방향을 부드럽게 전환
        if (previousMoveDir != Vector2.zero)
        {
            finalMoveDir = Vector2.Lerp(previousMoveDir, finalMoveDir, Time.deltaTime * 20f);
        }
        previousMoveDir = finalMoveDir;
        return finalMoveDir;
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
        if (monster.Target == null)
            return;

        UpdateCirclingParameters();
        Vector2 targetPosition = CalculateCirclingPosition();

        // 타겟 위치가 장애물 내부인지 확인
        Node targetNode = GameManager.Instance.PathFindingSystem.GetNodeFromWorldPosition(
            targetPosition
        );
        if (targetNode != null && !targetNode.walkable)
        {
            targetPosition = FindSafeCirclingPosition(targetPosition);
        }

        ApplyCirclingMovement(targetPosition);
    }

    private Vector2 CalculateCirclingPosition()
    {
        Vector2 offset =
            new Vector2(
                Mathf.Cos(circlingAngle * Mathf.Deg2Rad),
                Mathf.Sin(circlingAngle * Mathf.Deg2Rad)
            ) * circlingRadius;

        return (Vector2)monster.Target.position + offset;
    }

    private Vector2 FindSafeCirclingPosition(Vector2 originalPosition)
    {
        // 다양한 각도에서 안전한 위치 검색
        float[] checkAngles = { 45f, -45f, 90f, -90f, 135f, -135f, 180f };

        foreach (float angleOffset in checkAngles)
        {
            float newAngle = circlingAngle + angleOffset;
            Vector2 checkPosition =
                (Vector2)monster.Target.position
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
        // 몬스터 수에 따라 원 반경 조정
        int enemyCount = GameManager.Instance.Monsters.Count;
        circlingRadius = Mathf.Max(2.0f, Mathf.Min(3.0f, enemyCount * 0.5f));

        // 시간 기반 기본 각도
        float baseAngle = Time.time * 20f;

        // 각 몬스터마다 고유한 위치 할당
        int myIndex = GameManager.Instance.Monsters.IndexOf(monster);
        float angleStep = 360f / Mathf.Max(1, enemyCount);
        float targetAngle = baseAngle + (myIndex * angleStep);

        // 각도를 부드럽게 변경
        circlingAngle = Mathf.LerpAngle(circlingAngle, targetAngle, Time.deltaTime * 5f);
    }

    private void ApplyCirclingMovement(Vector2 targetPosition)
    {
        // 회피 방향 계산
        Vector2 moveDirection = CalculateAvoidanceDirection(transform.position, targetPosition);

        // 분리력 계산 - 원형 이동 시 낮은 가중치 적용
        Vector2 separationForce = CalculateSeparationForce(transform.position);
        moveDirection = (moveDirection + separationForce * 0.1f).normalized;

        // 원형 이동 시 약간 더 빠른 속도로 이동 (1.2배)
        Vector2 targetVelocity = moveDirection * monster.stat.GetStat(StatType.MoveSpeed) * 1.2f;
        rb.MovePosition(rb.position + targetVelocity * Time.deltaTime);
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

    private void MoveDirectlyTowardsTarget()
    {
        if (monster.Target == null)
            return;

        Vector2 currentPos = transform.position;
        Vector2 targetPos = GetTargetPosition(monster.Target);

        // 여러 이동 영향력 계산
        Vector2 flockingForce = CalculateFlockingForce(currentPos);
        Vector2 formationPos = (Vector2)monster.Target.position + formationOffset;
        Vector2 formationDir = (formationPos - currentPos).normalized;

        // 기본 이동 방향에 다양한 영향력 결합
        Vector2 moveDir = (
            (targetPos - currentPos).normalized + flockingForce + formationDir
        ).normalized;

        // 장애물 회피 계산
        moveDir = CalculateAvoidanceDirection(currentPos, currentPos + moveDir);

        // 분리 힘 적용
        Vector2 separationForce = CalculateSeparationForce(currentPos);
        moveDir = (moveDir + separationForce * 0.2f).normalized;

        // 최종 속도 계산 및 적용
        Vector2 targetVelocity = moveDir * monster.stat.GetStat(StatType.MoveSpeed);
        rb.MovePosition(rb.position + targetVelocity * Time.deltaTime);
    }

    private Vector2 CalculateFlockingForce(Vector2 currentPos)
    {
        // 군집 행동의 세 가지 규칙에 대한 결과값 초기화
        Vector2 cohesion = Vector2.zero; // 응집: 주변 몬스터들의 중심으로 이동
        Vector2 alignment = Vector2.zero; // 정렬: 주변 몬스터들과 같은 방향으로 이동
        Vector2 separation = Vector2.zero; // 분리: 주변 몬스터들과 충돌 회피
        int neighborCount = 0;

        // 주변 몬스터 탐색
        foreach (Monster otherMonster in GameManager.Instance.Monsters)
        {
            if (otherMonster == monster)
                continue;

            float distance = Vector2.Distance(currentPos, otherMonster.transform.position);
            if (distance < FORMATION_RADIUS)
            {
                // 응집: 이웃 몬스터 위치 합산
                cohesion += (Vector2)otherMonster.transform.position;

                // 정렬: 이웃 몬스터 속도 벡터 합산
                alignment += otherMonster.rb.velocity;

                // 분리: 이웃 몬스터로부터 멀어지는 벡터 계산 (거리에 반비례)
                Vector2 diff = currentPos - (Vector2)otherMonster.transform.position;
                separation += diff.normalized / Mathf.Max(distance, 0.1f);

                neighborCount++;
            }
        }

        // 주변 몬스터가 있을 경우에만 계산
        if (neighborCount > 0)
        {
            // 응집: 이웃 몬스터들의 평균 위치로 향하는 벡터
            cohesion = (cohesion / neighborCount - currentPos) * COHESION_WEIGHT;

            // 정렬: 이웃 몬스터들의 평균 속도 방향
            alignment = (alignment / neighborCount) * ALIGNMENT_WEIGHT;

            // 분리: 이웃 몬스터들로부터 멀어지는 힘
            separation = separation * SEPARATION_WEIGHT;
        }

        // 세 가지 힘을 결합하여 정규화된 최종 군집 힘 반환
        return (cohesion + alignment + separation).normalized;
    }

    #region Gizmos
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || monster == null || monster.Target == null)
            return;

        // 현재 진행 중인 경로 시각화
        if (currentPath != null && currentPath.Count > 0)
        {
            // 현재 위치에서 첫 번째 웨이포인트까지 연결
            Gizmos.color = new Color(0, 1, 0, 0.8f); // 녹색
            Gizmos.DrawLine(transform.position, currentPath[0]);

            // 경로 웨이포인트 연결
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.color = new Color(0, 0.7f, 1f, 0.8f); // 청록색
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);

                // 웨이포인트 표시
                Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.8f); // 주황색
                Gizmos.DrawSphere(currentPath[i], 0.1f);
            }

            // 마지막 웨이포인트 표시
            if (currentPath.Count > 0)
            {
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f); // 빨간색
                Gizmos.DrawSphere(currentPath[currentPath.Count - 1], 0.15f);
            }
        }

        // 목표 지점 표시
        if (monster.Target != null)
        {
            Vector2 targetPos = GetTargetPosition(monster.Target);
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // 빨간색
            Gizmos.DrawWireSphere(targetPos, 0.3f);

            // 선호 거리 원 표시
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f); // 연한 파란색
            Gizmos.DrawWireSphere(monster.Target.position, monster.preferredDistance);
        }

        // 원형 이동 시각화
        if (isCirclingPlayer && monster.Target != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // 노란색
            Gizmos.DrawWireSphere(monster.Target.position, circlingRadius);

            // 현재 원형 이동 각도 표시
            Vector2 directionOffset =
                new Vector2(
                    Mathf.Cos(circlingAngle * Mathf.Deg2Rad),
                    Mathf.Sin(circlingAngle * Mathf.Deg2Rad)
                ) * circlingRadius;

            Vector2 targetCirclePos = (Vector2)monster.Target.position + directionOffset;
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f); // 주황색
            Gizmos.DrawLine(monster.Target.position, targetCirclePos);
            Gizmos.DrawSphere(targetCirclePos, 0.2f);
        }
    }
    #endregion
}
