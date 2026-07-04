using System.Collections.Generic;
using UnityEngine;

public struct SpawnEntry
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
    private int _totalSpawnedThisRound;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPhaseChanged.AddListener(OnPhaseChanged);
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPhaseChanged.RemoveListener(OnPhaseChanged);
    }

    private void OnPhaseChanged(TurnPhase phase)
    {
        if (phase == TurnPhase.Battle)
        {
            _isSpawning = true;
            _totalSpawnedThisRound = 0;
            _activeTimers.Clear();
            for (int i = 0; i < _pendingSpawns.Count; i++)
            {
                var entry = _pendingSpawns[i];
                entry.Timer = entry.SpawnDelay;
                _activeTimers.Add(entry);
            }
            _pendingSpawns.Clear();
            Debug.Log($"[SpawnQueue] Battle phase: {_activeTimers.Count} spawns queued");
        }
        else if (phase == TurnPhase.RoundEnd)
        {
            _isSpawning = false;
            _pendingSpawns.Clear();
            _activeTimers.Clear();
        }
    }

    private void Update()
    {
        if (!_isSpawning) return;
        if (_activeTimers.Count == 0)
        {
            _isSpawning = false;
            return;
        }

        float dt = Time.deltaTime;
        for (int i = _activeTimers.Count - 1; i >= 0; i--)
        {
            var entry = _activeTimers[i];
            entry.Timer -= dt;
            _activeTimers[i] = entry;

            if (entry.Timer <= 0f)
            {
                _totalSpawnedThisRound++;
                if (_totalSpawnedThisRound > 50)
                {
                    Debug.LogError($"[SpawnQueue] ABORT: spawned {_totalSpawnedThisRound} units, something wrong!");
                    _isSpawning = false;
                    _activeTimers.Clear();
                    return;
                }
                try
                {
                    SpawnUnit(entry);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SpawnQueue] SpawnUnit failed: {e.Message}");
                }
                _activeTimers.RemoveAt(i);
            }
        }
    }

    private void SpawnUnit(SpawnEntry entry)
    {
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

        Vector2Int spawnPos = spawnBase.GridPosition;
        Debug.Log($"[SpawnQueue] Spawning {entry.UnitName} ourUnit={entry.IsOurUnit}, base={spawnBase.name} grid={spawnBase.GridPosition}, spawnGrid={spawnPos}");

        GameObject unitObj;
        if (_unitPrefab != null)
            unitObj = Instantiate(_unitPrefab);
        else
            unitObj = new GameObject(entry.UnitName);

        unitObj.name = entry.UnitName;
        var unit = unitObj.GetComponent<Unit>();
        if (unit == null) unit = unitObj.AddComponent<Unit>();

        unit.Initialize(entry.IsOurUnit, spawnPos,
            entry.UnitName, entry.Hp, entry.Attack,
            entry.AttackSpeed, entry.AttackRange, entry.MoveSpeed);
        BattleResolver.Instance?.RegisterUnit(unit);
    }

    public void EnqueueSpawn(SpawnEntry entry)
    {
        _pendingSpawns.Add(entry);
    }

    public int PendingCount => _pendingSpawns.Count;
    public int ActiveCount => _activeTimers.Count;

    public void ClearQueue()
    {
        _pendingSpawns.Clear();
        _activeTimers.Clear();
    }
}
