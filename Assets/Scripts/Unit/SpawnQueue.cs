using System.Collections.Generic;
using UnityEngine;

public class SpawnEntry
{
    public string UnitName;
    public bool IsOurUnit;
    public float SpawnDelay;
    public int Hp;
    public int Attack;
    public float AttackSpeed;
    public float AttackRange;
    public float MoveSpeed;
    public float Timer;
}

public class SpawnQueue : MonoBehaviour
{
    public static SpawnQueue Instance { get; private set; }

    [SerializeField] private GameObject _unitPrefab;

    private List<SpawnEntry> _pendingSpawns = new List<SpawnEntry>();
    private List<SpawnEntry> _activeTimers = new List<SpawnEntry>();
    private bool _isSpawning;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        GameManager.Instance.OnPhaseChanged.AddListener(OnPhaseChanged);
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPhaseChanged.RemoveListener(OnPhaseChanged);
    }

    private void OnPhaseChanged(TurnPhase phase)
    {
        Debug.Log($"[SpawnQueue] OnPhaseChanged: {phase}, pending: {_pendingSpawns.Count}");
        if (phase == TurnPhase.Battle)
        {
            _isSpawning = true;
            _activeTimers.Clear();
            for (int i = 0; i < _pendingSpawns.Count; i++)
            {
                var entry = _pendingSpawns[i];
                entry.Timer = entry.SpawnDelay;
                _activeTimers.Add(entry);
            }
            _pendingSpawns.Clear();
        }
        else if (phase == TurnPhase.RoundEnd)
        {
            _isSpawning = false;
            _pendingSpawns.Clear();
            _activeTimers.Clear();
        }
    }

    private int _frameCount;
    private void Update()
    {
        if (!_isSpawning) return;

        _frameCount++;
        if (_frameCount <= 5)
            Debug.Log($"[SpawnQueue] Update frame {_frameCount}, activeTimers: {_activeTimers.Count}");

        float dt = Time.deltaTime;
        for (int i = _activeTimers.Count - 1; i >= 0; i--)
        {
            _activeTimers[i].Timer -= dt;
            if (_activeTimers[i].Timer <= 0f)
            {
                SpawnUnit(_activeTimers[i]);
                _activeTimers.RemoveAt(i);
            }
        }
    }

    private void SpawnUnit(SpawnEntry entry)
    {
        Debug.Log($"[SpawnQueue] Spawning {entry.UnitName}, activeTimers left: {_activeTimers.Count}");
        var bases = FindObjectsOfType<Base>();
        Base spawnBase = null;
        foreach (var b in bases)
        {
            if (b.IsOurBase == entry.IsOurUnit) spawnBase = b;
        }
        if (spawnBase == null)
        {
            Debug.LogError($"No base found for {(entry.IsOurUnit ? "our" : "enemy")} unit");
            return;
        }

        Vector2Int spawnPos = spawnBase.GetSpawnGridPos();
        if (GridManager.Instance.GetCell(spawnPos, entry.IsOurUnit).IsOccupied)
            spawnPos = FindFreeAdjacent(spawnPos, entry.IsOurUnit);

        GameObject unitObj;
        if (_unitPrefab != null)
            unitObj = Instantiate(_unitPrefab);
        else
            unitObj = new GameObject(entry.UnitName);

        unitObj.name = entry.UnitName;
        var unit = unitObj.GetComponent<Unit>();
        if (unit == null) unit = unitObj.AddComponent<Unit>();

        SetSerializedField(unit, "_unitName", entry.UnitName);
        SetSerializedField(unit, "_maxHp", entry.Hp);
        SetSerializedField(unit, "_attack", entry.Attack);
        SetSerializedField(unit, "_attackSpeed", entry.AttackSpeed);
        SetSerializedField(unit, "_attackRange", entry.AttackRange);
        SetSerializedField(unit, "_moveSpeed", entry.MoveSpeed);

        unit.Initialize(entry.IsOurUnit, spawnPos);
        BattleResolver.Instance?.RegisterUnit(unit);
    }

    private Vector2Int FindFreeAdjacent(Vector2Int pos, bool isOurBoard)
    {
        var offsets = new[] {
            new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1),
            new Vector2Int(1,1), new Vector2Int(-1,1), new Vector2Int(1,-1), new Vector2Int(-1,-1)
        };
        foreach (var off in offsets)
        {
            var test = pos + off;
            if (GridManager.Instance.IsInBounds(test) && !GridManager.Instance.GetCell(test, isOurBoard).IsOccupied)
                return test;
        }
        return pos;
    }

    private void SetSerializedField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(obj, value);
    }

    public void EnqueueSpawn(SpawnEntry entry)
    {
        _pendingSpawns.Add(entry);
    }

    public void ClearQueue()
    {
        _pendingSpawns.Clear();
        _activeTimers.Clear();
    }
}
