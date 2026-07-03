using UnityEngine;

public class PortalManager : MonoBehaviour
{
    public static PortalManager Instance { get; private set; }

    [SerializeField] private float _minPortalDistance = 1.5f;

    private Portal _ourPortalOurSide;
    private Portal _ourPortalEnemySide;
    private Portal _enemyPortalEnemySide;
    private Portal _enemyPortalOurSide;

    public Portal OurPortalOurSide => _ourPortalOurSide;
    public Portal OurPortalEnemySide => _ourPortalEnemySide;
    public Portal EnemyPortalEnemySide => _enemyPortalEnemySide;
    public Portal EnemyPortalOurSide => _enemyPortalOurSide;

    public float MinPortalDistance => _minPortalDistance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        CreatePortals();
    }

    private void CreatePortals()
    {
        GridManager gm = GridManager.Instance;

        // Default positions: our portal at left-center, enemy portal at right-center
        Vector2Int ourDefault = new Vector2Int(2, 3);
        Vector2Int enemyDefault = new Vector2Int(5, 3);

        _ourPortalOurSide = CreatePortalObject("OurPortal_OurSide", PortalType.Our, true, ourDefault);
        _ourPortalEnemySide = CreatePortalObject("OurPortal_EnemySide", PortalType.Our, false, ourDefault);

        _enemyPortalEnemySide = CreatePortalObject("EnemyPortal_EnemySide", PortalType.Enemy, false, enemyDefault);
        _enemyPortalOurSide = CreatePortalObject("EnemyPortal_OurSide", PortalType.Enemy, true, enemyDefault);
    }

    private Portal CreatePortalObject(string name, PortalType type, bool isOnOurBoard, Vector2Int pos)
    {
        GameObject obj = new GameObject(name);
        obj.transform.parent = transform;
        Portal portal = obj.AddComponent<Portal>();
        portal.Initialize(type, isOnOurBoard, pos);
        return portal;
    }

    public Portal GetPortalOnBoard(PortalType type, bool isOnOurBoard)
    {
        if (type == PortalType.Our)
            return isOnOurBoard ? _ourPortalOurSide : _ourPortalEnemySide;
        else
            return isOnOurBoard ? _enemyPortalOurSide : _enemyPortalEnemySide;
    }

    public bool CanPlacePortal(Vector2Int gridPos, bool isOnOurBoard, PortalType placingType)
    {
        GridManager gm = GridManager.Instance;
        if (!gm.IsInBounds(gridPos)) return false;

        var otherPortals = new[]
        {
            GetPortalOnBoard(PortalType.Our, isOnOurBoard),
            GetPortalOnBoard(PortalType.Enemy, isOnOurBoard)
        };

        foreach (var p in otherPortals)
        {
            if (p == null) continue;
            if (p.Type == placingType) continue; // Same type can be replaced
            Vector3 myWorld = gm.GridToWorld(gridPos, isOnOurBoard);
            Vector3 otherWorld = gm.GridToWorld(p.GridPosition, isOnOurBoard);
            if (Vector3.Distance(myWorld, otherWorld) < _minPortalDistance)
                return false;
        }

        return true;
    }

    public void PlacePortal(Vector2Int gridPos, bool isOnOurBoard, PortalType type)
    {
        if (!CanPlacePortal(gridPos, isOnOurBoard, type)) return;

        Portal portalOurSide = GetPortalOnBoard(type, true);
        Portal portalEnemySide = GetPortalOnBoard(type, false);

        if (isOnOurBoard)
        {
            portalOurSide.MoveTo(gridPos);
            // Mirror position on enemy board
            if (GridManager.Instance.IsInBounds(gridPos))
                portalEnemySide.MoveTo(gridPos);
        }
        else
        {
            portalEnemySide.MoveTo(gridPos);
            if (GridManager.Instance.IsInBounds(gridPos))
                portalOurSide.MoveTo(gridPos);
        }
    }
}
