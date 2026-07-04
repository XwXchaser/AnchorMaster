using System.Collections.Generic;
using UnityEngine;

public class BattleResolver : MonoBehaviour
{
    public static BattleResolver Instance { get; private set; }

    [SerializeField] private bool _battleActive;

    private List<Unit> _activeUnits = new List<Unit>();

    public List<Unit> GetActiveUnits() => _activeUnits;

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
        if (phase == TurnPhase.Battle)
            StartBattle();
        else if (phase == TurnPhase.RoundEnd)
            EndBattle();
    }

    private void StartBattle()
    {
        _battleActive = true;
        _activeUnits.Clear();
        _activeUnits.AddRange(FindObjectsOfType<Unit>());
    }

    private void Update()
    {
        if (!_battleActive) return;

        _activeUnits.RemoveAll(u => u == null || u.State == UnitState.Dead);

        foreach (var unit in _activeUnits)
        {
            if (unit != null && unit.State != UnitState.Dead)
                unit.UpdateUnit(Time.deltaTime);
        }
    }

    private void EndBattle()
    {
        _battleActive = false;
        _activeUnits.RemoveAll(u => u == null || u.State == UnitState.Dead);

        bool ourAlive = false, enemyAlive = false;
        int ourSurvivors = 0, enemySurvivors = 0;
        foreach (var u in _activeUnits)
        {
            if (u.State == UnitState.Dead) continue;
            if (u.IsOurUnit) { ourAlive = true; ourSurvivors++; }
            else { enemyAlive = true; enemySurvivors++; }
        }

        if (!ourAlive && !enemyAlive) return;

        if (ourAlive && !enemyAlive)
        {
            var bases = FindObjectsOfType<Base>();
            foreach (var b in bases)
            {
                if (!b.IsOurBase)
                    b.TakeDamage(ourSurvivors * 3);
            }
        }
        else if (!ourAlive && enemyAlive)
        {
            var bases = FindObjectsOfType<Base>();
            foreach (var b in bases)
            {
                if (b.IsOurBase)
                    b.TakeDamage(enemySurvivors * 3);
            }
        }

        _activeUnits.Clear();
    }

    public void RegisterUnit(Unit unit)
    {
        if (!_activeUnits.Contains(unit))
            _activeUnits.Add(unit);
    }
}
