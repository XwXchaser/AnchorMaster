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
        gameObject.layer = isOnOurBoard ? LayerMask.NameToLayer("OurBoard") : LayerMask.NameToLayer("EnemyBoard");

        var cell = GridManager.Instance.GetCell(gridPos, isOnOurBoard);
        if (cell != null)
        {
            cell.IsOccupied = true;
            cell.Occupant = gameObject;
        }

        CreateVisual();
    }

    public void MoveTo(Vector2Int newGridPos)
    {
        GridManager gm = GridManager.Instance;

        // Clear old cell
        var oldCell = gm.GetCell(_gridPosition, _isOnOurBoard);
        if (oldCell != null && oldCell.Occupant == gameObject)
        {
            oldCell.IsOccupied = false;
            oldCell.Occupant = null;
        }

        _gridPosition = newGridPos;
        transform.position = gm.GridToWorld(newGridPos, _isOnOurBoard);

        // Mark new cell
        var newCell = gm.GetCell(newGridPos, _isOnOurBoard);
        if (newCell != null)
        {
            newCell.IsOccupied = true;
            newCell.Occupant = gameObject;
        }
    }

    private void CreateVisual()
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "PortalVisual";
        ring.transform.parent = transform;
        ring.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        ring.transform.localScale = new Vector3(0.6f, 0.3f, 0.6f);
        Color c = _type == PortalType.Our
            ? new Color(0.2f, 1f, 0.3f, 0.85f)
            : new Color(1f, 0.2f, 0f, 0.85f);
        var renderer = ring.GetComponent<Renderer>();
        renderer.material.color = c;
        renderer.material.SetFloat("_Mode", 2);
        renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        renderer.material.EnableKeyword("_ALPHABLEND_ON");
        renderer.material.renderQueue = 3000;
        Destroy(ring.GetComponent<Collider>());
    }

}
