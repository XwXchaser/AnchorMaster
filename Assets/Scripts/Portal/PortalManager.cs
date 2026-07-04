using UnityEngine;
using UnityEngine.AI;

public class PortalManager : MonoBehaviour
{
    public static PortalManager Instance { get; private set; }

    [SerializeField] private float _minPortalDistance = 0.5f;

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
        Vector2Int ourDefault = new Vector2Int(4, 5);
        Vector2Int enemyDefault = new Vector2Int(5, 5);

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
        if (!gm.IsInBounds(gridPos))
        {
            Debug.Log($"[PortalManager] CanPlace rejected: gridPos={gridPos} out of bounds");
            return false;
        }

        var cell = gm.GetCell(gridPos, isOnOurBoard);
        Portal myPortal = GetPortalOnBoard(placingType, isOnOurBoard);
        if (cell != null && cell.IsOccupied && cell.Occupant != myPortal.gameObject)
        {
            Debug.Log($"[PortalManager] CanPlace rejected: gridPos={gridPos} occupied by {cell.Occupant.name}");
            return false;
        }

        Debug.Log($"[PortalManager] CanPlace accepted: gridPos={gridPos}, board={(isOnOurBoard?"Our":"Enemy")}, type={placingType}");
        return true;
    }

    public void PlacePortal(Vector2Int gridPos, bool isOnOurBoard, PortalType type)
    {
        if (!CanPlacePortal(gridPos, isOnOurBoard, type)) return;
        Portal portal = GetPortalOnBoard(type, isOnOurBoard);
        portal.MoveTo(gridPos);
    }
}
