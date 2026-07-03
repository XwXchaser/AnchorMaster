using UnityEngine;
using UnityEngine.Events;

public enum TurnPhase
{
    Preparation,
    Battle,
    RoundEnd
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private TurnPhase _currentPhase = TurnPhase.Preparation;
    [SerializeField] private int _currentRound = 1;
    [SerializeField] private float _battleTimeLimit = 60f;

    public TurnPhase CurrentPhase => _currentPhase;
    public int CurrentRound => _currentRound;
    public float BattleTimeRemaining { get; private set; }

    public UnityEvent<TurnPhase> OnPhaseChanged;
    public UnityEvent<int> OnRoundChanged;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        EnterPreparation();
    }

    private void Update()
    {
        if (_currentPhase == TurnPhase.Battle)
        {
            BattleTimeRemaining -= Time.deltaTime;
            if (BattleTimeRemaining <= 0f)
            {
                BattleTimeRemaining = 0f;
                EndBattle();
            }
        }
    }

    public void EndPreparation()
    {
        if (_currentPhase != TurnPhase.Preparation) return;
        _currentPhase = TurnPhase.Battle;
        BattleTimeRemaining = _battleTimeLimit;
        OnPhaseChanged.Invoke(TurnPhase.Battle);
    }

    public void EndBattle()
    {
        if (_currentPhase != TurnPhase.Battle) return;
        _currentPhase = TurnPhase.RoundEnd;
        OnPhaseChanged.Invoke(TurnPhase.RoundEnd);
    }

    public void NextRound()
    {
        if (_currentPhase != TurnPhase.RoundEnd) return;
        _currentRound++;
        OnRoundChanged.Invoke(_currentRound);
        EnterPreparation();
    }

    private void EnterPreparation()
    {
        _currentPhase = TurnPhase.Preparation;
        OnPhaseChanged.Invoke(TurnPhase.Preparation);
    }
}
