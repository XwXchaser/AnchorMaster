using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CardDeck : MonoBehaviour
{
    public static CardDeck Instance { get; private set; }

    [SerializeField] private int _maxHandSize = 5;
    [SerializeField] private CardData[] _initialDeck;

    private List<CardData> _drawPile = new List<CardData>();
    private List<CardData> _hand = new List<CardData>();
    private List<CardData> _discardPile = new List<CardData>();
    private int _overdraftPenalty;

    public int MaxHandSize => _maxHandSize;
    public int HandCount => _hand.Count;
    public int DrawPileCount => _drawPile.Count;
    public int DiscardPileCount => _discardPile.Count;
    public int OverdraftPenalty => _overdraftPenalty;

    public UnityEvent OnDeckChanged;
    public UnityEvent OnHandChanged;

    private void Awake()
    {
        Instance = this;
        foreach (var card in _initialDeck)
            _drawPile.Add(card);
        Shuffle(_drawPile);
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
        if (phase == TurnPhase.Preparation)
            DrawToFillHand();
        else if (phase == TurnPhase.Battle)
            AutoDeployHand();
        else if (phase == TurnPhase.RoundEnd)
            EndRoundCleanup();
    }

    private void AutoDeployHand()
    {
        while (_hand.Count > 0)
            PlayCard(0);
    }

    public void DrawToFillHand()
    {
        int cardsToDraw = _maxHandSize - _hand.Count;
        for (int i = 0; i < cardsToDraw; i++)
            DrawCard();
        OnHandChanged.Invoke();
        OnDeckChanged.Invoke();
    }

    public bool DrawCard()
    {
        if (_hand.Count >= _maxHandSize) return false;

        if (_drawPile.Count == 0)
        {
            if (_discardPile.Count == 0) return false;
            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            Shuffle(_drawPile);
        }

        CardData card = _drawPile[0];
        _drawPile.RemoveAt(0);

        if (card.CardType == CardType.Passive)
        {
            // Passive cards auto-deploy and leave the cycle
            Debug.Log($"[CardDeck] Passive card '{card.CardName}' auto-deployed, removed from cycle");
            OnDeckChanged.Invoke();
            return DrawCard(); // Draw replacement
        }

        _hand.Add(card);
        OnHandChanged.Invoke();
        OnDeckChanged.Invoke();
        return true;
    }

    public void PlayCard(int handIndex)
    {
        if (handIndex < 0 || handIndex >= _hand.Count) return;
        CardData card = _hand[handIndex];
        _hand.RemoveAt(handIndex);

        if (card.CardType == CardType.Battle)
        {
            SpawnQueue.Instance?.EnqueueSpawn(card.ToSpawnEntry(true));
        }
        else if (card.CardType == CardType.Terrain)
        {
            // Terrain: deploy persistent effect, card removed
            Debug.Log($"[CardDeck] Terrain card '{card.CardName}' deployed");
        }

        _discardPile.Add(card);
        OnHandChanged.Invoke();
        OnDeckChanged.Invoke();
    }

    public void OverdraftCard()
    {
        if (_drawPile.Count == 0 && _discardPile.Count == 0) return;
        if (!DrawCard()) return;

        _overdraftPenalty++;
        // Last drawn card is used as overdraft
        if (_hand.Count > 0)
        {
            CardData card = _hand[_hand.Count - 1];
            _hand.RemoveAt(_hand.Count - 1);
            SpawnQueue.Instance?.EnqueueSpawn(card.ToSpawnEntry(true));
            _discardPile.Add(card);
            Debug.Log($"[CardDeck] Overdrafted '{card.CardName}', penalty: {_overdraftPenalty}");
        }
        OnHandChanged.Invoke();
        OnDeckChanged.Invoke();
    }

    public void DiscardHand()
    {
        _discardPile.AddRange(_hand);
        _hand.Clear();
        OnHandChanged.Invoke();
        OnDeckChanged.Invoke();
    }

    public void EndRoundCleanup()
    {
        DiscardHand();
        // Apply overdraft penalty: reduce next round's draw
        int penalty = _overdraftPenalty;
        _overdraftPenalty = 0;

        int drawCount = _maxHandSize - penalty;
        for (int i = 0; i < drawCount; i++)
            DrawCard();

        OnHandChanged.Invoke();
        OnDeckChanged.Invoke();
    }

    public List<CardData> GetHand() => new List<CardData>(_hand);

    private void Shuffle(List<CardData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }
}
