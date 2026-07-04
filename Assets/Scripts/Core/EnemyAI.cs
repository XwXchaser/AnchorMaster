using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct EnemyWaveEntry
{
    public string UnitName;
    public int Hp;
    public int Attack;
    public float AttackSpeed;
    public float AttackRange;
    public float MoveSpeed;
    public float SpawnDelay;
}

[Serializable]
public class EnemyWaveConfig
{
    public int RoundNumber;
    public EnemyWaveEntry[] Units;
}

public class EnemyAI : MonoBehaviour
{
    public static EnemyAI Instance { get; private set; }

    [SerializeField] private EnemyWaveConfig[] _waveConfigs;

    private Dictionary<int, EnemyWaveConfig> _waveMap = new Dictionary<int, EnemyWaveConfig>();

    private void Awake()
    {
        Instance = this;
        foreach (var cfg in _waveConfigs)
            _waveMap[cfg.RoundNumber] = cfg;
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
            EnqueueWaveForRound(GameManager.Instance.CurrentRound);
    }

    private void EnqueueWaveForRound(int round)
    {
        EnemyWaveConfig config = null;
        // Find config for this round, or fall back to last matching
        foreach (var cfg in _waveConfigs)
        {
            if (cfg.RoundNumber <= round)
                config = cfg;
        }

        if (config == null)
        {
            Debug.LogWarning($"[EnemyAI] No wave config for round {round}");
            return;
        }

        foreach (var entry in config.Units)
        {
            SpawnQueue.Instance?.EnqueueSpawn(new SpawnEntry
            {
                UnitName = entry.UnitName,
                IsOurUnit = false,
                SpawnDelay = entry.SpawnDelay,
                Hp = entry.Hp,
                Attack = entry.Attack,
                AttackSpeed = entry.AttackSpeed,
                AttackRange = entry.AttackRange,
                MoveSpeed = entry.MoveSpeed
            });
        }

        Debug.Log($"[EnemyAI] Round {round}: enqueued {config.Units.Length} units (wave config round {config.RoundNumber})");
    }

    public EnemyWaveEntry[] GetPreviewForRound(int round)
    {
        EnemyWaveConfig config = null;
        foreach (var cfg in _waveConfigs)
        {
            if (cfg.RoundNumber <= round)
                config = cfg;
        }
        return config?.Units ?? Array.Empty<EnemyWaveEntry>();
    }
}
