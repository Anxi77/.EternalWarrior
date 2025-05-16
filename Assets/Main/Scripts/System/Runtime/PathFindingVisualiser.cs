using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PathfindingVisualizer : MonoBehaviour
{
    public Vector2 startPos;
    public Vector2 targetPos;
    public PathFindingSystem pathFindingSystem;

    private Node currentNode;
    private Queue<Node> searchQueue = new Queue<Node>();
    private List<Node> visitedNodes = new List<Node>();
    private List<List<Node>> openSetHistory = new List<List<Node>>();
    private int currentStep = 0;
    private bool isSearching = false;

    private bool isSimplifying = false;
    private List<Vector2> rawPath = new List<Vector2>();
    private List<Vector2> optimizedPath = new List<Vector2>();
    private int optimizeStep = 0;
    private bool isPathDrawn = false;
    private bool showRawPath = false;
    private List<Vector2> blockedPoints = new List<Vector2>();
    private List<(Vector2 from, Vector2 to)> blockedRays = new List<(Vector2, Vector2)>();
    private List<(Vector2 from, Vector2 to)> blockedClearanceRays = new List<(Vector2, Vector2)>();

    private int simplifyRayTryIndex = 0;
    private int simplifyRayFromIndex = 0;
    private bool simplifyRayStepActive = false;
    private Vector2? lastSimplifyRayFrom = null;
    private Vector2? lastSimplifyRayTo = null;
    private bool lastSimplifyRayHit = false;
    private Vector2? lastSimplifyRayHitPoint = null;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => pathFindingSystem.IsInitialized);
        pathFindingSystem.GetPath(startPos, targetPos, true);
        currentStep = 0;
        isSearching = true;
        isPathDrawn = false;
        showRawPath = false;
        optimizedPath = new List<Vector2>();
        optimizeStep = 0;
        blockedPoints.Clear();
        visitedNodes.Clear();
        blockedRays.Clear();
        blockedClearanceRays.Clear();
        simplifyRayTryIndex = 0;
        simplifyRayFromIndex = 0;
        simplifyRayStepActive = false;
        lastSimplifyRayFrom = null;
        lastSimplifyRayTo = null;
        lastSimplifyRayHit = false;
        lastSimplifyRayHitPoint = null;
    }

    void Update()
    {
        if (isSearching && Input.GetKeyDown(KeyCode.Space))
        {
            if (searchQueue.Count > 0)
            {
                currentNode = searchQueue.Dequeue();
                visitedNodes.Add(currentNode);
                currentStep++;
            }
            else
            {
                isSearching = false;
                isSimplifying = true;
                showRawPath = true;
                isPathDrawn = true;
                optimizedPath = new List<Vector2>();
                optimizeStep = 0;
                if (rawPath != null && rawPath.Count > 0)
                    optimizedPath.Add(rawPath[0]);
                simplifyRayTryIndex = 0;
                simplifyRayFromIndex = 0;
                simplifyRayStepActive = false;
            }
        }
        else if (isSimplifying && Input.GetKeyDown(KeyCode.Space))
        {
            StepOptimizePathWithRayStep();
        }
    }

    void StepOptimizePathWithRayStep()
    {
        if (rawPath == null || optimizedPath == null || optimizeStep >= rawPath.Count - 2)
        {
            isSimplifying = false;
            return;
        }
        if (!simplifyRayStepActive)
        {
            simplifyRayFromIndex = optimizeStep;
            simplifyRayTryIndex = optimizeStep + 2;
            simplifyRayStepActive = true;
        }
        if (simplifyRayTryIndex >= rawPath.Count)
        {
            optimizedPath.Add(rawPath[rawPath.Count - 1]);
            optimizeStep = rawPath.Count - 1;
            isSimplifying = false;
            simplifyRayStepActive = false;
            return;
        }
        Vector2 from = rawPath[simplifyRayFromIndex];
        Vector2 to = rawPath[simplifyRayTryIndex];
        Vector2? hitPoint;
        bool visible = IsNodeVisible(from, to, out hitPoint);
        lastSimplifyRayFrom = from;
        lastSimplifyRayTo = to;
        lastSimplifyRayHit = !visible;
        lastSimplifyRayHitPoint = hitPoint;
        if (visible)
        {
            simplifyRayTryIndex++;
        }
        else
        {
            if (hitPoint.HasValue)
                blockedPoints.Add(hitPoint.Value);
            else
            {
                var nonWalkableNode = FindFirstNonWalkableNodeBetween(from, to);
                if (nonWalkableNode != null)
                    blockedPoints.Add(nonWalkableNode.worldPosition);
            }
            blockedRays.Add((from, to));
            optimizedPath.Add(rawPath[simplifyRayTryIndex - 1]);
            optimizeStep = simplifyRayTryIndex - 1;
            simplifyRayStepActive = false;
        }
    }

    private Node FindFirstNonWalkableNodeBetween(Vector2 from, Vector2 to)
    {
        if (pathFindingSystem == null)
            return null;
        int sampleCount = Mathf.CeilToInt(Vector2.Distance(from, to) / 0.1f);
        for (int i = 0; i <= sampleCount; i++)
        {
            Vector2 pos = Vector2.Lerp(from, to, i / (float)sampleCount);
            Node node = pathFindingSystem.GetNodeFromWorldPosition(pos);
            if (node != null && !node.walkable)
                return node;
        }
        return null;
    }

    private bool IsNodeVisible(Vector2 from, Vector2 to, out Vector2? hitPoint)
    {
        RaycastHit2D hit = Physics2D.Linecast(from, to, LayerMask.GetMask("Obstacle"));
        hitPoint = hit ? (Vector2?)hit.point : null;
        Vector2 direction = (to - from).normalized;
        Vector2 perpendicular =
            Vector2.Perpendicular(direction) * (pathFindingSystem.nodeSize * 0.3f);
        Vector2 leftFrom = from + perpendicular;
        Vector2 leftTo = to + perpendicular;
        Vector2 rightFrom = from - perpendicular;
        Vector2 rightTo = to - perpendicular;
        bool leftHit = Physics2D.Linecast(leftFrom, leftTo, LayerMask.GetMask("Obstacle"));
        bool rightHit = Physics2D.Linecast(rightFrom, rightTo, LayerMask.GetMask("Obstacle"));
        if (leftHit)
            blockedClearanceRays.Add((leftFrom, leftTo));
        if (rightHit)
            blockedClearanceRays.Add((rightFrom, rightTo));
        bool clearance = !leftHit && !rightHit;
        Debug.Log(
            $"[Linecast] from: {from}, to: {to}, hit: {hit.collider}, hitPoint: {hitPoint}, clearance: {clearance}"
        );
        return !hit && clearance;
    }

    void OnDrawGizmos()
    {
        if (isSearching && openSetHistory != null && openSetHistory.Count > currentStep)
        {
            Gizmos.color = Color.blue;
            foreach (var node in openSetHistory[currentStep])
            {
                Gizmos.DrawSphere(node.worldPosition, 0.13f);
#if UNITY_EDITOR
                Handles.Label(
                    (Vector3)node.worldPosition + Vector3.right * 0.2f,
                    $"g:{(node.gCost == float.MaxValue ? "∞" : Mathf.CeilToInt(node.gCost).ToString())}\nf:{(node.fCost == float.MaxValue ? "∞" : Mathf.CeilToInt(node.fCost).ToString())}"
                );
#endif
            }
        }
        if (visitedNodes != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f);
            foreach (var node in visitedNodes)
            {
                Gizmos.DrawSphere(node.worldPosition, 0.16f);
#if UNITY_EDITOR
                Handles.Label(
                    (Vector3)node.worldPosition + Vector3.right * 0.2f,
                    $"g:{(node.gCost == float.MaxValue ? "∞" : Mathf.CeilToInt(node.gCost).ToString())}\nf:{(node.fCost == float.MaxValue ? "∞" : Mathf.CeilToInt(node.fCost).ToString())}"
                );
#endif
            }
        }
        if (currentNode != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(currentNode.worldPosition, 0.18f);
        }
        if (showRawPath && rawPath != null && rawPath.Count > 1)
        {
            Gizmos.color = Color.magenta;
            for (int i = 0; i < rawPath.Count - 1; i++)
            {
                Gizmos.DrawLine(rawPath[i], rawPath[i + 1]);
            }
        }
        if (blockedRays != null)
        {
            Gizmos.color = Color.red;
            foreach (var ray in blockedRays)
            {
                Gizmos.DrawLine(ray.from, ray.to);
            }
        }
        if (blockedClearanceRays != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var ray in blockedClearanceRays)
            {
                Gizmos.DrawLine(ray.from, ray.to);
            }
        }
        if (isSimplifying && lastSimplifyRayFrom.HasValue && lastSimplifyRayTo.HasValue)
        {
            Gizmos.color = lastSimplifyRayHit ? Color.red : Color.cyan;
            Gizmos.DrawLine(lastSimplifyRayFrom.Value, lastSimplifyRayTo.Value);
        }
        if (isSimplifying && lastSimplifyRayHit && lastSimplifyRayHitPoint.HasValue)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(lastSimplifyRayHitPoint.Value, 0.18f);
        }
        if (isPathDrawn && optimizedPath != null && optimizedPath.Count > 1)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < optimizedPath.Count - 1; i++)
            {
                Gizmos.DrawLine(optimizedPath[i], optimizedPath[i + 1]);
            }
        }
        if (blockedPoints != null)
        {
            Gizmos.color = Color.red;
            foreach (var pt in blockedPoints)
            {
                Gizmos.DrawSphere(pt, 0.18f);
            }
        }
    }
}
