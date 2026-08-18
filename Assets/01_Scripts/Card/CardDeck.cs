using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// 카드 덱 화면의 총괄자: 카드 드로우, 드래그 앤 드롭 슬롯 배치, 3장 선택 제한, 결정 버튼 활성화를 관리한다.
public class CardDeck : MonoBehaviour
{
    private const int DeckCardCount = 6;
    private const int MaxSelectable = 3;

    [Header("카드 수집/펼치기 담당")]
    [SerializeField] private CardCollector cardCollector;
    [SerializeField] private RectTransform cardContainer;
    [SerializeField] private RectTransform dragLayer;
    [SerializeField] private RectTransform stackPoint;
    [SerializeField] private GameObject stackVisual;
    [SerializeField] private Button drawButton;

    [Header("선택 슬롯")]
    [SerializeField] private CardSelectionSlot[] selectionSlots = new CardSelectionSlot[MaxSelectable];

    [Header("3장 선택 완료 시 활성화할 버튼")]
    [SerializeField] private Button nextButton;

    [Header("연출")]
    [SerializeField] private float drawDuration = 0.22f;
    [SerializeField] private float drawInterval = 0.04f;
    [SerializeField] private float flipDuration = 0.26f;

    private readonly List<CardView> cards = new List<CardView>();
    private bool cardsDrawn;
    private bool isResolving;

    public IReadOnlyList<CardView> SelectedCards => selectionSlots
        .Where(slot => slot != null && slot.CurrentCard != null)
        .Select(slot => slot.CurrentCard)
        .ToList();

    public RectTransform DragLayer => dragLayer != null ? dragLayer : (RectTransform)transform;
    public Camera UICamera => null;
    public bool CanInteractWithCards => cardsDrawn && !isResolving;

    private void Awake()
    {
        SetupCards();
        SetupSlots();
        if (drawButton != null) drawButton.onClick.AddListener(DrawCards);
        if (nextButton != null) nextButton.onClick.AddListener(ConfirmSelection);
        if (nextButton != null) nextButton.interactable = false;
    }

    private void Start()
    {
        CacheCardHomePoses();
    }

    // 손님이 바뀔 때마다(카드 덱 패널이 다시 열릴 때마다) GameManager가 호출한다.
    public void PrepareForNewRound()
    {
        SetupCards();
        ResetSelection();
        CacheCardHomePoses();

        cardsDrawn = false;
        isResolving = false;

        foreach (var card in cards)
        {
            if (card == null) continue;

            card.ReturnToDeckHome(keepFaceState: false);
            card.gameObject.SetActive(false);
        }

        if (stackVisual != null) stackVisual.SetActive(true);
        if (drawButton != null)
        {
            drawButton.gameObject.SetActive(true);
            drawButton.interactable = true;
        }

        UpdateNextButton();
    }

    private void OnDestroy()
    {
        foreach (var card in cards)
        {
            if (card != null) card.OnClicked -= HandleCardClicked;
        }

        if (drawButton != null) drawButton.onClick.RemoveListener(DrawCards);
        if (nextButton != null) nextButton.onClick.RemoveListener(ConfirmSelection);
    }

    // 3장 선택 완료 시 활성화되는 버튼 onClick에 연결한다.
    public void ConfirmSelection()
    {
        if (!AllSlotsFilled() || isResolving) return;

        StartCoroutine(ConfirmSelectionRoutine());
    }

    public void DrawCards()
    {
        if (cardsDrawn || isResolving) return;

        StartCoroutine(DrawCardsRoutine());
    }

    public bool TryPlaceCardAtScreenPosition(CardView card, Vector2 screenPosition)
    {
        if (!CanInteractWithCards) return false;

        foreach (var slot in selectionSlots)
        {
            if (slot == null) continue;

            var slotRect = (RectTransform)slot.transform;
            if (RectTransformUtility.RectangleContainsScreenPoint(slotRect, screenPosition, UICamera))
            {
                PlaceCardInSlot(card, slot);
                return true;
            }
        }

        return false;
    }

    public void PlaceCardInSlot(CardView card, CardSelectionSlot targetSlot)
    {
        if (!CanInteractWithCards || card == null || targetSlot == null) return;

        CardSelectionSlot previousSlot = card.CurrentSlot;
        if (previousSlot == targetSlot)
        {
            card.MoveToSlot(targetSlot);
            return;
        }

        previousSlot?.ClearCard(card);

        CardView displacedCard = targetSlot.CurrentCard;
        if (displacedCard != null && displacedCard != card)
        {
            ReturnCardToDeck(displacedCard);
        }

        targetSlot.SetCard(card);
        card.MoveToSlot(targetSlot);
        UpdateNextButton();
    }

    public void ReturnCardToDeck(CardView card)
    {
        if (card == null) return;

        card.CurrentSlot?.ClearCard(card);
        card.ReturnToDeckHome(keepFaceState: true);
        UpdateNextButton();
    }

    public int CountCorrectSlots(CustomerData customer)
    {
        if (customer == null || selectionSlots == null || selectionSlots.Length < MaxSelectable) return 0;

        int count = 0;
        if (selectionSlots[0] != null && selectionSlots[0].CurrentCard != null && selectionSlots[0].CurrentCard.Data == customer.causeCard) count++;
        if (selectionSlots[1] != null && selectionSlots[1].CurrentCard != null && selectionSlots[1].CurrentCard.Data == customer.presentCard) count++;
        if (selectionSlots[2] != null && selectionSlots[2].CurrentCard != null && selectionSlots[2].CurrentCard.Data == customer.adviceCard) count++;
        return count;
    }

    private void HandleCardClicked(CardView card)
    {
        if (!CanInteractWithCards || card == null) return;

        if (card.CurrentSlot != null)
        {
            ReturnCardToDeck(card);
            return;
        }

        CardSelectionSlot emptySlot = selectionSlots.FirstOrDefault(slot => slot != null && slot.CurrentCard == null);
        if (emptySlot != null) PlaceCardInSlot(card, emptySlot);
    }

    private IEnumerator DrawCardsRoutine()
    {
        isResolving = true;
        if (drawButton != null) drawButton.interactable = false;

        Vector2 pilePosition = stackPoint != null ? stackPoint.anchoredPosition : Vector2.zero;

        foreach (var card in cards)
        {
            if (card == null) continue;

            card.PrepareForDraw(pilePosition);
            yield return StartCoroutine(card.MoveHomeFromCurrentPosition(drawDuration));
            yield return new WaitForSeconds(drawInterval);
        }

        if (stackVisual != null) stackVisual.SetActive(false);
        if (drawButton != null) drawButton.gameObject.SetActive(false);

        cardsDrawn = true;
        isResolving = false;
        UpdateNextButton();
    }

    private IEnumerator ConfirmSelectionRoutine()
    {
        isResolving = true;
        UpdateNextButton();

        foreach (var slot in selectionSlots)
        {
            if (slot == null || slot.CurrentCard == null) continue;

            yield return StartCoroutine(slot.CurrentCard.FlipFaceUp(flipDuration));
            yield return new WaitForSeconds(0.08f);
        }

        yield return new WaitForSeconds(0.25f);
        isResolving = false;

        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.ShowingCardResult);
    }

    public void ResetSelection()
    {
        foreach (var slot in selectionSlots)
        {
            if (slot == null) continue;

            CardView card = slot.CurrentCard;
            slot.ClearCard();
            if (card != null) card.ReturnToDeckHome(keepFaceState: false);
        }

        UpdateNextButton();
    }

    private void SetupCards()
    {
        cards.Clear();

        if (cardCollector != null)
        {
            cardCollector.CollectCards();
            cards.AddRange(cardCollector.Cards.Where(card => card != null));
        }

        if (cards.Count > DeckCardCount)
        {
            cards.RemoveRange(DeckCardCount, cards.Count - DeckCardCount);
        }

        foreach (var card in cards)
        {
            if (card == null) continue;

            card.OnClicked -= HandleCardClicked;
            card.OnClicked += HandleCardClicked;
            card.RegisterDeck(this);
        }
    }

    private void SetupSlots()
    {
        for (int i = 0; i < selectionSlots.Length; i++)
        {
            if (selectionSlots[i] == null) continue;

            selectionSlots[i].Initialize(this, (CardSlotRole)i);
        }
    }

    private void CacheCardHomePoses()
    {
        if (cardContainer != null) LayoutRebuilder.ForceRebuildLayoutImmediate(cardContainer);

        foreach (var card in cards)
        {
            if (card != null) card.CacheHomePose();
        }
    }

    private bool AllSlotsFilled()
    {
        if (selectionSlots == null || selectionSlots.Length < MaxSelectable) return false;

        for (int i = 0; i < MaxSelectable; i++)
        {
            if (selectionSlots[i] == null || selectionSlots[i].CurrentCard == null) return false;
        }

        return true;
    }

    private void UpdateNextButton()
    {
        if (nextButton != null) nextButton.interactable = cardsDrawn && !isResolving && AllSlotsFilled();
    }
}
