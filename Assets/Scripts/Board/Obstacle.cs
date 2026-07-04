using UnityEngine;
using UnityEngine.AI;

public class Obstacle : MonoBehaviour
{
    [SerializeField] private string _obstacleName = "Tree";
    [SerializeField] private int _maxHp = 3;
    [SerializeField] private bool _destructible = true;
    [SerializeField] private bool _isOnOurBoard;
    private Vector2Int _gridPosition;

    public string ObstacleName => _obstacleName;
    public int MaxHp => _maxHp;
    public bool Destructible => _destructible;
    public bool IsOnOurBoard => _isOnOurBoard;
    public Vector2Int GridPosition => _gridPosition;
    public int CurrentHp { get; private set; }

    private NavMeshObstacle _navObs;

    public void Initialize(Vector2Int gridPos, bool isOnOurBoard)
    {
        _gridPosition = gridPos;
        _isOnOurBoard = isOnOurBoard;
        CurrentHp = _maxHp;

        transform.position = GridManager.Instance.GridToWorld(gridPos, isOnOurBoard);
        GridManager.Instance.GetCell(gridPos, isOnOurBoard).IsOccupied = true;
        GridManager.Instance.GetCell(gridPos, isOnOurBoard).Occupant = gameObject;

        _navObs = gameObject.AddComponent<NavMeshObstacle>();
        _navObs.shape = NavMeshObstacleShape.Capsule;
        _navObs.center = Vector3.zero;
        _navObs.radius = 0.45f;
        _navObs.height = 1f;
        _navObs.carving = true;

        CreateVisual();
    }

    private void CreateVisual()
    {
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.parent = transform;
        trunk.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        trunk.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        trunk.GetComponent<Renderer>().material.color = new Color(0.3f, 0.5f, 0.2f);
        Destroy(trunk.GetComponent<Collider>());

        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.name = "Crown";
        crown.transform.parent = transform;
        crown.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        crown.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
        crown.GetComponent<Renderer>().material.color = new Color(0.2f, 0.6f, 0.15f);
        Destroy(crown.GetComponent<Collider>());
    }

    public void TakeDamage(int amount)
    {
        if (!_destructible) return;
        CurrentHp = Mathf.Max(0, CurrentHp - amount);
        if (CurrentHp <= 0)
            DestroyObstacle();
    }

    public void DestroyObstacle()
    {
        GridManager.Instance.GetCell(_gridPosition, _isOnOurBoard).IsOccupied = false;
        GridManager.Instance.GetCell(_gridPosition, _isOnOurBoard).Occupant = null;
        if (_navObs != null) Destroy(_navObs);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (GridManager.Instance != null)
        {
            var cell = GridManager.Instance.GetCell(_gridPosition, _isOnOurBoard);
            if (cell != null && cell.Occupant == gameObject)
            {
                cell.IsOccupied = false;
                cell.Occupant = null;
            }
        }
    }
}
