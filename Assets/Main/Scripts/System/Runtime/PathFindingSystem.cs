using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PathFindingSystem : MonoBehaviour, IInitializable
{
    public Tilemap obstacleTilemap;
    public Tilemap terrainTilemap;
    public bool IsInitialized { get; private set; }

    #region Constants
    private const int MAX_PATH_LENGTH = 100;
    private const int MAX_SEARCH_ITERATIONS = 1000;
    private const int INITIAL_POOL_SIZE = 20;
    #endregion

    #region Fields
    private Dictionary<Vector2Int, Node> activeNodes = new();
    private List<Node> openSet;
    private HashSet<Node> closedSet;
    private Queue<PathFindingInstance> instancePool = new();
    public float nodeSize = 1f;

    #endregion

    private class PathFindingInstance
    {
        public List<Node> openSet = new List<Node>(1000);
        public HashSet<Node> closedSet = new HashSet<Node>();
        public List<Vector2> path = new List<Vector2>(MAX_PATH_LENGTH);
    }

    private void Start()
    {
        Test();
    }

    public void Test()
    {
        InitializePools();

        nodeSize = terrainTilemap.cellSize.x;
        activeNodes.Clear();

        var bounds = terrainTilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                Vector2 worldPosition =
                    terrainTilemap.CellToWorld(cellPosition)
                    + (Vector3)terrainTilemap.cellSize / 2f;
                bool isWalkable = !obstacleTilemap.HasTile(cellPosition);
                activeNodes[new Vector2Int(x, y)] = new Node(isWalkable, worldPosition, x, y);
            }
        }

        IsInitialized = true;
    }

    private void InitializePools()
    {
        for (int i = 0; i < INITIAL_POOL_SIZE; i++)
        {
            instancePool.Enqueue(new PathFindingInstance());
        }
    }

    private PathFindingInstance GetPathFindingInstance()
    {
        if (instancePool.Count == 0)
        {
            return new PathFindingInstance();
        }
        var instance = instancePool.Dequeue();
        instance.openSet.Clear();
        instance.closedSet.Clear();
        instance.path.Clear();
        return instance;
    }

    private void ReturnPathFindingInstance(PathFindingInstance instance)
    {
        instancePool.Enqueue(instance);
    }

    public void Initialize()
    {
        try
        {
            InitializePools();
            IsInitialized = true;
        }
        catch (Exception e)
        {
            Logger.LogError(typeof(PathFindingSystem), $"Error initializing : {e.Message}");
            IsInitialized = false;
        }
    }

    public void InitializeGrid(Tilemap terrainTilemap, Tilemap obstacleTilemap)
    {
        this.terrainTilemap = terrainTilemap;
        this.obstacleTilemap = obstacleTilemap;
        nodeSize = terrainTilemap.cellSize.x;
        activeNodes.Clear();

        var bounds = terrainTilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                Vector2 worldPosition =
                    terrainTilemap.CellToWorld(cellPosition)
                    + (Vector3)terrainTilemap.cellSize / 2f;
                bool isWalkable = !obstacleTilemap.HasTile(cellPosition);
                activeNodes[new Vector2Int(x, y)] = new Node(isWalkable, worldPosition, x, y);
            }
        }
    }

    private Vector2Int WorldToGridPosition(Vector2 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / nodeSize);
        int y = Mathf.FloorToInt(worldPosition.y / nodeSize);
        return new Vector2Int(x, y);
    }

    public Node GetNodeFromWorldPosition(Vector2 worldPosition)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPosition);
        activeNodes.TryGetValue(gridPos, out Node node);
        return node;
    }

    public List<Node> GetNeighbours(Node node)
    {
        var neighbours = new List<Node>(8);
        int[,] directions = new int[,]
        {
            { 1, 0 },
            { -1, 0 },
            { 0, 1 },
            { 0, -1 },
        };
        for (int i = 0; i < 4; i++)
        {
            int dx = directions[i, 0];
            int dy = directions[i, 1];
            Vector2Int checkPos = new Vector2Int(node.gridX + dx, node.gridY + dy);
            if (activeNodes.TryGetValue(checkPos, out Node neighbour))
            {
                neighbours.Add(neighbour);
            }
        }
        return neighbours;
    }

    public List<Vector2> GetPath(Vector2 startPos, Vector2 targetPos, bool recordSearch = false)
    {
        var pathFindingInstance = GetPathFindingInstance();
        try
        {
            var path = FindPath(startPos, targetPos, pathFindingInstance, recordSearch);
            if (path != null && path.Count > 2)
            {
                path = OptimizePath(path);
            }
            return path;
        }
        finally
        {
            ReturnPathFindingInstance(pathFindingInstance);
        }
    }

    private List<Vector2> FindPath(
        Vector2 startPos,
        Vector2 targetPos,
        PathFindingInstance instance,
        bool recordSearch = false
    )
    {
        openSet = instance.openSet;
        closedSet = instance.closedSet;
        if (TryGetDirectPath(startPos, targetPos, out var directPath))
        {
            return directPath;
        }
        var result = FindPath(startPos, targetPos);

        return result;
    }

    private bool TryGetDirectPath(Vector2 startPos, Vector2 targetPos, out List<Vector2> directPath)
    {
        directPath = null;
        float distanceToTarget = Vector2.Distance(startPos, targetPos);
        if (
            distanceToTarget < nodeSize * 2f
            && !Physics2D.Linecast(startPos, targetPos, LayerMask.GetMask("Obstacle"))
        )
        {
            directPath = new List<Vector2> { startPos, targetPos };
            return true;
        }
        return false;
    }

    private bool ValidateNodes(ref Node startNode, ref Node targetNode)
    {
        if (!startNode.walkable || !targetNode.walkable)
        {
            startNode = FindNearestWalkableNode(startNode);
            targetNode = FindNearestWalkableNode(targetNode);
            return startNode != null && targetNode != null;
        }
        return true;
    }

    /// <summary>
    /// 시작/목표 노드의 유효성을 검증하고, 노드 및 자료구조를 초기화한 뒤 경로를 탐색합니다.
    /// </summary>
    /// <param name="startPos">경로 탐색의 시작 위치(월드 좌표)</param>
    /// <param name="targetPos">경로 탐색의 목표 위치(월드 좌표)</param>
    /// <returns>시작점에서 목표점까지의 경로(좌표 리스트), 경로가 없으면 null</returns>
    private List<Vector2> FindPath(Vector2 startPos, Vector2 targetPos)
    {
        // 시작 노드와 목표 노드를 월드좌표에서 노드로 변환
        Node startNode = GetNodeFromWorldPosition(startPos);
        Node targetNode = GetNodeFromWorldPosition(targetPos);
        // 두 노드가 이동 가능한지 검증
        if (!ValidateNodes(ref startNode, ref targetNode))
        {
            // 유효하지 않으면 경로 탐색 중단
            return null;
        }
        // 모든 노드의 비용 초기화 및 시작 노드 세팅
        InitializePathfindingNodes(startNode, targetNode);
        return SearchPath(startNode, targetNode, startPos, targetPos);
    }

    /// <summary>
    /// 모든 노드의 경로 비용 및 이전 노드 정보를 초기화하고, 시작 노드의 g/h/f 비용을 세팅합니다.
    /// </summary>
    /// <param name="startNode">경로 탐색의 시작 노드</param>
    /// <param name="targetNode">경로 탐색의 목표 노드</param>
    public void InitializePathfindingNodes(Node startNode, Node targetNode)
    {
        // 모든 노드의 비용 및 이전 노드 정보 초기화
        foreach (var node in activeNodes.Values)
        {
            node.gCost = float.MaxValue;
            // fCost(g+h) 재계산
            node.CalculateFCost();
            node.previousNode = null;
        }
        // 시작 노드의 비용 세팅
        if (startNode != null)
        {
            // 시작 노드는 비용 0
            startNode.gCost = 0;
            // 휴리스틱(목표까지 예상 비용) 계산
            startNode.hCost = CalculateDistance(startNode, targetNode);
            // fCost 재계산
            startNode.CalculateFCost();
        }
    }

    /// <summary>
    /// 두 노드 간의 맨해튼 거리(대각선 가중치 포함)를 계산합니다.
    /// </summary>
    /// <param name="a">시작 노드</param>
    /// <param name="b">목표 노드</param>
    /// <returns>두 노드 간의 거리(비용)</returns>
    private float CalculateDistance(Node a, Node b)
    {
        float dx = Mathf.Abs(a.gridX - b.gridX); // x축 거리
        float dy = Mathf.Abs(a.gridY - b.gridY); // y축 거리
        // 맨해튼 거리 + 대각선 이동 가중치
        return dx + dy;
    }

    /// <summary>
    /// f값이 가장 낮은 노드를 반복적으로 선택하며, 목표에 도달할 때까지 이웃 노드를 탐색하고 비용을 갱신합니다.
    /// </summary>
    /// <returns>시작점에서 목표점까지의 경로(좌표 리스트), 경로가 없으면 start~target 직선 반환</returns>
    private List<Vector2> SearchPath(
        Node startNode,
        Node targetNode,
        Vector2 startPos,
        Vector2 targetPos
    )
    {
        int iterations = 0;
        openSet.Add(startNode);
        while (openSet.Count > 0 && iterations < MAX_SEARCH_ITERATIONS)
        {
            iterations++;
            Node currentNode = GetLowestFCostNode(openSet);
            if (currentNode == targetNode)
            {
                var path = CalculatePath(targetNode);
                if (path != null && path.Count > 0)
                {
                    return path;
                }
                return null;
            }
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);
            foreach (Node neighbour in GetNeighbours(currentNode))
            {
                if (closedSet.Contains(neighbour))
                    continue;
                if (!neighbour.walkable)
                {
                    closedSet.Add(neighbour);
                    continue;
                }
                float tempGCost = currentNode.gCost + 1f;
                if (tempGCost < neighbour.gCost)
                {
                    neighbour.previousNode = currentNode;
                    neighbour.gCost = tempGCost;
                    neighbour.hCost = CalculateDistance(neighbour, targetNode);
                    neighbour.CalculateFCost();
                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }
                }
            }
        }
        return new List<Vector2> { startPos, targetPos };
    }

    /// <summary>
    /// 경로 상의 불필요한 노드를 제거하여 경로를 단순화(최적화)합니다.
    /// </summary>
    /// <returns>최적화된 경로(좌표 리스트)</returns>
    private List<Vector2> OptimizePath(List<Vector2> path)
    {
        if (path.Count <= 2)
            return path;
        // 최적화된 경로 리스트에 시작점 추가
        var optimizedPath = new List<Vector2>(50) { path[0] };
        int i = 0;
        // 경로를 따라 불필요한 노드(직선상 장애물 없는 구간) 제거
        while (i < path.Count - 2)
        {
            i = FindFurthestVisibleNode(path, i, optimizedPath);
        }
        // 마지막 노드가 누락되지 않도록 추가
        if (i != path.Count - 1)
        {
            optimizedPath.Add(path[path.Count - 1]);
        }
        path.Clear(); // 기존 경로 비움
        return optimizedPath;
    }

    /// <summary>
    /// 현재 위치에서 장애물 없이 직선으로 도달 가능한 가장 먼 노드의 인덱스를 반환하고, 그 노드를 최적화 경로에 추가합니다.
    /// </summary>
    /// <returns>가장 멀리 도달 가능한 노드의 인덱스</returns>
    private int FindFurthestVisibleNode(
        List<Vector2> path,
        int currentIndex,
        List<Vector2> optimizedPath
    )
    {
        Vector2 current = path[currentIndex];
        int furthestVisible = currentIndex + 1;
        // 현재 위치에서 직선으로 도달 가능한 가장 먼 노드 찾기
        for (int j = currentIndex + 2; j < path.Count; j++)
        {
            if (IsNodeVisible(current, path[j]))
            {
                furthestVisible = j;
            }
            else
                break;
        }
        optimizedPath.Add(path[furthestVisible]); // 최적화 경로에 추가
        return furthestVisible;
    }

    /// <summary>
    /// 두 지점 사이에 장애물이 없는지(Linecast) 확인하여, 직선 이동이 가능한지 판정합니다.
    /// </summary>
    /// <returns>장애물 없이 이동 가능하면 true, 아니면 false</returns>
    public bool IsNodeVisible(Vector2 from, Vector2 to)
    {
        // 두 지점 사이에 장애물이 있는지 확인
        bool hasObstacle = Physics2D.Linecast(from, to, LayerMask.GetMask("Obstacle"));
        // 장애물이 없고, 경로 폭도 충분히 확보되면 true
        return !hasObstacle && CheckPathClearance(from, to);
    }

    private Node GetLowestFCostNode(List<Node> nodeList)
    {
        Node lowestFCostNode = nodeList[0];
        for (int i = 1; i < nodeList.Count; i++)
        {
            if (
                nodeList[i].fCost < lowestFCostNode.fCost
                || (
                    Mathf.Approximately(nodeList[i].fCost, lowestFCostNode.fCost)
                    && nodeList[i].hCost < lowestFCostNode.hCost
                )
            )
            {
                lowestFCostNode = nodeList[i];
            }
        }
        return lowestFCostNode;
    }

    private List<Vector2> CalculatePath(Node endNode)
    {
        List<Vector2> path = new List<Vector2>(MAX_PATH_LENGTH);
        Node currentNode = endNode;
        int pathLength = 0;
        while (currentNode != null && pathLength < MAX_PATH_LENGTH)
        {
            path.Add(currentNode.worldPosition);
            currentNode = currentNode.previousNode;
            pathLength++;
        }
        path.Reverse();
        return path;
    }

    public Node FindNearestWalkableNode(Node node)
    {
        if (node.walkable)
            return node;
        Queue<Node> openNodes = new Queue<Node>();
        HashSet<Node> visitedNodes = new HashSet<Node>();
        openNodes.Enqueue(node);
        visitedNodes.Add(node);
        while (openNodes.Count > 0)
        {
            Node currentNode = openNodes.Dequeue();
            foreach (Node neighbor in GetNeighbours(currentNode))
            {
                if (!visitedNodes.Contains(neighbor))
                {
                    if (neighbor.walkable)
                    {
                        return neighbor;
                    }
                    openNodes.Enqueue(neighbor);
                    visitedNodes.Add(neighbor);
                }
            }
        }
        return null;
    }

    public bool CheckPathClearance(Vector2 start, Vector2 end)
    {
        Vector2 direction = (end - start).normalized;
        Vector2 perpendicular = Vector2.Perpendicular(direction) * (nodeSize * 0.3f);
        bool leftClear = !Physics2D.Linecast(
            start + perpendicular,
            end + perpendicular,
            LayerMask.GetMask("Obstacle")
        );
        bool rightClear = !Physics2D.Linecast(
            start - perpendicular,
            end - perpendicular,
            LayerMask.GetMask("Obstacle")
        );
        return leftClear && rightClear;
    }

    private void OnDrawGizmos()
    {
        if (activeNodes == null || activeNodes.Count == 0)
            return;
        var bounds =
            obstacleTilemap != null ? obstacleTilemap.cellBounds : new BoundsInt(0, 0, 0, 1, 1, 1);
        Vector3Int cellCenter = new Vector3Int(
            bounds.x + bounds.size.x / 2,
            bounds.y + bounds.size.y / 2,
            0
        );
        Vector3 center =
            obstacleTilemap != null
                ? obstacleTilemap.CellToWorld(cellCenter) + (Vector3)obstacleTilemap.cellSize / 2f
                : Vector3.zero;
        Vector3 size =
            obstacleTilemap != null
                ? new Vector3(bounds.size.x * nodeSize, bounds.size.y * nodeSize, 1)
                : Vector3.one;
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        Gizmos.DrawWireCube(center, size);
        foreach (var node in activeNodes.Values)
        {
            Gizmos.color = node.walkable ? new Color(1, 1, 1, 0.1f) : new Color(1, 0, 0, 0.2f);
            Gizmos.DrawCube(node.worldPosition, Vector3.one * nodeSize * 0.8f);
        }

        if (GameManager.Instance?.Monsters == null)
            return;
        foreach (var monster in GameManager.Instance.Monsters)
        {
            if (
                monster?.pathFinder?.currentPath != null
                && monster.pathFinder.currentPath.Count > 0
            )
            {
                var path = monster.pathFinder.currentPath;
                // 최종 경로: 초록색
                Gizmos.color = new Color(0, 1, 0, 1f);
                for (int i = 0; i < path.Count - 1; i++)
                {
                    Gizmos.DrawLine(path[i], path[i + 1]);
                    Gizmos.DrawWireSphere(path[i], nodeSize * 0.3f);
                }
                if (path.Count > 0)
                {
                    Gizmos.DrawWireSphere(path[path.Count - 1], nodeSize * 0.4f);
                }
            }
        }
    }

    public void ResetRuntimeData()
    {
        activeNodes.Clear();
        openSet?.Clear();
        closedSet?.Clear();
        instancePool.Clear();
        obstacleTilemap = null;
    }
}
