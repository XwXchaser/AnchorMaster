using UnityEngine;
using UnityEngine.Events;

public enum UnitState
{
    Spawning,
    MovingToPortal,
    MovingToBase,
    Fighting,
    Dead
}

public class Unit : MonoBehaviour
{
    [SerializeField] private string _unitName = "Unit";
    [SerializeField] private int _maxHp = 10;
    [SerializeField] private int _attack = 3;
    [SerializeField] private float _attackSpeed = 1f;
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _moveSpeed = 2f;
    [SerializeField] private float _searchRange = 3f;

    public string UnitName => _unitName;
    public int MaxHp => _maxHp;
    public int Attack => _attack;
    public float AttackSpeed => _attackSpeed;
    public float AttackRange => _attackRange;
    public float MoveSpeed => _moveSpeed;
    public float SearchRange => _searchRange;
    public int CurrentHp { get; private set; }
    public UnitState State { get; private set; }
    public bool IsOurUnit { get; private set; }
    public bool IsOnOurBoard { get; private set; }
    public Vector2Int GridPosition { get; private set; }

    public UnityEvent<UnitState, UnitState> OnStateChanged;

    private float _attackCooldown;
    private float _moveProgress;
    private Vector2Int _nextGridPos;
    private System.Collections.Generic.List<Vector2Int> _currentPath;
    private Unit _currentTarget;

    public void Initialize(bool isOurUnit, Vector2Int spawnGridPos)
    {
        IsOurUnit = isOurUnit;
        IsOnOurBoard = isOurUnit;
        GridPosition = spawnGridPos;
        CurrentHp = _maxHp;

        transform.position = GridManager.Instance.GridToWorld(spawnGridPos, IsOnOurBoard);
        GridManager.Instance.GetCell(spawnGridPos, IsOnOurBoard).IsOccupied = true;
        GridManager.Instance.GetCell(spawnGridPos, IsOnOurBoard).Occupant = gameObject;

        SetState(UnitState.MovingToPortal);
        CreateVisual();
    }

    private void CreateVisual()
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.parent = transform;
        body.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        body.transform.localScale = new Vector3(0.3f, 0.4f, 0.3f);
        body.GetComponent<Renderer>().material.color = IsOurUnit ? Color.cyan : new Color(1f, 0.4f, 0.2f);

        // HP bar placeholder
        GameObject hpBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hpBar.name = "HpBar";
        hpBar.transform.parent = transform;
        hpBar.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        hpBar.transform.localScale = new Vector3(0.5f, 0.08f, 0.08f);
        hpBar.GetComponent<Renderer>().material.color = Color.green;
    }

    public void UpdateUnit(float deltaTime)
    {
        if (State == UnitState.Dead || State == UnitState.Spawning) return;

        _attackCooldown -= deltaTime;

        switch (State)
        {
            case UnitState.MovingToPortal:
                UpdateMovement(deltaTime, true);
                break;
            case UnitState.MovingToBase:
                UpdateMovement(deltaTime, false);
                break;
            case UnitState.Fighting:
                UpdateCombat(deltaTime);
                break;
        }
    }

    private void UpdateMovement(float deltaTime, bool toPortal)
    {
        // Check for enemies in search range
        Unit enemy = FindNearestEnemy();
        if (enemy != null)
        {
            _currentTarget = enemy;
            SetState(UnitState.Fighting);
            return;
        }

        // Get target position
        Vector2Int targetGrid;
        if (toPortal)
        {
            var portal = PortalManager.Instance.GetPortalOnBoard(IsOurUnit ? PortalType.Our : PortalType.Enemy, IsOnOurBoard);
            targetGrid = portal.GridPosition;
        }
        else
        {
            // Moving to enemy base - find it
            var bases = FindObjectsOfType<Base>();
            Base targetBase = null;
            foreach (var b in bases)
            {
                if (b.IsOurBase != IsOurUnit) targetBase = b;
            }
            if (targetBase == null) return;
            targetGrid = targetBase.GridPosition;
        }

        // Recalculate path if needed
        if (_currentPath == null || _currentPath.Count == 0)
        {
            _currentPath = Pathfinding.FindPath(GridPosition, targetGrid, IsOnOurBoard);
            _moveProgress = 0f;
            if (_currentPath != null && _currentPath.Count > 0)
                _nextGridPos = _currentPath[0];
        }

        if (_currentPath == null || _currentPath.Count == 0) return;

        _moveProgress += _moveSpeed * deltaTime;
        if (_moveProgress >= 1f)
        {
            _moveProgress -= 1f;
            MoveToNextCell(toPortal);
        }
        else
        {
            // Smooth movement between cells
            Vector3 from = GridManager.Instance.GridToWorld(GridPosition, IsOnOurBoard);
            Vector3 to = GridManager.Instance.GridToWorld(_nextGridPos, IsOnOurBoard);
            transform.position = Vector3.Lerp(from, to, _moveProgress);
        }
    }

    private void MoveToNextCell(bool toPortal)
    {
        if (_currentPath == null || _currentPath.Count == 0) return;

        // Free old cell
        GridManager.Instance.GetCell(GridPosition, IsOnOurBoard).IsOccupied = false;
        GridManager.Instance.GetCell(GridPosition, IsOnOurBoard).Occupant = null;

        // Occupy new cell
        GridPosition = _nextGridPos;
        GridManager.Instance.GetCell(GridPosition, IsOnOurBoard).IsOccupied = true;
        GridManager.Instance.GetCell(GridPosition, IsOnOurBoard).Occupant = gameObject;

        _currentPath.RemoveAt(0);

        if (_currentPath.Count > 0)
        {
            _nextGridPos = _currentPath[0];
        }
        else
        {
            // Reached destination
            if (toPortal)
            {
                Teleport();
            }
            else
            {
                AttackBase();
            }
        }
    }

    private void Teleport()
    {
        // Free old cell
        GridManager.Instance.GetCell(GridPosition, IsOnOurBoard).IsOccupied = false;
        GridManager.Instance.GetCell(GridPosition, IsOnOurBoard).Occupant = null;

        // Switch board
        IsOnOurBoard = !IsOnOurBoard;
        var portal = PortalManager.Instance.GetPortalOnBoard(IsOurUnit ? PortalType.Our : PortalType.Enemy, IsOnOurBoard);
        GridPosition = portal.GridPosition;

        // Occupy new cell
        GridManager.Instance.GetCell(GridPosition, IsOnOurBoard).IsOccupied = true;
        GridManager.Instance.GetCell(GridPosition, IsOnOurBoard).Occupant = gameObject;

        transform.position = GridManager.Instance.GridToWorld(GridPosition, IsOnOurBoard);
        _currentPath = null;
        SetState(UnitState.MovingToBase);
    }

    private void UpdateCombat(float deltaTime)
    {
        if (_currentTarget == null || _currentTarget.State == UnitState.Dead)
        {
            _currentTarget = FindNearestEnemy();
            if (_currentTarget == null)
            {
                SetState(IsOnOurBoard ? UnitState.MovingToPortal : UnitState.MovingToBase);
                return;
            }
        }

        float dist = Vector3.Distance(transform.position, _currentTarget.transform.position);
        if (dist > _searchRange)
        {
            _currentTarget = null;
            SetState(IsOnOurBoard ? UnitState.MovingToPortal : UnitState.MovingToBase);
            return;
        }

        if (dist <= _attackRange && _attackCooldown <= 0f)
        {
            _currentTarget.TakeDamage(_attack);
            _attackCooldown = 1f / _attackSpeed;
        }
    }

    private Unit FindNearestEnemy()
    {
        Unit best = null;
        float bestDist = _searchRange;
        var allUnits = FindObjectsOfType<Unit>();
        foreach (var u in allUnits)
        {
            if (u == this || u.State == UnitState.Dead) continue;
            if (u.IsOurUnit == IsOurUnit) continue;
            if (u.IsOnOurBoard != IsOnOurBoard) continue;

            float d = Vector3.Distance(transform.position, u.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = u;
            }
        }
        return best;
    }

    public void TakeDamage(int amount)
    {
        CurrentHp -= amount;
        if (CurrentHp <= 0)
        {
            CurrentHp = 0;
            Die();
        }
    }

    private void Die()
    {
        GridManager.Instance.GetCell(GridPosition, IsOnOurBoard).IsOccupied = false;
        GridManager.Instance.GetCell(GridPosition, IsOnOurBoard).Occupant = null;
        SetState(UnitState.Dead);
        Destroy(gameObject, 0.5f);
    }

    private void AttackBase()
    {
        var bases = FindObjectsOfType<Base>();
        Base targetBase = null;
        foreach (var b in bases)
        {
            if (b.IsOurBase != IsOurUnit) targetBase = b;
        }
        if (targetBase != null)
            targetBase.TakeDamage(_attack);
        Die();
    }

    private void SetState(UnitState newState)
    {
        if (State == newState) return;
        var old = State;
        State = newState;
        OnStateChanged.Invoke(old, newState);
    }

    private void OnDestroy()
    {
        if (GridManager.Instance != null && GridManager.Instance.GetCell(GridPosition, IsOnOurBoard) != null)
        {
            var cell = GridManager.Instance.GetCell(GridPosition, IsOnOurBoard);
            if (cell.Occupant == gameObject)
            {
                cell.IsOccupied = false;
                cell.Occupant = null;
            }
        }
    }
}
