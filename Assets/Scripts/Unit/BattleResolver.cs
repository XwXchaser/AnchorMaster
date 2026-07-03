using System.Collections.Generic;
using UnityEngine;

public class BattleResolver : MonoBehaviour
{
    public static BattleResolver Instance { get; private set; }

    [SerializeField] private bool _battleActive;

    private List<Unit> _activeUnits = new List<Unit>();

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
        _activeUnits.Clear();

        // Check if any side has remaining units
        var allUnits = FindObjectsOfType<Unit>();
        bool ourAlive = false, enemyAlive = false;
        foreach (var u in allUnits)
        {
            if (u.State == UnitState.Dead) continue;
            if (u.IsOurUnit) ourAlive = true;
            else enemyAlive = true;
        }

        if (!ourAlive && !enemyAlive) return; // Both dead, no damage

        // If one side has survivors and the other doesn't, survivors damage enemy base
        if (ourAlive && !enemyAlive)
        {
            Debug.Log("我方存活单位对敌方基地造成伤害！");
        }
        else if (!ourAlive && enemyAlive)
        {
            Debug.Log("敌方存活单位对我方基地造成伤害！");
        }
    }

    public void RegisterUnit(Unit unit)
    {
        if (!_activeUnits.Contains(unit))
            _activeUnits.Add(unit);
    }
}
