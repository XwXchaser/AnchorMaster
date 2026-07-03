using UnityEngine;

public enum PortalType
{
    Our,
    Enemy
}

public class Portal : MonoBehaviour
{
    [SerializeField] private PortalType _type;
    [SerializeField] private bool _isOnOurBoard;
    private Vector2Int _gridPosition;

    public PortalType Type => _type;
    public bool IsOnOurBoard => _isOnOurBoard;
    public Vector2Int GridPosition => _gridPosition;

    public void Initialize(PortalType type, bool isOnOurBoard, Vector2Int gridPos)
    {
        _type = type;
        _isOnOurBoard = isOnOurBoard;
        _gridPosition = gridPos;
        transform.position = GridManager.Instance.GridToWorld(gridPos, isOnOurBoard);

        GridManager.Instance.GetCell(gridPos, isOnOurBoard).IsOccupied = true;
        GridManager.Instance.GetCell(gridPos, isOnOurBoard).Occupant = gameObject;

        CreateVisual();
    }

    public void MoveTo(Vector2Int newGridPos)
    {
        GridManager.Instance.GetCell(_gridPosition, _isOnOurBoard).IsOccupied = false;
        GridManager.Instance.GetCell(_gridPosition, _isOnOurBoard).Occupant = null;

        _gridPosition = newGridPos;
        transform.position = GridManager.Instance.GridToWorld(newGridPos, _isOnOurBoard);

        GridManager.Instance.GetCell(newGridPos, _isOnOurBoard).IsOccupied = true;
        GridManager.Instance.GetCell(newGridPos, _isOnOurBoard).Occupant = gameObject;
    }

    private void CreateVisual()
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "PortalVisual";
        ring.transform.parent = transform;
        ring.transform.localPosition = Vector3.zero;
        ring.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
        Color c = _type == PortalType.Our ? Color.green : new Color(1f, 0.5f, 0f);
        ring.GetComponent<Renderer>().material.color = c;
    }
}
