using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    [SerializeField] private bool _isLeftCamera = true;
    [SerializeField] private float _minDistance = 5f;
    [SerializeField] private float _maxDistance = 25f;
    [SerializeField] private float _zoomSpeed = 3f;
    [SerializeField] private float _panSpeed = 0.03f;

    private Camera _cam;
    private Vector3 _targetPoint;
    private float _distance;
    private float _pitchAngle;
    private float _yawAngle;
    private int _boardLayerMask;

    private void Start()
    {
        _cam = GetComponent<Camera>();
        _boardLayerMask = LayerMask.GetMask("OurBoard", "EnemyBoard");

        Vector3 euler = _cam.transform.rotation.eulerAngles;
        _pitchAngle = euler.x;
        _yawAngle = euler.y;

        Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, _boardLayerMask))
        {
            _targetPoint = hit.point;
            _distance = hit.distance;
        }
        else
        {
            _distance = 10f;
            _targetPoint = _cam.transform.position + _cam.transform.forward * _distance;
            _targetPoint.y = 0f;
        }
    }

    private void LateUpdate()
    {
        bool isMyHalf = _isLeftCamera
            ? Input.mousePosition.x < Screen.width / 2f
            : Input.mousePosition.x >= Screen.width / 2f;

        if (!isMyHalf) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
            HandleZoom(scroll);

        if (Input.GetMouseButton(2))
            HandlePan();
    }

    private void HandleZoom(float scroll)
    {
        Vector3? worldUnderMouse = GetBoardPointUnderMouse();

        _distance = Mathf.Clamp(_distance - scroll * _zoomSpeed, _minDistance, _maxDistance);

        if (worldUnderMouse.HasValue)
        {
            ApplyPositionRotation();
            Vector3? worldAfter = GetBoardPointUnderMouse();
            if (worldAfter.HasValue)
                _targetPoint += worldUnderMouse.Value - worldAfter.Value;
        }

        ApplyPositionRotation();
    }

    private void HandlePan()
    {
        float dx = Input.GetAxis("Mouse X");
        float dy = Input.GetAxis("Mouse Y");

        Vector3 right = _cam.transform.right;
        Vector3 forwardInPlane = Vector3.ProjectOnPlane(_cam.transform.forward, Vector3.up).normalized;

        float scale = _panSpeed * _distance;
        _targetPoint -= right * (dx * scale) + forwardInPlane * (dy * scale);

        ApplyPositionRotation();
    }

    private Vector3? GetBoardPointUnderMouse()
    {
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, _boardLayerMask))
            return hit.point;
        return null;
    }

    private void ApplyPositionRotation()
    {
        Quaternion rot = Quaternion.Euler(_pitchAngle, _yawAngle, 0f);
        _cam.transform.rotation = rot;
        _cam.transform.position = _targetPoint - rot * Vector3.forward * _distance;
    }
}
