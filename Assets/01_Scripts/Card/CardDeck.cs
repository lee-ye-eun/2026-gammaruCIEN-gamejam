using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// CardDeckPanel 안에서만 카드 드로우, 슬롯 드래그 선택, 제출 연출을 담당한다.
public class CardDeck : MonoBehaviour
{
    private const int DeckCardCount = 6;
    private const int MaxSelectable = 3;

    [Header("카드 수집/드로우 (질문 1회 답변 시 자동으로 드로우됨)")]
    [SerializeField] private CardCollector cardCollector;
    [SerializeField] private RectTransform cardContainer;
    [SerializeField] private RectTransform dragLayer;
    [SerializeField] private RectTransform stackPoint;
    [SerializeField] private GameObject stackVisual;

    [Header("선택 슬롯")]
    [SerializeField] private CardSelectionSlot[] selectionSlots = new CardSelectionSlot[MaxSelectable];

    [Header("3장 선택 완료 시 활성화할 버튼")]
    [SerializeField] private Button nextButton;

    [Header("연출")]
    [SerializeField] private float drawDuration = 0.22f;
    [SerializeField] private float drawInterval = 0.04f;
    [SerializeField] private float flipDuration = 0.26f;

    [Header("투시경")]
    [SerializeField] private Button xRayButton;
    [SerializeField] private GameObject xRayOverlay;
    [SerializeField] private TMP_Text xRayButtonLabel;
    [SerializeField] private string xRayOnText = "투시경 ON";
    [SerializeField] private string xRayOffText = "투시경 OFF";
    [SerializeField] private string xRayToggleSfxKey = "piiik";
    [SerializeField, Min(0f)] private float xRayGraceDuration = 8f;
    [SerializeField, Min(0.01f)] private float xRaySuspicionInterval = 5f;
    [SerializeField, Min(0)] private int xRaySuspicionPerInterval = 2;

    [Header("사운드 키")]
    [SerializeField] private string cardDrawSfxKey = "cardDraw";
    [SerializeField] private string cardFlipSfxKey = "cardFlip";
    [SerializeField] private string cardPlaceSfxKey = "drop";

    private readonly List<CardView> cards = new List<CardView>();
    private Canvas parentCanvas;
    private bool cardsDrawn;
    private bool isDrawing;
    private bool isResolving;
    private bool xRayActive;
    private float xRayUsageTime;
    private float xRayPenaltyTime;

    public IReadOnlyList<CardView> SelectedCards => selectionSlots
        .Where(slot => slot != null && slot.CurrentCard != null)
        .Select(slot => slot.CurrentCard)
        .ToList();

    public RectTransform DragLayer => dragLayer != null ? dragLayer : (RectTransform)transform;
    public Camera UICamera => parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
        ? parentCanvas.worldCamera
        : null;
    public bool CanInteractWithCards => cardsDrawn && !isDrawing && !isResolving;

    // 플레이어에게 카드 뒷면이 실제로 보이고 있는지. 투시경을 켜면 앞면이 보이므로 false,
    // 카드를 아직 안 뽑았거나 제출 후 뒤집는 중(isResolving)이어도 false.
    public bool AreCardBacksVisible => cardsDrawn && !isResolving && !xRayActive;

    private void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        SetupSlots();
        SetupCards();

        if (nextButton != null) nextButton.onClick.AddListener(ConfirmSelection);
        if (xRayButton != null) xRayButton.onClick.AddListener(ToggleXRayMode);

        UpdateActionButtons();
    }

    private void Update()
    {
        UpdateXRaySuspicion();
    }

    private void OnDestroy()
    {
        if (nextButton != null) nextButton.onClick.RemoveListener(ConfirmSelection);
        if (xRayButton != null) xRayButton.onClick.RemoveListener(ToggleXRayMode);

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
        ResetXRaySuspicion();
        SetXRayMode(false);

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

    // 카드덱 상태 진입 시 GameFlowManager가 자동으로 호출한다. 이미 뽑혀 있으면 아무 일도 하지 않는다.
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
        card.SetXRayVisible(xRayActive);
        PlaySfx(cardPlaceSfxKey);
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
        card.SetXRayVisible(xRayActive);
        UpdateActionButtons();
    }

    public void ResetSelection()
    {
        SetXRayMode(false);

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

        isDrawing = false;
        cardsDrawn = true;
        UpdateActionButtons();
    }

    private IEnumerator ConfirmSelectionRoutine()
    {
        isResolving = true;
        SetXRayMode(false);
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
            card.SetXRayVisible(xRayActive && cardsDrawn);
            card.OnClicked += HandleCardClicked;
        }
    }

    private void UpdateActionButtons()
    {
        bool canUseXRay = CanUseXRayMode();
        if (!canUseXRay && xRayActive)
        {
            SetXRayMode(false);
        }

        if (nextButton != null)
        {
            bool showNextButton = cardsDrawn || isResolving;
            nextButton.gameObject.SetActive(showNextButton);
            nextButton.interactable = showNextButton && !isResolving && SelectedCards.Count == MaxSelectable;
        }

        if (xRayButton != null)
        {
            xRayButton.gameObject.SetActive(canUseXRay);
            xRayButton.interactable = canUseXRay;
        }

        UpdateXRayVisual();
    }

    private void ToggleXRayMode()
    {
        if (!CanUseXRayMode()) return;

        SetXRayMode(!xRayActive);
        PlaySfx(xRayToggleSfxKey);
    }

    private void SetXRayMode(bool active)
    {
        xRayActive = active && CanUseXRayMode();

        if (xRayOverlay != null)
        {
            xRayOverlay.SetActive(xRayActive);
        }

        foreach (var card in cards)
        {
            if (card != null) card.SetXRayVisible(xRayActive);
        }

        UpdateXRayVisual();
    }

    private void UpdateXRayVisual()
    {
        if (xRayButtonLabel != null)
        {
            xRayButtonLabel.text = xRayActive ? xRayOnText : xRayOffText;
        }
    }

    private void UpdateXRaySuspicion()
    {
        if (!xRayActive || isDrawing || isResolving) return;

        float deltaTime = Mathf.Max(0f, Time.deltaTime);
        if (deltaTime <= 0f) return;

        float previousUsageTime = xRayUsageTime;
        xRayUsageTime += deltaTime;

        float graceDuration = Mathf.Max(0f, xRayGraceDuration);
        float previousPenaltyTime = Mathf.Max(0f, previousUsageTime - graceDuration);
        float currentPenaltyTime = Mathf.Max(0f, xRayUsageTime - graceDuration);
        xRayPenaltyTime += currentPenaltyTime - previousPenaltyTime;

        int suspicionPerInterval = Mathf.Max(0, xRaySuspicionPerInterval);
        if (suspicionPerInterval <= 0) return;

        float suspicionInterval = Mathf.Max(0.01f, xRaySuspicionInterval);
        while (xRayPenaltyTime >= suspicionInterval)
        {
            xRayPenaltyTime -= suspicionInterval;

            GameManager gameManager = GameManager.Instance;
            if (gameManager == null) break;

            gameManager.AddSuspicion(suspicionPerInterval);
            if (gameManager.IsSuspicionThresholdReached) break;
        }
    }

    private void ResetXRaySuspicion()
    {
        xRayUsageTime = 0f;
        xRayPenaltyTime = 0f;
    }

    private bool CanUseXRayMode()
    {
        return cardsDrawn && !isDrawing && !isResolving;
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
