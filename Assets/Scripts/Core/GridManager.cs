using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [SerializeField] private int _width = 8;
    [SerializeField] private int _height = 6;
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private Vector3 _ourBoardOrigin = new Vector3(-6f, 0f, 0f);
    [SerializeField] private Vector3 _enemyBoardOrigin = new Vector3(6f, 0f, 0f);

    public int Width => _width;
    public int Height => _height;
    public float CellSize => _cellSize;

    private GridCell[,] _ourGrid;
    private GridCell[,] _enemyGrid;

    private void Awake()
    {
        Instance = this;
        _ourGrid = new GridCell[_width, _height];
        _enemyGrid = new GridCell[_width, _height];
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                _ourGrid[x, y] = new GridCell();
                _enemyGrid[x, y] = new GridCell();
            }
        }
    }

    public Vector3 GridToWorld(Vector2Int gridPos, bool isOurSide)
    {
        Vector3 origin = isOurSide ? _ourBoardOrigin : _enemyBoardOrigin;
        return origin + new Vector3(gridPos.x * _cellSize, 0f, gridPos.y * _cellSize);
    }

    public Vector2Int WorldToGrid(Vector3 worldPos, bool isOurSide)
    {
        Vector3 origin = isOurSide ? _ourBoardOrigin : _enemyBoardOrigin;
        int x = Mathf.RoundToInt((worldPos.x - origin.x) / _cellSize);
        int y = Mathf.RoundToInt((worldPos.z - origin.z) / _cellSize);
        return new Vector2Int(x, y);
    }

    public bool IsInBounds(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < _width && gridPos.y >= 0 && gridPos.y < _height;
    }

    public GridCell GetCell(Vector2Int gridPos, bool isOurSide)
    {
        if (!IsInBounds(gridPos)) return null;
        return isOurSide ? _ourGrid[gridPos.x, gridPos.y] : _enemyGrid[gridPos.x, gridPos.y];
    }

    public Vector3 GetBoardCenter(bool isOurSide)
    {
        Vector3 origin = isOurSide ? _ourBoardOrigin : _enemyBoardOrigin;
        return origin + new Vector3((_width - 1) * _cellSize / 2f, 0f, (_height - 1) * _cellSize / 2f);
    }

    public Vector3 GetBoardOrigin(bool isOurSide)
    {
        return isOurSide ? _ourBoardOrigin : _enemyBoardOrigin;
    }
}

public class GridCell
{
    public bool IsOccupied;
    public GameObject Occupant;
}
