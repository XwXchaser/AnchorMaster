using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public enum UnitState
{
    Spawning,
    MovingToPortal,
    MovingToBase,
    Fighting,
    Dead
}

[RequireComponent(typeof(NavMeshAgent))]
public class Unit : MonoBehaviour
{
    [SerializeField] private string _unitName = "Unit";
    [SerializeField] private int _maxHp = 10;
    [SerializeField] private int _attack = 3;
    [SerializeField] private float _attackSpeed = 1f;
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _moveSpeed = 1.5f;
    [SerializeField] private float _searchRange = 4f;
    [SerializeField] private float _unitRadius = 0.3f;

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

    public UnityEvent<UnitState, UnitState> OnStateChanged;

    private NavMeshAgent _agent;
    private float _attackCooldown;
    private Unit _currentTarget;
    private float _diagTimer = 2f;
    private Renderer _bodyRenderer;
    private Material _bodyMaterial;
    private Color _originalColor;
    private float _flashTimer;
    private Transform _hpBarTransform;
    private Renderer _hpBarRenderer;
    private Material _hpBarMaterial;
    private float _hpBarFullWidth;

    public void Initialize(bool isOurUnit, Vector2Int spawnGridPos,
        string unitName = null, int hp = -1, int attack = -1,
        float attackSpeed = -1f, float attackRange = -1f, float moveSpeed = -1f)
    {
        IsOurUnit = isOurUnit;
        IsOnOurBoard = isOurUnit;

        if (unitName != null) _unitName = unitName;
        if (hp > 0) _maxHp = hp;
        if (attack > 0) _attack = attack;
        if (attackSpeed > 0f) _attackSpeed = attackSpeed;
        if (attackRange > 0f) _attackRange = attackRange;
        if (moveSpeed > 0f) _moveSpeed = moveSpeed;

        CurrentHp = _maxHp;

        _agent = GetComponent<NavMeshAgent>();
        _agent.radius = _unitRadius;
        _agent.speed = _moveSpeed;
        _agent.acceleration = 20f;
        _agent.angularSpeed = 720f;
        _agent.stoppingDistance = 0.1f;
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        _agent.autoBraking = true;

        int ourBoardLayer = LayerMask.NameToLayer("OurBoard");
        int enemyBoardLayer = LayerMask.NameToLayer("EnemyBoard");
        gameObject.layer = IsOnOurBoard ? ourBoardLayer : enemyBoardLayer;
        _agent.areaMask = IsOnOurBoard ? 1 : (1 << 3);

        Vector3 worldPos = GridManager.Instance.GridToWorld(spawnGridPos, IsOnOurBoard);
        int myAreaMask = IsOnOurBoard ? 1 : (1 << 3);
        var filter = new NavMeshQueryFilter { areaMask = myAreaMask, agentTypeID = 0 };
        if (NavMesh.SamplePosition(worldPos, out NavMeshHit hit, 2f, filter))
            worldPos = hit.position;
        else
            Debug.LogError($"[Unit] {_unitName} Init: no NavMesh near {worldPos}, areaMask={myAreaMask}");
        
        transform.position = worldPos;
        _agent.Warp(worldPos);

        Debug.Log($"[Unit] {_unitName} spawned: ourUnit={IsOurUnit}, onOurBoard={IsOnOurBoard}, pos={worldPos}, layer={LayerMask.LayerToName(gameObject.layer)}");

        CreateVisual();
        SetDestinationToPortal();
    }

    private void CreateVisual()
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.parent = transform;
        body.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        body.transform.localScale = new Vector3(0.3f, 0.4f, 0.3f);
        _bodyRenderer = body.GetComponent<Renderer>();
        _bodyMaterial = _bodyRenderer.material;
        _originalColor = IsOurUnit ? Color.cyan : new Color(1f, 0.4f, 0.2f);
        _bodyMaterial.color = _originalColor;
        Destroy(body.GetComponent<Collider>());

        GameObject hpBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hpBar.name = "HpBar";
        hpBar.transform.parent = transform;
        hpBar.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        hpBar.transform.localScale = new Vector3(0.5f, 0.08f, 0.08f);
        _hpBarRenderer = hpBar.GetComponent<Renderer>();
        _hpBarMaterial = _hpBarRenderer.material;
        _hpBarMaterial.color = Color.green;
        Destroy(hpBar.GetComponent<Collider>());
        _hpBarTransform = hpBar.transform;
        _hpBarFullWidth = 0.5f;
    }

    public void UpdateUnit(float deltaTime)
    {
        if (_flashTimer > 0f)
        {
            _flashTimer -= deltaTime;
            if (_bodyMaterial != null)
                _bodyMaterial.color = Color.Lerp(_originalColor, Color.red, _flashTimer / 0.15f);
        }

        if (State == UnitState.Dead || State == UnitState.Spawning) return;

        _diagTimer -= deltaTime;
        if (_diagTimer <= 0f)
        {
            _diagTimer = 2f;
            string pathStatus = _agent.hasPath ? _agent.pathStatus.ToString() : "noPath";
            float rem = _agent.hasPath ? _agent.remainingDistance : -1f;
            Portal portal = PortalManager.Instance?.GetPortalOnBoard(
                IsOurUnit ? PortalType.Our : PortalType.Enemy, IsOnOurBoard);
            float portalDist = portal != null ? Vector3.Distance(transform.position, portal.transform.position) : -1f;
            Debug.Log($"[Unit] {_unitName} diag: state={State}, ourUnit={IsOurUnit}, onOurBoard={IsOnOurBoard}, pos={transform.position}, " +
                $"pathStatus={pathStatus}, remDist={rem:F2}, portalDist={portalDist:F2}, agentVel={_agent.velocity.magnitude:F2}, " +
                $"agentDest={_agent.destination}, agentPathPending={_agent.pathPending}, agentHasPath={_agent.hasPath}");
        }

        _attackCooldown -= deltaTime;

        if (State == UnitState.Fighting)
        {
            UpdateCombat(deltaTime);
            return;
        }

        Unit enemy = FindNearestEnemy();
        if (enemy != null)
        {
            _currentTarget = enemy;
            SetState(UnitState.Fighting);
            return;
        }

        // Always check portal/base proximity regardless of agent state
        if (State == UnitState.MovingToPortal)
            CheckPortalEntry();
        else if (State == UnitState.MovingToBase)
            CheckBaseReached();
    }

    private void CheckPortalEntry()
    {
        Portal targetPortal = PortalManager.Instance.GetPortalOnBoard(
            IsOurUnit ? PortalType.Our : PortalType.Enemy, IsOnOurBoard);
        if (targetPortal == null) return;

        float dist = Vector3.Distance(transform.position, targetPortal.transform.position);
        if (dist < 1.2f)
        {
            Debug.Log($"[Unit] {_unitName} CheckPortalEntry: dist={dist:F2}, teleporting!");
            Teleport();
        }
    }

    private void CheckBaseReached()
    {
        var bases = FindObjectsOfType<Base>();
        Base targetBase = null;
        foreach (var b in bases)
        {
            if (b.IsOurBase != IsOurUnit) targetBase = b;
        }
        if (targetBase == null) return;

        float dist = Vector3.Distance(transform.position, targetBase.transform.position);
        if (dist < 1.2f)
            AttackBase();
    }

    private void UpdateCombat(float deltaTime)
    {
        if (_currentTarget == null || _currentTarget.State == UnitState.Dead)
        {
            _currentTarget = FindNearestEnemy();
            if (_currentTarget == null)
            {
                _agent.isStopped = false;
                ResumeMovement();
                return;
            }
        }

        float dist = Vector3.Distance(transform.position, _currentTarget.transform.position);
        if (dist > _searchRange * 1.5f)
        {
            _currentTarget = null;
            _agent.isStopped = false;
            ResumeMovement();
            return;
        }

        if (dist <= _attackRange)
        {
            _agent.isStopped = true;
            if (_attackCooldown <= 0f)
            {
                _currentTarget.TakeDamage(_attack);
                _attackCooldown = 1f / _attackSpeed;
            }
        }
        else
        {
            _agent.isStopped = false;
            Vector3 targetPos = _currentTarget.transform.position;
            if (Vector3.Distance(_agent.destination, targetPos) > 0.5f)
                _agent.SetDestination(targetPos);
        }
    }

    private void ResumeMovement()
    {
        if (IsOurUnit == IsOnOurBoard)
            SetDestinationToPortal();
        else
            SetDestinationToBase();
    }

    private void SetDestinationToPortal()
    {
        Portal portal = PortalManager.Instance.GetPortalOnBoard(
            IsOurUnit ? PortalType.Our : PortalType.Enemy, IsOnOurBoard);
        if (portal != null)
        {
            _agent.enabled = true;
            _agent.isStopped = false;
            bool ok = _agent.SetDestination(portal.transform.position);
            Debug.Log($"[Unit] {_unitName} SetDestToPortal: portalPos={portal.transform.position}, myBoard={IsOnOurBoard}, agentOk={ok}, agentEnabled={_agent.enabled}, agentStopped={_agent.isStopped}, agentOnNavMesh={_agent.isOnNavMesh}");
            SetState(UnitState.MovingToPortal);
        }
        else
        {
            Debug.LogError($"[Unit] {_unitName} NO PORTAL FOUND! ourUnit={IsOurUnit}, onOurBoard={IsOnOurBoard}");
        }
    }

    private void SetDestinationToBase()
    {
        var bases = FindObjectsOfType<Base>();
        Base targetBase = null;
        foreach (var b in bases)
        {
            if (b.IsOurBase != IsOurUnit) targetBase = b;
        }
        if (targetBase != null)
        {
            _agent.enabled = true;
            _agent.isStopped = false;
            bool ok = _agent.SetDestination(targetBase.transform.position);
            Debug.Log($"[Unit] {_unitName} SetDestToBase: basePos={targetBase.transform.position}, myBoard={IsOnOurBoard}, agentOk={ok}, agentEnabled={_agent.enabled}, agentStopped={_agent.isStopped}, agentOnNavMesh={_agent.isOnNavMesh}");
            SetState(UnitState.MovingToBase);
        }
        else
        {
            Debug.LogError($"[Unit] {_unitName} NO TARGET BASE! ourUnit={IsOurUnit}");
        }
    }

    public void Teleport()
    {
        Portal exitPortal = PortalManager.Instance.GetPortalOnBoard(
            IsOurUnit ? PortalType.Our : PortalType.Enemy, !IsOnOurBoard);
        if (exitPortal == null)
        {
            Debug.LogError($"[Unit] {_unitName} Teleport: NO EXIT PORTAL! ourUnit={IsOurUnit}, fromBoard={IsOnOurBoard}");
            return;
        }

        Debug.Log($"[Unit] {_unitName} Teleport: {IsOnOurBoard} -> {!IsOnOurBoard}, exitPos={exitPortal.transform.position}");

        IsOnOurBoard = !IsOnOurBoard;
        gameObject.layer = IsOnOurBoard ? LayerMask.NameToLayer("OurBoard") : LayerMask.NameToLayer("EnemyBoard");
        _agent.areaMask = IsOnOurBoard ? 1 : (1 << 3);
        Vector3 targetPos = exitPortal.transform.position;

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            targetPos = hit.position;

        _agent.Warp(targetPos);
        Debug.Log($"[Unit] {_unitName} Teleport Warp done: pos={transform.position}, agentOnNavMesh={_agent.isOnNavMesh}, agentPos={_agent.nextPosition}");
        SetDestinationToBase();
    }

    private Unit FindNearestEnemy()
    {
        Unit best = null;
        float bestDist = _searchRange;
        var activeList = BattleResolver.Instance?.GetActiveUnits();
        System.Collections.Generic.IEnumerable<Unit> allUnits = activeList != null
            ? activeList
            : FindObjectsOfType<Unit>();

        foreach (var u in allUnits)
        {
            if (u == null || u == this || u.State == UnitState.Dead) continue;
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
        _flashTimer = 0.15f;
        if (_bodyMaterial != null)
            _bodyMaterial.color = Color.red;
        UpdateHpBar();
        if (CurrentHp <= 0)
        {
            CurrentHp = 0;
            Die();
        }
    }

    private void UpdateHpBar()
    {
        if (_hpBarTransform == null) return;
        float ratio = (float)CurrentHp / _maxHp;
        var scale = _hpBarTransform.localScale;
        scale.x = _hpBarFullWidth * ratio;
        _hpBarTransform.localScale = scale;
        if (_hpBarMaterial != null)
            _hpBarMaterial.color = Color.Lerp(Color.red, Color.green, ratio);
    }

    private void Die()
    {
        SetState(UnitState.Dead);
        Destroy(gameObject, 0.5f);
    }

    private void AttackBase()
    {
        if (_attackCooldown > 0f) return;

        var bases = FindObjectsOfType<Base>();
        Base targetBase = null;
        foreach (var b in bases)
        {
            if (b.IsOurBase != IsOurUnit) targetBase = b;
        }
        if (targetBase != null)
        {
            targetBase.TakeDamage(_attack);
            _attackCooldown = 1f / _attackSpeed;
        }
    }

    private void SetState(UnitState newState)
    {
        if (State == newState) return;
        var old = State;
        State = newState;
        OnStateChanged?.Invoke(old, newState);
    }
}
