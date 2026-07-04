using UnityEngine;
using UnityEngine.UI;

public class BattleTimerUI : MonoBehaviour
{
    [SerializeField] private Text _timerText;
    [SerializeField] private Text _phaseText;
    [SerializeField] private Text _roundText;

    private void Update()
    {
        if (GameManager.Instance == null) return;

        int round = GameManager.Instance.CurrentRound;
        var phase = GameManager.Instance.CurrentPhase;

        if (_roundText != null)
            _roundText.text = $"回合 {round}";

        if (_phaseText != null)
            _phaseText.text = phase == TurnPhase.Preparation ? "准备阶段" :
                              phase == TurnPhase.Battle ? "战斗阶段" : "回合结束";

        if (_timerText != null)
        {
            if (phase == TurnPhase.Battle)
            {
                float remaining = GameManager.Instance.BattleTimeRemaining;
                _timerText.text = $"倒计时: {Mathf.CeilToInt(remaining)}s";
                _timerText.color = remaining <= 10f ? Color.red : Color.white;
                _timerText.enabled = true;
            }
            else
            {
                _timerText.enabled = false;
            }
        }
    }
}
