using UnityEngine;
using UnityEngine.UI;

public class CardButtonUI : MonoBehaviour
{
    [SerializeField] private Image _background;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _typeText;
    [SerializeField] private Text _statsText;

    public void Setup(CardData data)
    {
        if (_nameText != null)
            _nameText.text = data.CardName;
        if (_typeText != null)
            _typeText.text = data.CardType.ToString();

        if (_statsText != null)
        {
            if (data.CardType == CardType.Battle)
                _statsText.text = $"<b>{data.UnitName}</b>\nHP:{data.Hp} ATK:{data.Attack}";
            else if (data.CardType == CardType.Terrain)
                _statsText.text = data.Description;
            else
                _statsText.text = data.Description;
        }

        if (_background != null)
        {
            Color c = data.CardType == CardType.Battle ? new Color(0.25f, 0.35f, 0.55f)
                : data.CardType == CardType.Terrain ? new Color(0.35f, 0.45f, 0.25f)
                : new Color(0.45f, 0.35f, 0.25f);
            _background.color = c;
        }
    }

    public void Setup(EnemyWaveEntry entry)
    {
        if (_nameText != null)
            _nameText.text = entry.UnitName;
        if (_typeText != null)
            _typeText.text = "Battle";

        if (_statsText != null)
            _statsText.text = $"HP:{entry.Hp} ATK:{entry.Attack}\nAS:{entry.AttackSpeed} MS:{entry.MoveSpeed}";

        if (_background != null)
            _background.color = new Color(0.55f, 0.2f, 0.2f);
    }
}
