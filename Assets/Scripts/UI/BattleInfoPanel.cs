using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleInfoPanel : MonoBehaviour
{
    [Header("Base HP — Our")]
    [SerializeField] private Slider _ourBaseHpSlider;
    [SerializeField] private Text _ourBaseHpText;

    [Header("Base HP — Enemy")]
    [SerializeField] private Slider _enemyBaseHpSlider;
    [SerializeField] private Text _enemyBaseHpText;

    [Header("Deck Stats")]
    [SerializeField] private Text _drawPileText;
    [SerializeField] private Text _handCountText;
    [SerializeField] private Text _discardPileText;

    [Header("Card Hand")]
    [SerializeField] private Transform _cardHandContainer;
    [SerializeField] private GameObject _cardButtonPrefab;

    [Header("Enemy Wave")]
    [SerializeField] private Text _enemyWaveLabel;
    [SerializeField] private Transform _enemyWaveContainer;

    [Header("Panels")]
    [SerializeField] private GameObject _cardHandPanel;

    private List<GameObject> _cardButtons = new List<GameObject>();
    private List<GameObject> _enemyWaveButtons = new List<GameObject>();
    private Base _ourBase;
    private Base _enemyBase;

    private void Start()
    {
        FindBases();
        RefreshBaseHp();
        RefreshDeckStats();
        RefreshCardHand();
        RefreshEnemyWave();
        UpdateCardHandVisibility(GameManager.Instance != null
            ? GameManager.Instance.CurrentPhase : TurnPhase.Preparation);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged.AddListener(OnPhaseChanged);
            GameManager.Instance.OnRoundChanged.AddListener(OnRoundChanged);
        }
        if (CardDeck.Instance != null)
        {
            CardDeck.Instance.OnDeckChanged.AddListener(OnDeckChanged);
            CardDeck.Instance.OnHandChanged.AddListener(OnHandChanged);
        }
    }

    private void OnDestroy()
    {
        if (_ourBase != null)
            _ourBase.OnHpChanged.RemoveListener(OnOurBaseHpChanged);
        if (_enemyBase != null)
            _enemyBase.OnHpChanged.RemoveListener(OnEnemyBaseHpChanged);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged.RemoveListener(OnPhaseChanged);
            GameManager.Instance.OnRoundChanged.RemoveListener(OnRoundChanged);
        }
        if (CardDeck.Instance != null)
        {
            CardDeck.Instance.OnDeckChanged.RemoveListener(OnDeckChanged);
            CardDeck.Instance.OnHandChanged.RemoveListener(OnHandChanged);
        }
    }

    private void FindBases()
    {
        var bases = FindObjectsOfType<Base>();
        foreach (var b in bases)
        {
            if (b.IsOurBase)
            {
                _ourBase = b;
                _ourBase.OnHpChanged.AddListener(OnOurBaseHpChanged);
            }
            else
            {
                _enemyBase = b;
                _enemyBase.OnHpChanged.AddListener(OnEnemyBaseHpChanged);
            }
        }
    }

    private void OnOurBaseHpChanged(int current, int max)
    {
        if (_ourBaseHpSlider != null)
        {
            _ourBaseHpSlider.maxValue = max;
            _ourBaseHpSlider.value = current;
        }
        if (_ourBaseHpText != null)
            _ourBaseHpText.text = $"{current}/{max}";
    }

    private void OnEnemyBaseHpChanged(int current, int max)
    {
        if (_enemyBaseHpSlider != null)
        {
            _enemyBaseHpSlider.maxValue = max;
            _enemyBaseHpSlider.value = current;
        }
        if (_enemyBaseHpText != null)
            _enemyBaseHpText.text = $"{current}/{max}";
    }

    private void RefreshBaseHp()
    {
        if (_ourBase != null)
            OnOurBaseHpChanged(_ourBase.CurrentHp, _ourBase.MaxHp);
        if (_enemyBase != null)
            OnEnemyBaseHpChanged(_enemyBase.CurrentHp, _enemyBase.MaxHp);
    }

    private void OnDeckChanged()
    {
        RefreshDeckStats();
    }

    private void OnHandChanged()
    {
        RefreshDeckStats();
        RefreshCardHand();
    }

    private void RefreshDeckStats()
    {
        var deck = CardDeck.Instance;
        if (deck == null) return;

        if (_drawPileText != null)
            _drawPileText.text = $"牌库:{deck.DrawPileCount}";
        if (_handCountText != null)
            _handCountText.text = $"手牌:{deck.HandCount}";
        if (_discardPileText != null)
            _discardPileText.text = $"弃牌:{deck.DiscardPileCount}";
    }

    private void OnPhaseChanged(TurnPhase phase)
    {
        UpdateCardHandVisibility(phase);
        if (phase == TurnPhase.Preparation)
            RefreshEnemyWave();
    }

    private void UpdateCardHandVisibility(TurnPhase phase)
    {
        if (_cardHandPanel != null)
            _cardHandPanel.SetActive(phase == TurnPhase.Preparation);
    }

    private void RefreshCardHand()
    {
        foreach (var btn in _cardButtons)
            Destroy(btn);
        _cardButtons.Clear();

        var deck = CardDeck.Instance;
        if (deck == null || _cardHandContainer == null) return;

        var hand = deck.GetHand();
        for (int i = 0; i < hand.Count; i++)
        {
            var cardObj = Instantiate(_cardButtonPrefab, _cardHandContainer);
            var cardUI = cardObj.GetComponent<CardButtonUI>();
            if (cardUI != null)
                cardUI.Setup(hand[i]);
            _cardButtons.Add(cardObj);
        }
    }

    private void OnRoundChanged(int round)
    {
        RefreshEnemyWave();
    }

    private void RefreshEnemyWave()
    {
        foreach (var btn in _enemyWaveButtons)
            Destroy(btn);
        _enemyWaveButtons.Clear();

        if (_enemyWaveLabel != null)
        {
            int round = GameManager.Instance != null ? GameManager.Instance.CurrentRound : 1;
            _enemyWaveLabel.text = $"敌方第{round}回合部署";
        }

        var entries = EnemyAI.Instance?.GetPreviewForRound(
            GameManager.Instance != null ? GameManager.Instance.CurrentRound : 1);
        if (entries == null || entries.Length == 0 || _enemyWaveContainer == null) return;

        foreach (var entry in entries)
        {
            var cardObj = Instantiate(_cardButtonPrefab, _enemyWaveContainer);
            var cardUI = cardObj.GetComponent<CardButtonUI>();
            if (cardUI != null)
                cardUI.Setup(entry);
            _enemyWaveButtons.Add(cardObj);
        }
    }
}
