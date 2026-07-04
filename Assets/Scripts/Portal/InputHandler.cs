using UnityEngine;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private Camera _ourCamera;
    [SerializeField] private Camera _enemyCamera;

    private int _boardLayerMask;

    private void Awake()
    {
        _boardLayerMask = LayerMask.GetMask("OurBoard", "EnemyBoard");
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentPhase != TurnPhase.Preparation) return;

        if (Input.GetMouseButtonDown(0))
            HandleClick(PortalType.Our);
        else if (Input.GetMouseButtonDown(1))
            HandleClick(PortalType.Enemy);

        if (Input.GetKeyDown(KeyCode.Alpha1))
            HandleObstaclePlace();
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            HandleObstacleRemove();
    }

    private void HandleObstaclePlace()
    {
        if (!TryGetGridFromMouse(out Vector2Int gridPos, out bool isOurBoard)) return;
        ObstacleManager.Instance?.PlaceObstacle(gridPos, isOurBoard);
    }

    private void HandleObstacleRemove()
    {
        if (!TryGetGridFromMouse(out Vector2Int gridPos, out bool isOurBoard)) return;
        var cell = GridManager.Instance.GetCell(gridPos, isOurBoard);
        if (cell != null && cell.Occupant != null)
        {
            var obs = cell.Occupant.GetComponent<Obstacle>();
            if (obs != null)
                obs.DestroyObstacle();
        }
    }

    private bool TryGetGridFromMouse(out Vector2Int gridPos, out bool isOurBoard)
    {
        gridPos = Vector2Int.zero;
        isOurBoard = false;

        Vector3 mousePos = Input.mousePosition;
        Camera targetCam = mousePos.x < Screen.width / 2f ? _ourCamera : _enemyCamera;
        if (targetCam == null) return false;

        Ray ray = targetCam.ScreenPointToRay(mousePos);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, _boardLayerMask)) return false;

        bool isOurBoardHit = hit.collider.name.Contains("OurBoard");
        bool isEnemyBoardHit = hit.collider.name.Contains("EnemyBoard");
        if (!isOurBoardHit && !isEnemyBoardHit) return false;

        gridPos = GridManager.Instance.WorldToGrid(hit.point, isOurBoardHit);
        isOurBoard = isOurBoardHit;
        return true;
    }

    private void HandleClick(PortalType type)
    {
        Vector3 mousePos = Input.mousePosition;
        bool isOverOurBoard = mousePos.x < Screen.width / 2f;
        Camera targetCam = isOverOurBoard ? _ourCamera : _enemyCamera;

        if (targetCam == null) return;

        Ray ray = targetCam.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, _boardLayerMask))
        {
            bool isOurBoardHit = hit.collider.name.Contains("OurBoard");
            bool isEnemyBoardHit = hit.collider.name.Contains("EnemyBoard");

            if (!isOurBoardHit && !isEnemyBoardHit) return;

            Vector2Int gridPos = GridManager.Instance.WorldToGrid(hit.point, isOurBoardHit);
            PortalManager.Instance.PlacePortal(gridPos, isOurBoardHit, type);
        }
    }
}
