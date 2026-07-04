using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum EditorTool
{
    Brush,
    Eraser,
    PlaceOurPortalOurSide,
    PlaceOurPortalEnemySide,
    PlaceEnemyPortalEnemySide,
    PlaceEnemyPortalOurSide,
    PlaceOurBase,
    PlaceEnemyBase
}

public class EditorController : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private Camera _ourCamera;
    [SerializeField] private Camera _enemyCamera;

    [Header("Toolbar Buttons")]
    [SerializeField] private Button _brushButton;
    [SerializeField] private Button _eraserButton;
    [SerializeField] private Button _ourPortalOurSideButton;
    [SerializeField] private Button _ourPortalEnemySideButton;
    [SerializeField] private Button _enemyPortalEnemySideButton;
    [SerializeField] private Button _enemyPortalOurSideButton;
    [SerializeField] private Button _ourBaseButton;
    [SerializeField] private Button _enemyBaseButton;
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _loadButton;

    [Header("Status")]
    [SerializeField] private Text _statusText;

    [Header("Prefabs")]
    [SerializeField] private GameObject _editorObstaclePrefab;
    [SerializeField] private GameObject _editorPortalPrefab;
    [SerializeField] private GameObject _editorBasePrefab;

    private EditorTool _currentTool = EditorTool.Brush;
    private int _boardLayerMask;
    private bool _isPainting;

    // Editor state: visual GameObjects tracking
    private Dictionary<Vector2Int, GameObject> _ourObstacles = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> _enemyObstacles = new Dictionary<Vector2Int, GameObject>();
    private GameObject _ourPortalOurSideVis;
    private GameObject _ourPortalEnemySideVis;
    private GameObject _enemyPortalEnemySideVis;
    private GameObject _enemyPortalOurSideVis;
    private GameObject _ourBaseVis;
    private GameObject _enemyBaseVis;

    // Current positions
    private Vector2Int _ourPortalOurSidePos = new Vector2Int(4, 5);
    private Vector2Int _ourPortalEnemySidePos = new Vector2Int(4, 5);
    private Vector2Int _enemyPortalEnemySidePos = new Vector2Int(5, 5);
    private Vector2Int _enemyPortalOurSidePos = new Vector2Int(5, 5);
    private Vector2Int _ourBasePos = new Vector2Int(0, 5);
    private Vector2Int _enemyBasePos = new Vector2Int(9, 5);

    private MapConfig _loadedConfig;

    private void Start()
    {
        _boardLayerMask = LayerMask.GetMask("OurBoard", "EnemyBoard");

        _brushButton.onClick.AddListener(() => SelectTool(EditorTool.Brush));
        _eraserButton.onClick.AddListener(() => SelectTool(EditorTool.Eraser));
        _ourPortalOurSideButton.onClick.AddListener(() => SelectTool(EditorTool.PlaceOurPortalOurSide));
        _ourPortalEnemySideButton.onClick.AddListener(() => SelectTool(EditorTool.PlaceOurPortalEnemySide));
        _enemyPortalEnemySideButton.onClick.AddListener(() => SelectTool(EditorTool.PlaceEnemyPortalEnemySide));
        _enemyPortalOurSideButton.onClick.AddListener(() => SelectTool(EditorTool.PlaceEnemyPortalOurSide));
        _ourBaseButton.onClick.AddListener(() => SelectTool(EditorTool.PlaceOurBase));
        _enemyBaseButton.onClick.AddListener(() => SelectTool(EditorTool.PlaceEnemyBase));
        _saveButton.onClick.AddListener(SaveMap);
        _loadButton.onClick.AddListener(LoadMap);

        SelectTool(EditorTool.Brush);
        CreateInitialVisuals();
    }

    private void SelectTool(EditorTool tool)
    {
        _currentTool = tool;
        _statusText.text = $"当前工具: {GetToolName(tool)}";
        UpdateButtonHighlights();
    }

    private string GetToolName(EditorTool tool) => tool switch
    {
        EditorTool.Brush => "画笔",
        EditorTool.Eraser => "橡皮",
        EditorTool.PlaceOurPortalOurSide => "我方传送门(己方棋盘)",
        EditorTool.PlaceOurPortalEnemySide => "我方传送门(对方棋盘)",
        EditorTool.PlaceEnemyPortalEnemySide => "敌方传送门(对方棋盘)",
        EditorTool.PlaceEnemyPortalOurSide => "敌方传送门(己方棋盘)",
        EditorTool.PlaceOurBase => "我方基地",
        EditorTool.PlaceEnemyBase => "敌方基地",
        _ => "未知"
    };

    private void UpdateButtonHighlights()
    {
        SetButtonColor(_brushButton, _currentTool == EditorTool.Brush);
        SetButtonColor(_eraserButton, _currentTool == EditorTool.Eraser);
        SetButtonColor(_ourPortalOurSideButton, _currentTool == EditorTool.PlaceOurPortalOurSide);
        SetButtonColor(_ourPortalEnemySideButton, _currentTool == EditorTool.PlaceOurPortalEnemySide);
        SetButtonColor(_enemyPortalEnemySideButton, _currentTool == EditorTool.PlaceEnemyPortalEnemySide);
        SetButtonColor(_enemyPortalOurSideButton, _currentTool == EditorTool.PlaceEnemyPortalOurSide);
        SetButtonColor(_ourBaseButton, _currentTool == EditorTool.PlaceOurBase);
        SetButtonColor(_enemyBaseButton, _currentTool == EditorTool.PlaceEnemyBase);
    }

    private void SetButtonColor(Button btn, bool active)
    {
        var colors = btn.colors;
        colors.normalColor = active ? new Color(0.4f, 0.7f, 1f) : Color.white;
        btn.colors = colors;
    }

    private void Update()
    {
        bool isBrushTool = _currentTool == EditorTool.Brush || _currentTool == EditorTool.Eraser;

        if (isBrushTool)
        {
            if (Input.GetMouseButtonDown(0))
                _isPainting = true;
            if (Input.GetMouseButtonUp(0))
                _isPainting = false;
            if (_isPainting)
                HandlePaintStroke();
            if (Input.GetMouseButtonDown(1))
                HandleEraserClick();
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
                HandleToolClick();
        }
    }

    private void HandlePaintStroke()
    {
        if (!GetWorldHit(out Vector3 worldPos, out bool isOurBoard)) return;
        Vector2Int gridPos = GridManager.Instance.WorldToGrid(worldPos, isOurBoard);
        if (!GridManager.Instance.IsInBounds(gridPos)) return;

        if (_currentTool == EditorTool.Brush)
            PlaceObstacle(gridPos, isOurBoard);
        else
            RemoveObstacle(gridPos, isOurBoard);
    }

    private void HandleEraserClick()
    {
        if (!GetWorldHit(out Vector3 worldPos, out bool isOurBoard)) return;
        Vector2Int gridPos = GridManager.Instance.WorldToGrid(worldPos, isOurBoard);
        if (!GridManager.Instance.IsInBounds(gridPos)) return;
        RemoveObstacle(gridPos, isOurBoard);
    }

    private void HandleToolClick()
    {
        if (!GetWorldHit(out Vector3 worldPos, out bool isOurBoard)) return;
        Vector2Int gridPos = GridManager.Instance.WorldToGrid(worldPos, isOurBoard);
        if (!GridManager.Instance.IsInBounds(gridPos)) return;

        switch (_currentTool)
        {
            case EditorTool.PlaceOurPortalOurSide:
                PlacePortalVisual(ref _ourPortalOurSidePos, ref _ourPortalOurSideVis, gridPos, true, Color.green);
                break;
            case EditorTool.PlaceOurPortalEnemySide:
                PlacePortalVisual(ref _ourPortalEnemySidePos, ref _ourPortalEnemySideVis, gridPos, false, Color.green);
                break;
            case EditorTool.PlaceEnemyPortalEnemySide:
                PlacePortalVisual(ref _enemyPortalEnemySidePos, ref _enemyPortalEnemySideVis, gridPos, false, Color.red);
                break;
            case EditorTool.PlaceEnemyPortalOurSide:
                PlacePortalVisual(ref _enemyPortalOurSidePos, ref _enemyPortalOurSideVis, gridPos, true, Color.red);
                break;
            case EditorTool.PlaceOurBase:
                PlaceBaseVisual(ref _ourBasePos, ref _ourBaseVis, gridPos, true, Color.blue);
                break;
            case EditorTool.PlaceEnemyBase:
                PlaceBaseVisual(ref _enemyBasePos, ref _enemyBaseVis, gridPos, false, Color.red);
                break;
        }

        CheckConnectivity(isOurBoard);
    }

    private void PlaceObstacle(Vector2Int gridPos, bool isOurBoard)
    {
        var dict = isOurBoard ? _ourObstacles : _enemyObstacles;
        if (dict.ContainsKey(gridPos)) return;

        if (!CanPlaceAt(gridPos, isOurBoard)) return;

        var cell = GridManager.Instance.GetCell(gridPos, isOurBoard);
        cell.IsOccupied = true;
        cell.Occupant = null; // Editor-governed

        var go = CreateEditorVisual(gridPos, isOurBoard, new Color(0.3f, 0.5f, 0.2f), 0.5f);
        dict[gridPos] = go;
        CheckConnectivity(isOurBoard);
    }

    private void RemoveObstacle(Vector2Int gridPos, bool isOurBoard)
    {
        var dict = isOurBoard ? _ourObstacles : _enemyObstacles;
        if (!dict.TryGetValue(gridPos, out var go)) return;

        dict.Remove(gridPos);
        Destroy(go);

        var cell = GridManager.Instance.GetCell(gridPos, isOurBoard);
        if (cell != null && cell.Occupant == null)
            cell.IsOccupied = false;

        CheckConnectivity(isOurBoard);
    }

    private bool CanPlaceAt(Vector2Int gridPos, bool isOurBoard)
    {
        // Don't place on base/portal positions
        if (gridPos == _ourBasePos && isOurBoard) return false;
        if (gridPos == _enemyBasePos && !isOurBoard) return false;
        if (gridPos == _ourPortalOurSidePos && isOurBoard) return false;
        if (gridPos == _enemyPortalOurSidePos && isOurBoard) return false;
        if (gridPos == _ourPortalEnemySidePos && !isOurBoard) return false;
        if (gridPos == _enemyPortalEnemySidePos && !isOurBoard) return false;

        var cell = GridManager.Instance.GetCell(gridPos, isOurBoard);
        if (cell != null && cell.IsOccupied) return false;
        return true;
    }

    private void PlacePortalVisual(ref Vector2Int pos, ref GameObject vis, Vector2Int newPos, bool isOurBoard, Color color)
    {
        if (!GridManager.Instance.IsInBounds(newPos)) return;

        pos = newPos;
        if (vis != null) Destroy(vis);
        vis = CreateEditorVisual(pos, isOurBoard, color, 0.6f);
    }

    private void PlaceBaseVisual(ref Vector2Int pos, ref GameObject vis, Vector2Int newPos, bool isOurBoard, Color color)
    {
        if (!GridManager.Instance.IsInBounds(newPos)) return;

        pos = newPos;
        if (vis != null) Destroy(vis);
        vis = CreateEditorVisual(pos, isOurBoard, color, 0.8f);
    }

    private GameObject CreateEditorVisual(Vector2Int gridPos, bool isOurBoard, Color color, float scale)
    {
        GameObject go;
        if (_editorObstaclePrefab != null)
        {
            go = Instantiate(_editorObstaclePrefab, transform);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(go.GetComponent<Collider>());
        }

        go.transform.position = GridManager.Instance.GridToWorld(gridPos, isOurBoard);
        go.transform.localScale = new Vector3(scale, 0.3f, scale);
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial.color = color;

        int layer = isOurBoard ? LayerMask.NameToLayer("OurBoard") : LayerMask.NameToLayer("EnemyBoard");
        go.layer = layer;

        return go;
    }

    private void CreateInitialVisuals()
    {
        _ourBaseVis = CreateEditorVisual(_ourBasePos, true, Color.blue, 0.8f);
        _enemyBaseVis = CreateEditorVisual(_enemyBasePos, false, Color.red, 0.8f);
        _ourPortalOurSideVis = CreateEditorVisual(_ourPortalOurSidePos, true, Color.green, 0.6f);
        _ourPortalEnemySideVis = CreateEditorVisual(_ourPortalEnemySidePos, false, Color.green, 0.6f);
        _enemyPortalEnemySideVis = CreateEditorVisual(_enemyPortalEnemySidePos, false, Color.red, 0.6f);
        _enemyPortalOurSideVis = CreateEditorVisual(_enemyPortalOurSidePos, true, Color.red, 0.6f);
    }

    private void CheckConnectivity(bool isOurBoard)
    {
        Vector2Int basePos = isOurBoard ? _ourBasePos : _enemyBasePos;
        Vector2Int portalPos = isOurBoard ? _ourPortalOurSidePos : _enemyPortalEnemySidePos;
        var obstacles = isOurBoard ? _ourObstacles : _enemyObstacles;

        if (BFSPathExists(basePos, portalPos, isOurBoard, obstacles))
        {
            if (_statusText.text.Contains("道路被堵死"))
                _statusText.text = $"当前工具: {GetToolName(_currentTool)}";
        }
        else
        {
            string side = isOurBoard ? "我方棋盘" : "敌方棋盘";
            _statusText.text = $"⚠ {side}道路被堵死！";
        }
    }

    private bool BFSPathExists(Vector2Int start, Vector2Int end, bool isOurBoard, Dictionary<Vector2Int, GameObject> obstacles)
    {
        if (start == end) return true;

        var visited = new System.Collections.Generic.HashSet<Vector2Int> { start };
        var queue = new System.Collections.Generic.Queue<Vector2Int>();
        queue.Enqueue(start);

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            foreach (var dir in dirs)
            {
                var next = cur + dir;
                if (!GridManager.Instance.IsInBounds(next)) continue;
                if (visited.Contains(next)) continue;
                if (obstacles.ContainsKey(next) && next != end) continue;
                if (next == end) return true;
                visited.Add(next);
                queue.Enqueue(next);
            }
        }
        return false;
    }

    private bool GetWorldHit(out Vector3 worldPos, out bool isOurBoard)
    {
        worldPos = Vector3.zero;
        isOurBoard = false;

        Vector3 mousePos = Input.mousePosition;
        Camera cam = mousePos.x < Screen.width / 2f ? _ourCamera : _enemyCamera;
        if (cam == null) return false;

        Ray ray = cam.ScreenPointToRay(mousePos);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, _boardLayerMask)) return false;

        isOurBoard = hit.collider.name.Contains("OurBoard");
        if (!isOurBoard && !hit.collider.name.Contains("EnemyBoard")) return false;

        worldPos = hit.point;
        return true;
    }

    private void SaveMap()
    {
#if UNITY_EDITOR
        string path = EditorUtility.SaveFilePanelInProject("Save Map Config", "Map_New", "asset",
            "Choose a location to save the map config", "Assets/Data/Maps");
        if (string.IsNullOrEmpty(path)) return;

        var config = ScriptableObject.CreateInstance<MapConfig>();
        config.OurBaseGridPos = _ourBasePos;
        config.EnemyBaseGridPos = _enemyBasePos;
        config.OurPortalOurSidePos = _ourPortalOurSidePos;
        config.OurPortalEnemySidePos = _ourPortalEnemySidePos;
        config.EnemyPortalEnemySidePos = _enemyPortalEnemySidePos;
        config.EnemyPortalOurSidePos = _enemyPortalOurSidePos;
        config.OurObstacles = new List<Vector2Int>(_ourObstacles.Keys);
        config.EnemyObstacles = new List<Vector2Int>(_enemyObstacles.Keys);

        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        _loadedConfig = config;
        _statusText.text = $"已保存: {path}";
        Debug.Log($"[EditorController] Map saved to {path}");
#endif
    }

    private void LoadMap()
    {
#if UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel("Load Map Config", "Assets/Data/Maps", "asset");
        if (string.IsNullOrEmpty(path)) return;

        if (path.StartsWith(Application.dataPath))
            path = "Assets" + path.Substring(Application.dataPath.Length);

        var config = AssetDatabase.LoadAssetAtPath<MapConfig>(path);
        if (config == null)
        {
            _statusText.text = "加载失败：无效的 MapConfig";
            return;
        }

        _loadedConfig = config;
        LoadConfigState(config);
        _statusText.text = $"已加载: {config.name}";
        Debug.Log($"[EditorController] Map loaded from {path}");
#endif
    }

    private void LoadConfigState(MapConfig config)
    {
        // Clear existing
        ClearAllVisuals();

        _ourBasePos = config.OurBaseGridPos;
        _enemyBasePos = config.EnemyBaseGridPos;
        _ourPortalOurSidePos = config.OurPortalOurSidePos;
        _ourPortalEnemySidePos = config.OurPortalEnemySidePos;
        _enemyPortalEnemySidePos = config.EnemyPortalEnemySidePos;
        _enemyPortalOurSidePos = config.EnemyPortalOurSidePos;

        _ourBaseVis = CreateEditorVisual(_ourBasePos, true, Color.blue, 0.8f);
        _enemyBaseVis = CreateEditorVisual(_enemyBasePos, false, Color.red, 0.8f);
        _ourPortalOurSideVis = CreateEditorVisual(_ourPortalOurSidePos, true, Color.green, 0.6f);
        _ourPortalEnemySideVis = CreateEditorVisual(_ourPortalEnemySidePos, false, Color.green, 0.6f);
        _enemyPortalEnemySideVis = CreateEditorVisual(_enemyPortalEnemySidePos, false, Color.red, 0.6f);
        _enemyPortalOurSideVis = CreateEditorVisual(_enemyPortalOurSidePos, true, Color.red, 0.6f);

        foreach (var pos in config.OurObstacles)
        {
            var cell = GridManager.Instance.GetCell(pos, true);
            cell.IsOccupied = true;
            var go = CreateEditorVisual(pos, true, new Color(0.3f, 0.5f, 0.2f), 0.5f);
            _ourObstacles[pos] = go;
        }
        foreach (var pos in config.EnemyObstacles)
        {
            var cell = GridManager.Instance.GetCell(pos, false);
            cell.IsOccupied = true;
            var go = CreateEditorVisual(pos, false, new Color(0.3f, 0.5f, 0.2f), 0.5f);
            _enemyObstacles[pos] = go;
        }
    }

    private void ClearAllVisuals()
    {
        foreach (var kv in _ourObstacles)
        {
            var cell = GridManager.Instance.GetCell(kv.Key, true);
            if (cell != null && cell.Occupant == null) cell.IsOccupied = false;
            Destroy(kv.Value);
        }
        foreach (var kv in _enemyObstacles)
        {
            var cell = GridManager.Instance.GetCell(kv.Key, false);
            if (cell != null && cell.Occupant == null) cell.IsOccupied = false;
            Destroy(kv.Value);
        }
        _ourObstacles.Clear();
        _enemyObstacles.Clear();

        if (_ourBaseVis != null) Destroy(_ourBaseVis);
        if (_enemyBaseVis != null) Destroy(_enemyBaseVis);
        if (_ourPortalOurSideVis != null) Destroy(_ourPortalOurSideVis);
        if (_ourPortalEnemySideVis != null) Destroy(_ourPortalEnemySideVis);
        if (_enemyPortalEnemySideVis != null) Destroy(_enemyPortalEnemySideVis);
        if (_enemyPortalOurSideVis != null) Destroy(_enemyPortalOurSideVis);
    }
}
