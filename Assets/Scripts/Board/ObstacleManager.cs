using System.Collections.Generic;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    public static ObstacleManager Instance { get; private set; }

    [SerializeField] private GameObject _obstaclePrefab;
    [SerializeField] private float _minPassageWidth = 0.6f;

    public float MinPassageWidth => _minPassageWidth;

    private void Awake()
    {
        Instance = this;
    }

    public bool CanPlaceObstacle(Vector2Int gridPos, bool isOurBoard)
    {
        GridManager gm = GridManager.Instance;
        if (gm == null) return false;
        if (!gm.IsInBounds(gridPos)) return false;

        var cell = gm.GetCell(gridPos, isOurBoard);
        if (cell.IsOccupied) return false;

        // Temporarily mark as occupied, check path connectivity
        cell.IsOccupied = true;

        bool pathExists = CheckPathExists(isOurBoard);

        cell.IsOccupied = false;
        return pathExists;
    }

    public Obstacle PlaceObstacle(Vector2Int gridPos, bool isOurBoard)
    {
        if (!CanPlaceObstacle(gridPos, isOurBoard)) return null;

        GameObject obj;
        if (_obstaclePrefab != null)
            obj = Instantiate(_obstaclePrefab, transform);
        else
            obj = new GameObject("Obstacle", typeof(Obstacle));

        obj.transform.parent = transform;
        Obstacle obs = obj.GetComponent<Obstacle>();
        obs.Initialize(gridPos, isOurBoard);
        return obs;
    }

    public bool CheckPathExists(bool isOurBoard)
    {
        GridManager gm = GridManager.Instance;
        var baseObj = FindBase(isOurBoard);
        if (baseObj == null) return true;

        Vector2Int start = baseObj.GridPosition;
        Vector2Int portalPos = GetTargetPortalGrid(isOurBoard);
        if (portalPos.x < 0) return true; // No portal found, skip check

        return BFSPathExists(start, portalPos, isOurBoard, gm);
    }

    private Base FindBase(bool isOurBoard)
    {
        var bases = FindObjectsOfType<Base>();
        foreach (var b in bases)
            if (b.IsOurBase == isOurBoard) return b;
        return null;
    }

    private Vector2Int GetTargetPortalGrid(bool isOurBoard)
    {
        Portal ourPortal = PortalManager.Instance?.GetPortalOnBoard(PortalType.Our, isOurBoard);
        Portal enemyPortal = PortalManager.Instance?.GetPortalOnBoard(PortalType.Enemy, isOurBoard);

        // Units on their own board go to their portal
        // Our units go to Our portal on their board, Enemy units go to Enemy portal on their board
        if (isOurBoard)
        {
            if (ourPortal != null) return ourPortal.GridPosition;
            if (enemyPortal != null) return enemyPortal.GridPosition;
        }
        else
        {
            if (enemyPortal != null) return enemyPortal.GridPosition;
            if (ourPortal != null) return ourPortal.GridPosition;
        }
        return new Vector2Int(-1, -1);
    }

    private static readonly Vector2Int[] Directions = new[]
    {
        new Vector2Int(0, 1), new Vector2Int(1, 0),
        new Vector2Int(0, -1), new Vector2Int(-1, 0)
    };

    private bool BFSPathExists(Vector2Int start, Vector2Int end, bool isOurBoard, GridManager gm)
    {
        if (start == end) return true;

        var visited = new HashSet<Vector2Int> { start };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            foreach (var dir in Directions)
            {
                var next = cur + dir;
                if (!gm.IsInBounds(next)) continue;
                if (visited.Contains(next)) continue;

                var cell = gm.GetCell(next, isOurBoard);
                if (cell.IsOccupied && next != end) continue;

                if (next == end) return true;

                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        return false;
    }
}
