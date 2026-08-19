using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// CardDeckPanel 안에서만 카드 드로우, 슬롯 드래그 선택, 제출 연출을 담당한다.
public class CardDeck : MonoBehaviour
{
    private const int DeckCardCount = 6;
    private const int MaxSelectable = 3;

    [Header("카드 수집/드로우")]
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
    [SerializeField] private float mirrorPreviewFadeDuration = 0.18f;
    [SerializeField] private float flipDuration = 0.26f;

    [Header("사운드 키")]
    [SerializeField] private string cardDrawSfxKey = "cardDraw";
    [SerializeField] private string cardFlipSfxKey = "cardFlip";

    private readonly List<CardView> cards = new List<CardView>();
    private Canvas parentCanvas;
    private bool cardsDrawn;
    private bool isDrawing;
    private bool isResolving;

    public IReadOnlyList<CardView> SelectedCards => selectionSlots
        .Where(slot => slot != null && slot.CurrentCard != null)
        .Select(slot => slot.CurrentCard)
        .ToList();

    public RectTransform DragLayer => dragLayer != null ? dragLayer : (RectTransform)transform;
    public Camera UICamera => parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
        ? parentCanvas.worldCamera
        : null;
    public bool CanInteractWithCards => cardsDrawn && !isDrawing && !isResolving;

    private void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        SetupSlots();
        SetupCards();

        if (drawButton != null) drawButton.onClick.AddListener(DrawCards);
        if (nextButton != null) nextButton.onClick.AddListener(ConfirmSelection);

        UpdateActionButtons();
    }

    private void OnDestroy()
    {
        if (drawButton != null) drawButton.onClick.RemoveListener(DrawCards);
        if (nextButton != null) nextButton.onClick.RemoveListener(ConfirmSelection);

        foreach (var card in cards)
        {
            if (card != null) card.OnClicked -= HandleCardClicked;
        }
    }

    // 손님이 바뀔 때마다 GameFlowManager가 호출한다.
    public void PrepareForNewRound()
    {
        StopAllCoroutines();
        isDrawing = false;
        isResolving = false;
        cardsDrawn = false;

        SetupSlots();
        SetupCards();
        ResetSelection();

        Vector2 pilePosition = stackPoint != null ? stackPoint.anchoredPosition : Vector2.zero;
        foreach (var card in cards)
        {
            if (card == null) continue;

            card.PrepareForDraw(pilePosition);
            card.gameObject.SetActive(false);
        }

        if (stackVisual != null) stackVisual.SetActive(true);
        UpdateActionButtons();
    }

    public void DrawCards()
    {
        if (cardsDrawn || isDrawing || isResolving || cards.Count == 0) return;

        StartCoroutine(DrawCardsRoutine());
    }

    public bool PlaceCardInSlot(CardView card, CardSelectionSlot targetSlot)
    {
        if (card == null || targetSlot == null || !CanInteractWithCards) return false;

        CardSelectionSlot previousSlot = card.CurrentSlot;
        CardView previousCard = targetSlot.CurrentCard;

        if (previousCard == card) return true;

        if (previousSlot != null)
        {
            previousSlot.ClearCard(card);
        }

        if (previousCard != null)
        {
            previousCard.ReturnToDeckHome();
        }

        targetSlot.SetCard(card);
        card.MoveToSlot(targetSlot);
        UpdateActionButtons();
        return true;
    }

    public void ReturnCardToDeck(CardView card)
    {
        if (card == null) return;

        CardSelectionSlot slot = card.CurrentSlot;
        if (slot != null)
        {
            slot.ClearCard(card);
        }

        card.ReturnToDeckHome();
        UpdateActionButtons();
    }

    public void ResetSelection()
    {
        foreach (var slot in selectionSlots)
        {
            if (slot == null) continue;

            CardView card = slot.CurrentCard;
            if (card != null)
            {
                card.ReturnToDeckHome(false);
            }

            slot.ClearCard();
        }

        foreach (var card in cards)
        {
            if (card == null) continue;

            card.ReturnToDeckHome(false);
        }

        UpdateActionButtons();
    }

    // 선택 결정 버튼 onClick에 연결된다. 원인-현재-조언 순서로 카드를 뒤집은 뒤 기존 결과 상태로 넘긴다.
    public void ConfirmSelection()
    {
        if (SelectedCards.Count != MaxSelectable || isResolving) return;

        StartCoroutine(ConfirmSelectionRoutine());
    }

    public int CountCorrectSlots(CustomerData customer)
    {
        if (customer == null || selectionSlots.Length < MaxSelectable) return 0;

        int count = 0;
        if (IsSlotMatching(0, customer.causeCard)) count++;
        if (IsSlotMatching(1, customer.presentCard)) count++;
        if (IsSlotMatching(2, customer.adviceCard)) count++;
        return count;
    }

    private IEnumerator DrawCardsRoutine()
    {
        isDrawing = true;
        cardsDrawn = false;
        UpdateActionButtons();

        if (stackVisual != null) stackVisual.SetActive(false);
        PlaySfx(cardDrawSfxKey);

        Vector2 pilePosition = stackPoint != null ? stackPoint.anchoredPosition : Vector2.zero;

        foreach (var card in cards)
        {
            if (card == null) continue;

            card.PrepareForDraw(pilePosition);
            StartCoroutine(card.MoveHomeFromCurrentPosition(drawDuration));
            yield return new WaitForSeconds(drawInterval);
        }

        yield return new WaitForSeconds(drawDuration);

        foreach (var card in cards)
        {
            if (card == null) continue;

            card.RevealFrontPreviewAfterDraw(mirrorPreviewFadeDuration);
        }

        yield return new WaitForSeconds(mirrorPreviewFadeDuration);

        isDrawing = false;
        cardsDrawn = true;
        UpdateActionButtons();
    }

    private IEnumerator ConfirmSelectionRoutine()
    {
        isResolving = true;
        UpdateActionButtons();

        foreach (var card in SelectedCards)
        {
            if (card == null) continue;

            PlaySfx(cardFlipSfxKey);
            yield return card.FlipFaceUp(flipDuration);
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(0.25f);

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ChangeState(GameFlowManager.GameState.ShowingCardResult);
        }
    }

    private void HandleCardClicked(CardView card)
    {
        if (card == null || isResolving) return;

        if (card.CurrentSlot != null)
        {
            ReturnCardToDeck(card);
        }
    }

    private void SetupSlots()
    {
        if (selectionSlots == null) selectionSlots = new CardSelectionSlot[MaxSelectable];

        for (int i = 0; i < selectionSlots.Length; i++)
        {
            if (selectionSlots[i] == null) continue;

            selectionSlots[i].Initialize(this, IndexToSlotRole(i));
        }
    }

    private void SetupCards()
    {
        foreach (var card in cards)
        {
            if (card != null) card.OnClicked -= HandleCardClicked;
        }

        cards.Clear();

        if (cardCollector != null)
        {
            cardCollector.CollectCards();
            cards.AddRange(cardCollector.Cards);
        }

        if (cards.Count == 0)
        {
            RectTransform searchRoot = cardContainer != null ? cardContainer : (RectTransform)transform;
            cards.AddRange(searchRoot.GetComponentsInChildren<CardView>(true));
        }

        List<CardView> uniqueCards = cards
            .Where(card => card != null)
            .Distinct()
            .Take(DeckCardCount)
            .ToList();

        cards.Clear();
        cards.AddRange(uniqueCards);

        if (cardContainer != null) LayoutRebuilder.ForceRebuildLayoutImmediate(cardContainer);

        foreach (var card in cards)
        {
            if (card == null) continue;

            card.RegisterDeck(this);
            card.SetSelected(false);
            card.SetFaceUp(false, true);
            card.OnClicked += HandleCardClicked;
        }
    }

    private void UpdateActionButtons()
    {
        if (drawButton != null)
        {
            bool showDrawButton = !cardsDrawn && !isResolving;
            drawButton.gameObject.SetActive(showDrawButton);
            drawButton.interactable = showDrawButton && !isDrawing && cards.Count > 0;
        }

        if (nextButton != null)
        {
            bool showNextButton = cardsDrawn || isResolving;
            nextButton.gameObject.SetActive(showNextButton);
            nextButton.interactable = showNextButton && !isResolving && SelectedCards.Count == MaxSelectable;
        }
    }

    private static void PlaySfx(string key)
    {
        if (string.IsNullOrEmpty(key) || SoundManager.Instance == null) return;

        SoundManager.Instance.PlaySFX(key);
    }

    private bool IsSlotMatching(int index, CardData answer)
    {
        if (answer == null || index < 0 || index >= selectionSlots.Length) return false;

        CardView card = selectionSlots[index] != null ? selectionSlots[index].CurrentCard : null;
        return card != null && card.Data == answer;
    }

    private static CardSlotRole IndexToSlotRole(int index)
    {
        switch (index)
        {
            case 0:
                return CardSlotRole.Cause;
            case 1:
                return CardSlotRole.Present;
            case 2:
                return CardSlotRole.Advice;
            default:
                return CardSlotRole.Cause;
        }
    }
}
