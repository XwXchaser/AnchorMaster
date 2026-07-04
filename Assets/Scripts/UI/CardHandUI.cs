using System.Collections.Generic;
using UnityEngine;

public class CardHandUI : MonoBehaviour
{
    [SerializeField] private float _cardWidth = 120f;
    [SerializeField] private float _cardHeight = 80f;
    [SerializeField] private float _cardSpacing = 10f;
    [SerializeField] private float _hoverLift = 30f;
    [SerializeField] private float _bottomMargin = 20f;

    private int _hoveredIndex = -1;

    private void OnGUI()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentPhase != TurnPhase.Preparation) return;

        CardDeck deck = CardDeck.Instance;
        if (deck == null) return;

        List<CardData> hand = deck.GetHand();
        if (hand.Count == 0) return;

        float totalWidth = hand.Count * _cardWidth + (hand.Count - 1) * _cardSpacing;
        float startX = (Screen.width - totalWidth) / 2f;
        float baseY = Screen.height - _cardHeight - _bottomMargin;

        Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        _hoveredIndex = -1;

        for (int i = 0; i < hand.Count; i++)
        {
            float x = startX + i * (_cardWidth + _cardSpacing);
            float y = baseY;
            Rect cardRect = new Rect(x, y, _cardWidth, _cardHeight);

            if (cardRect.Contains(mousePos))
            {
                _hoveredIndex = i;
                y -= _hoverLift;
            }

            // Card background
            Color bgColor = hand[i].CardType == CardType.Battle ? new Color(0.25f, 0.35f, 0.55f)
                : hand[i].CardType == CardType.Terrain ? new Color(0.35f, 0.45f, 0.25f)
                : new Color(0.45f, 0.35f, 0.25f);

            GUI.color = bgColor;
            GUI.Box(new Rect(x, y, _cardWidth, _cardHeight), "");

            // Card name
            GUI.color = Color.white;
            GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
            nameStyle.alignment = TextAnchor.MiddleCenter;
            nameStyle.fontStyle = FontStyle.Bold;
            nameStyle.fontSize = 14;
            GUI.Label(new Rect(x, y + 5, _cardWidth, 20), hand[i].CardName, nameStyle);

            // Card type
            GUIStyle typeStyle = new GUIStyle(GUI.skin.label);
            typeStyle.alignment = TextAnchor.MiddleCenter;
            typeStyle.fontSize = 11;
            typeStyle.normal.textColor = Color.gray;
            GUI.Label(new Rect(x, y + 25, _cardWidth, 16), hand[i].CardType.ToString(), typeStyle);

            // Click to play
            if (_hoveredIndex == i && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                deck.PlayCard(i);
                Event.current.Use();
            }
        }

        // Draw pile count
        GUI.color = Color.white;
        GUIStyle pileStyle = new GUIStyle(GUI.skin.label);
        pileStyle.fontSize = 12;
        GUI.Label(new Rect(startX - 60, baseY + _cardHeight - 20, 50, 20), $"牌库:{deck.DrawPileCount}", pileStyle);
        GUI.Label(new Rect(startX + totalWidth + 10, baseY + _cardHeight - 20, 50, 20), $"弃牌:{deck.DiscardPileCount}", pileStyle);
    }

    public int HoveredIndex => _hoveredIndex;
}
