using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum CardSlotRole
{
    Cause,
    Present,
    Advice
}

[DisallowMultipleComponent]
public class CardSelectionSlot : MonoBehaviour, IDropHandler
{
    [Header("슬롯 정보")]
    [SerializeField] private CardSlotRole role;
    [SerializeField] private string displayName;

    [Header("UI 참조")]
    [SerializeField] private CardDeck cardDeck;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Image background;

    public CardSlotRole Role => role;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? RoleToDisplayName(role) : displayName;
    public CardView CurrentCard { get; private set; }

    public void Initialize(CardDeck deck, CardSlotRole newRole)
    {
        cardDeck = deck;
        role = newRole;
        displayName = RoleToDisplayName(role);
        if (background == null) background = GetComponent<Image>();
        RefreshLabel();
    }

    public void SetCard(CardView card)
    {
        CurrentCard = card;
        RefreshLabel();
    }

    public void ClearCard(CardView card = null)
    {
        if (card != null && CurrentCard != card) return;

        CurrentCard = null;
        RefreshLabel();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (cardDeck == null || eventData.pointerDrag == null) return;

        CardView card = eventData.pointerDrag.GetComponent<CardView>();
        if (card != null) cardDeck.PlaceCardInSlot(card, this);
    }

    private void RefreshLabel()
    {
        if (labelText != null) labelText.text = DisplayName;
        if (background != null)
        {
            background.color = CurrentCard == null
                ? new Color(1f, 1f, 1f, 0.06f)
                : new Color(1f, 1f, 1f, 0.18f);
        }
    }

    private static string RoleToDisplayName(CardSlotRole slotRole)
    {
        switch (slotRole)
        {
            case CardSlotRole.Cause:
                return "원인";
            case CardSlotRole.Present:
                return "현재";
            case CardSlotRole.Advice:
                return "조언";
            default:
                return string.Empty;
        }
    }
}
