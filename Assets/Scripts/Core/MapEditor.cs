using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MapEditor : MonoBehaviour
{
    [SerializeField] private GameObject _obstaclePrefab;
    [SerializeField] private Camera _ourCamera;
    [SerializeField] private Camera _enemyCamera;
    [SerializeField] private float _brushSpacing = 0.5f;
    [SerializeField] private KeyCode _toggleKey = KeyCode.E;
    [SerializeField] private KeyCode _ourBaseKey = KeyCode.B;
    [SerializeField] private KeyCode _enemyBaseKey = KeyCode.N;
    [SerializeField] private KeyCode _ourPortalOurSideKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode _ourPortalEnemySideKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode _enemyPortalOurSideKey = KeyCode.Alpha3;
    [SerializeField] private KeyCode _enemyPortalEnemySideKey = KeyCode.Alpha4;

    private bool _editorActive;
    private bool _isPainting;
    private Vector3 _lastPaintPos = Vector3.negativeInfinity;
    private List<GameObject> _editorObstacles = new List<GameObject>();
    private string _warningMessage;
    private float _warningTimer;
    private int _boardLayerMask;

    public bool IsEditorActive => _editorActive;

    private void Awake()
    {
        _boardLayerMask = LayerMask.GetMask("OurBoard", "EnemyBoard");
    }

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
        {
            _editorActive = !_editorActive;
            _isPainting = false;
            if (_editorActive)
            {
                _warningMessage = "地图编辑器已开启 - 游戏系统已暂停";
                _warningTimer = 2f;
                SetGameplaySystemsEnabled(false);
            }
            else
            {
                _warningMessage = "";
                _warningTimer = 0f;
                SetGameplaySystemsEnabled(true);
            }
            Debug.Log(_editorActive ? "[MapEditor] Editor ON (gameplay paused)" : "[MapEditor] Editor OFF (gameplay resumed)");
        }

        if (!_editorActive) return;

        if (_warningTimer > 0f)
        {
            _warningTimer -= Time.deltaTime;
            if (_warningTimer <= 0f) _warningMessage = "";
        }

        // Portal placement (snap to nearest grid)
        if (Input.GetKeyDown(_ourPortalOurSideKey))
            MovePortalToMouse(PortalType.Our, true);
        if (Input.GetKeyDown(_ourPortalEnemySideKey))
            MovePortalToMouse(PortalType.Our, false);
        if (Input.GetKeyDown(_enemyPortalOurSideKey))
            MovePortalToMouse(PortalType.Enemy, true);
        if (Input.GetKeyDown(_enemyPortalEnemySideKey))
            MovePortalToMouse(PortalType.Enemy, false);

        // Base placement
        if (Input.GetKeyDown(_ourBaseKey))
            MoveBaseToMouse(true);
        if (Input.GetKeyDown(_enemyBaseKey))
            MoveBaseToMouse(false);

        // Paint obstacles (free placement)
        if (Input.GetMouseButtonDown(0))
        {
            _isPainting = true;
            _lastPaintPos = Vector3.negativeInfinity;
        }
        if (Input.GetMouseButtonUp(0))
            _isPainting = false;

        if (_isPainting)
            PaintAtMouse();

        // Erase obstacles
        if (Input.GetMouseButtonDown(1))
            EraseAtMouse();
    }

    private void SetGameplaySystemsEnabled(bool enabled)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.enabled = enabled;
        if (EnemyAI.Instance != null)
            EnemyAI.Instance.enabled = enabled;
        if (SpawnQueue.Instance != null)
            SpawnQueue.Instance.enabled = enabled;
        if (BattleResolver.Instance != null)
            BattleResolver.Instance.enabled = enabled;
        var inputHandler = FindObjectOfType<InputHandler>();
        if (inputHandler != null)
            inputHandler.enabled = enabled;
    }

    private void MovePortalToMouse(PortalType type, bool isOnOurBoard)
    {
        if (!GetWorldHit(out Vector3 worldPos, out _)) return;
        Portal portal = PortalManager.Instance.GetPortalOnBoard(type, isOnOurBoard);
        if (portal == null) return;

        // Snap portal to nearest grid
        Vector2Int gridPos = GridManager.Instance.WorldToGrid(worldPos, isOnOurBoard);
        if (!GridManager.Instance.IsInBounds(gridPos)) return;
        portal.MoveTo(gridPos);
        Debug.Log($"[MapEditor] Portal {type} on {(isOnOurBoard ? "our" : "enemy")} board -> grid {gridPos}");
    }

    private void PaintAtMouse()
    {
        if (!GetWorldHit(out Vector3 worldPos, out bool isOurBoard)) return;

        if (Vector3.Distance(worldPos, _lastPaintPos) < _brushSpacing) return;
        _lastPaintPos = worldPos;

        GameObject obj;
        if (_obstaclePrefab != null)
        {
            obj = Instantiate(_obstaclePrefab, transform);
            obj.transform.position = worldPos;
        }
        else
        {
            obj = new GameObject("EditorObstacle");
            obj.transform.parent = transform;
            obj.transform.position = worldPos;
            CreateObstacleVisual(obj);
            CreateObstacleNavMesh(obj);
        }

        _editorObstacles.Add(obj);
        UpdatePathWarning(isOurBoard);
    }

    private void EraseAtMouse()
    {
        if (!GetWorldHit(out Vector3 worldPos, out _)) return;

        float bestDist = 0.8f;
        GameObject toRemove = null;
        foreach (var obj in _editorObstacles)
        {
            if (obj == null) continue;
            float d = Vector3.Distance(worldPos, obj.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                toRemove = obj;
            }
        }

        if (toRemove != null)
        {
            bool isOurBoard = IsOnOurBoardByPosition(toRemove.transform.position);
            _editorObstacles.Remove(toRemove);
            Destroy(toRemove);
            UpdatePathWarning(isOurBoard);
        }
    }

    private bool IsOnOurBoardByPosition(Vector3 worldPos)
    {
        float midX = (GridManager.Instance.GetBoardOrigin(true).x + GridManager.Instance.GetBoardOrigin(false).x) / 2f;
        // Our board is on the left side
        Vector3 ourOrigin = GridManager.Instance.GetBoardOrigin(true);
        float boardHalfW = GridManager.Instance.Width * GridManager.Instance.CellSize / 2f;
        return worldPos.x >= ourOrigin.x - boardHalfW && worldPos.x <= ourOrigin.x + boardHalfW;
    }

    private void MoveBaseToMouse(bool isOurBase)
    {
        if (!GetWorldHit(out Vector3 worldPos, out _)) return;

        var bases = FindObjectsOfType<Base>();
        foreach (var b in bases)
        {
            if (b.IsOurBase == isOurBase)
            {
                b.transform.position = worldPos;
                Debug.Log($"[MapEditor] Moved {(isOurBase ? "Our" : "Enemy")}Base to {worldPos}");
                break;
            }
        }
    }

    private void UpdatePathWarning(bool isOurBoard)
    {
        GridManager gm = GridManager.Instance;
        if (gm == null) return;

        var baseObj = FindBase(isOurBoard);
        if (baseObj == null) return;

        Portal portal = PortalManager.Instance.GetPortalOnBoard(PortalType.Our, isOurBoard)
            ?? PortalManager.Instance.GetPortalOnBoard(PortalType.Enemy, isOurBoard);
        if (portal == null) return;

        bool pathOk = NavMeshPathExists(baseObj.transform.position, portal.transform.position);
        if (!pathOk)
        {
            string side = isOurBoard ? "我方棋盘" : "敌方棋盘";
            _warningMessage = $"⚠ 警告: {side}道路被堵死，无法部署到实际游戏中！";
            _warningTimer = 3f;
        }
        else if (_warningMessage.Contains("道路被堵死"))
        {
            _warningMessage = "";
            _warningTimer = 0f;
        }
    }

    private bool NavMeshPathExists(Vector3 from, Vector3 to)
    {
        var path = new NavMeshPath();
        if (NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path))
            return path.status == NavMeshPathStatus.PathComplete;
        return false;
    }

    private Base FindBase(bool isOurBoard)
    {
        var bases = FindObjectsOfType<Base>();
        foreach (var b in bases)
            if (b.IsOurBase == isOurBoard) return b;
        return null;
    }

    private bool GetWorldHit(out Vector3 worldPos, out bool isOurBoard)
    {
        worldPos = Vector3.zero;
        isOurBoard = false;

        Vector3 mousePos = Input.mousePosition;
        Camera targetCam = mousePos.x < Screen.width / 2f ? _ourCamera : _enemyCamera;
        if (targetCam == null) return false;

        Ray ray = targetCam.ScreenPointToRay(mousePos);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, _boardLayerMask)) return false;

        bool isOurBoardHit = hit.collider.name.Contains("OurBoard");
        bool isEnemyBoardHit = hit.collider.name.Contains("EnemyBoard");
        if (!isOurBoardHit && !isEnemyBoardHit) return false;

        worldPos = hit.point;
        isOurBoard = isOurBoardHit;
        return true;
    }

    private void CreateObstacleVisual(GameObject obj)
    {
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.parent = obj.transform;
        trunk.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        trunk.transform.localScale = new Vector3(0.2f, 0.3f, 0.2f);
        trunk.GetComponent<Renderer>().material.color = new Color(0.3f, 0.5f, 0.2f);
        Destroy(trunk.GetComponent<Collider>());

        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.name = "Crown";
        crown.transform.parent = obj.transform;
        crown.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        crown.transform.localScale = new Vector3(0.4f, 0.35f, 0.4f);
        crown.GetComponent<Renderer>().material.color = new Color(0.2f, 0.6f, 0.15f);
        Destroy(crown.GetComponent<Collider>());
    }

    private void CreateObstacleNavMesh(GameObject obj)
    {
        var obs = obj.AddComponent<NavMeshObstacle>();
        obs.shape = NavMeshObstacleShape.Capsule;
        obs.center = Vector3.zero;
        obs.radius = 0.35f;
        obs.height = 1f;
        obs.carving = true;
    }

    private void OnGUI()
    {
        if (!string.IsNullOrEmpty(_warningMessage))
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 24;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.red;
            style.fontStyle = FontStyle.Bold;

            Rect rect = new Rect(0, 10, Screen.width, 40);
            GUI.Label(rect, _warningMessage, style);
        }
    }

    public List<GameObject> GetEditorObstacles() => _editorObstacles;
}
