using UnityEngine;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private LayerMask _boardLayerMask = ~0;

    private Camera _ourCamera;
    private Camera _enemyCamera;
    private bool _isOverOurBoard;

    private void Start()
    {
        var cams = FindObjectsOfType<Camera>();
        foreach (var cam in cams)
        {
            if (cam.name == "OurBoardCamera") _ourCamera = cam;
            else if (cam.name == "EnemyBoardCamera") _enemyCamera = cam;
        }
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentPhase != TurnPhase.Preparation) return;

        if (Input.GetMouseButtonDown(0))
            HandleClick(PortalType.Our);
        else if (Input.GetMouseButtonDown(1))
            HandleClick(PortalType.Enemy);
    }

    private void HandleClick(PortalType type)
    {
        Vector3 mousePos = Input.mousePosition;

        // Determine which board the mouse is over based on viewport
        bool isOverOurBoard = mousePos.x < Screen.width / 2f;
        Camera targetCam = isOverOurBoard ? _ourCamera : _enemyCamera;

        if (targetCam == null) return;

        Ray ray = targetCam.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, _boardLayerMask))
        {
            bool isOurBoardHit = hit.collider.name == "OurBoard" || hit.collider.name.StartsWith("Our");
            bool isEnemyBoardHit = hit.collider.name == "EnemyBoard" || hit.collider.name.StartsWith("Enemy");

            if (!isOurBoardHit && !isEnemyBoardHit) return;

            Vector2Int gridPos = GridManager.Instance.WorldToGrid(hit.point, isOurBoardHit);
            PortalManager.Instance.PlacePortal(gridPos, isOurBoardHit, type);
        }
    }
}
