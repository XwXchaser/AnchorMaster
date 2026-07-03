using UnityEngine;
using UnityEngine.Events;

public class Base : MonoBehaviour
{
    [SerializeField] private bool _isOurBase = true;
    [SerializeField] private int _maxHp = 20;
    [SerializeField] private Vector2Int _gridPosition;

    public bool IsOurBase => _isOurBase;
    public int CurrentHp { get; private set; }
    public int MaxHp => _maxHp;
    public Vector2Int GridPosition => _gridPosition;

    public UnityEvent<int, int> OnHpChanged; // current, max

    private void Start()
    {
        CurrentHp = _maxHp;
        transform.position = GridManager.Instance.GridToWorld(_gridPosition, _isOurBase);
        GridManager.Instance.GetCell(_gridPosition, _isOurBase).IsOccupied = true;
        GridManager.Instance.GetCell(_gridPosition, _isOurBase).Occupant = gameObject;
        CreateVisual();
    }

    private void CreateVisual()
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "BaseVisual";
        body.transform.parent = transform;
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.8f, 0.4f, 0.8f);
        body.GetComponent<Renderer>().material.color = _isOurBase ? Color.blue : Color.red;

        GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flag.name = "Flag";
        flag.transform.parent = transform;
        flag.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        flag.transform.localScale = new Vector3(0.15f, 0.5f, 0.15f);
        flag.GetComponent<Renderer>().material.color = _isOurBase ? Color.cyan : new Color(1f, 0.3f, 0.1f);
    }

    public void TakeDamage(int amount)
    {
        CurrentHp = Mathf.Max(0, CurrentHp - amount);
        OnHpChanged.Invoke(CurrentHp, _maxHp);
        if (CurrentHp <= 0)
        {
            Debug.Log(_isOurBase ? "我方基地被摧毁！" : "敌方基地被摧毁！");
        }
    }

    public Vector2Int GetSpawnGridPos()
    {
        // Spawn adjacent to base
        int offsetX = _isOurBase ? 1 : -1;
        return _gridPosition + new Vector2Int(offsetX, 0);
    }
}
