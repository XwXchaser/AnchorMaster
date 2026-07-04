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

    public UnityEvent<int, int> OnHpChanged;

    private Renderer _bodyRenderer;
    private Material _bodyMaterial;
    private Color _originalColor;
    private float _flashTimer;
    private Material _hpBarMaterial;
    private Transform _hpBarTransform;
    private float _hpBarFullWidth;

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
        _bodyRenderer = body.GetComponent<Renderer>();
        _bodyMaterial = _bodyRenderer.material;
        _originalColor = _isOurBase ? Color.blue : Color.red;
        _bodyMaterial.color = _originalColor;
        Destroy(body.GetComponent<Collider>());

        GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flag.name = "Flag";
        flag.transform.parent = transform;
        flag.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        flag.transform.localScale = new Vector3(0.15f, 0.5f, 0.15f);
        flag.GetComponent<Renderer>().material.color = _isOurBase ? Color.cyan : new Color(1f, 0.3f, 0.1f);
        Destroy(flag.GetComponent<Collider>());

        GameObject hpBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hpBar.name = "HpBar";
        hpBar.transform.parent = transform;
        hpBar.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        hpBar.transform.localScale = new Vector3(0.7f, 0.1f, 0.1f);
        var hpBarRenderer = hpBar.GetComponent<Renderer>();
        _hpBarMaterial = hpBarRenderer.material;
        _hpBarMaterial.color = Color.green;
        Destroy(hpBar.GetComponent<Collider>());
        _hpBarTransform = hpBar.transform;
        _hpBarFullWidth = 0.7f;
    }

    private void Update()
    {
        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            if (_bodyRenderer != null)
                _bodyMaterial.color = Color.Lerp(_originalColor, Color.red, _flashTimer / 0.15f);
        }
    }

    public void TakeDamage(int amount)
    {
        CurrentHp = Mathf.Max(0, CurrentHp - amount);
        _flashTimer = 0.15f;
        if (_bodyMaterial != null)
            _bodyMaterial.color = Color.red;
        UpdateHpBar();
        OnHpChanged.Invoke(CurrentHp, _maxHp);
        if (CurrentHp <= 0)
        {
            Debug.Log(_isOurBase ? "我方基地被摧毁！" : "敌方基地被摧毁！");
        }
    }

    private void UpdateHpBar()
    {
        if (_hpBarTransform == null || _hpBarMaterial == null) return;
        float ratio = (float)CurrentHp / _maxHp;
        _hpBarTransform.localScale = new Vector3(_hpBarFullWidth * ratio, 0.1f, 0.1f);
        _hpBarMaterial.color = ratio > 0.5f ? Color.green : (ratio > 0.25f ? Color.yellow : Color.red);
    }

    public Vector2Int GetSpawnGridPos()
    {
        int offsetX = _isOurBase ? 1 : -1;
        return _gridPosition + new Vector2Int(offsetX, 0);
    }
}
