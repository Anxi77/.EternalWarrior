using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PathFindingSystem : MonoBehaviour, IInitializable
{
    public Tilemap obstacleTilemap;
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

    private List<Node> GetNeighbours(Node node)
    {
        var neighbours = new List<Node>(4);
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

    public List<Vector2> FindPath(Vector2 startPos, Vector2 targetPos)
    {
        var pathFindingInstance = GetPathFindingInstance();
        try
        {
            var path = ExecutePathfinding(startPos, targetPos, pathFindingInstance);
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

    private List<Vector2> ExecutePathfinding(
        Vector2 startPos,
        Vector2 targetPos,
        PathFindingInstance instance
    )
    {
        openSet = instance.openSet;
        closedSet = instance.closedSet;
        if (TryGetDirectPath(startPos, targetPos, out var directPath))
        {
            return directPath;
        }
        return PerformAStarPathfinding(startPos, targetPos);
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

    private List<Vector2> PerformAStarPathfinding(Vector2 startPos, Vector2 targetPos)
    {
        Node startNode = GetNodeFromWorldPosition(startPos);
        Node targetNode = GetNodeFromWorldPosition(targetPos);
        if (!ValidateNodes(ref startNode, ref targetNode))
        {
            return null;
        }
        InitializePathfindingNodes(startNode, targetNode);
        return ExecuteAStarAlgorithm(startNode, targetNode, startPos, targetPos);
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

    private void InitializePathfindingNodes(Node startNode, Node targetNode)
    {
        foreach (var node in activeNodes.Values)
        {
            node.gCost = float.MaxValue;
            node.CalculateFCost();
            node.previousNode = null;
        }
        if (startNode != null)
        {
            startNode.gCost = 0;
            startNode.hCost = CalculateDistance(startNode, targetNode);
            startNode.CalculateFCost();
        }
    }

    private List<Vector2> ExecuteAStarAlgorithm(
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
                float tentativeGCost =
                    currentNode.gCost + CalculateDistance(currentNode, neighbour);
                if (tentativeGCost < neighbour.gCost)
                {
                    neighbour.previousNode = currentNode;
                    neighbour.gCost = tentativeGCost;
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

    private List<Vector2> OptimizePath(List<Vector2> path)
    {
        if (path.Count <= 2)
            return path;
        var optimizedPath = new List<Vector2>(50) { path[0] };
        int i = 0;
        while (i < path.Count - 2)
        {
            i = FindFurthestVisibleNode(path, i, optimizedPath);
        }
        if (i != path.Count - 1)
        {
            optimizedPath.Add(path[path.Count - 1]);
        }
        path.Clear();
        return optimizedPath;
    }

    private int FindFurthestVisibleNode(
        List<Vector2> path,
        int currentIndex,
        List<Vector2> optimizedPath
    )
    {
        Vector2 current = path[currentIndex];
        int furthestVisible = currentIndex + 1;
        for (int j = currentIndex + 2; j < path.Count; j++)
        {
            if (IsNodeVisible(current, path[j]))
            {
                furthestVisible = j;
            }
            else
                break;
        }
        optimizedPath.Add(path[furthestVisible]);
        return furthestVisible;
    }

    private bool IsNodeVisible(Vector2 from, Vector2 to)
    {
        bool hasObstacle = Physics2D.Linecast(from, to, LayerMask.GetMask("Obstacle"));
        return !hasObstacle && CheckPathClearance(from, to);
    }

    private float CalculateDistance(Node a, Node b)
    {
        float dx = Mathf.Abs(a.gridX - b.gridX);
        float dy = Mathf.Abs(a.gridY - b.gridY);
        return (dx + dy) + (1.4f - 2) * Mathf.Min(dx, dy);
    }

    private Node GetLowestFCostNode(List<Node> nodeList)
    {
        Node lowestFCostNode = nodeList[0];
        for (int i = 1; i < nodeList.Count; i++)
        {
            if (nodeList[i].fCost < lowestFCostNode.fCost)
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
        if (pathLength >= MAX_PATH_LENGTH)
        {
            path = path.GetRange(0, MAX_PATH_LENGTH);
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

    private bool CheckPathClearance(Vector2 start, Vector2 end)
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
            Gizmos.DrawCube(node.worldPosition, Vector3.one * nodeSize * 1f);
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
                Gizmos.color = new Color(1, 0, 1, 1f);
                Gizmos.DrawLine(monster.transform.position, path[0]);
                for (int i = 0; i < path.Count - 1; i++)
                {
                    Gizmos.color = new Color(0, 0, 1, 1f);
                    Gizmos.DrawLine(path[i], path[i + 1]);
                    Gizmos.color = new Color(1, 1, 0, 1f);
                    Gizmos.DrawWireSphere(path[i], nodeSize * 0.3f);
                }
                if (path.Count > 0)
                {
                    Gizmos.color = new Color(0, 1, 0, 1f);
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
