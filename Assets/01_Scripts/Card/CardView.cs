using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("데이터")]
    [SerializeField] private CardData data;

    [Header("UI 참조")]
    [SerializeField] private Image cardBodyImage;
    [SerializeField] private GameObject frontRoot;
    [SerializeField] private GameObject backRoot;
    [SerializeField] private Image symbolImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text keywordText;
    [SerializeField] private TMP_Text slotRoleText;

    [Header("선택 상태")]
    [SerializeField] private Button button;
    [SerializeField] private GameObject selectedIndicator; // 선택 시 켜지는 테두리/체크 표시 (선택)

    [Header("거울 앞면 미리보기")]
    [SerializeField] private GameObject frontPreviewRoot;
    [SerializeField] private RectTransform frontPreviewCard;
    [SerializeField] private Image previewSymbolImage;
    [SerializeField] private TMP_Text previewNameText;
    [SerializeField] private TMP_Text previewKeywordText;
    [SerializeField] private float frontPreviewCollapsedY = 100f;
    [SerializeField] private float frontPreviewExpandedY = 0f;
    [SerializeField] private float frontPreviewSlideDuration = 0.16f;

    [Header("연출")]
    [SerializeField] private Color backColor = new Color32(93, 25, 49, 255);
    [SerializeField] private Color frontColor = new Color32(248, 246, 238, 255);
    [SerializeField] private Vector2 hoverOffset = new Vector2(0f, -36f);
    [SerializeField] private float hoverScale = 1.08f;

    public CardData Data => data;
    public bool IsSelected { get; private set; }
    public bool IsFaceUp { get; private set; }
    public CardSelectionSlot CurrentSlot { get; private set; }

    public event Action<CardView> OnClicked;

    private CardDeck ownerDeck;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform homeParent;
    private int homeSiblingIndex;
    private Vector2 homeAnchoredPosition;
    private Vector2 homeSizeDelta;
    private bool isDragging;
    private bool ignoreNextClick;
    private Coroutine frontPreviewRoutine;

    private void Awake()
    {
        EnsureReferences();

        if (button != null)
        {
            button.onClick.AddListener(HandleButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClicked);
        }
    }

    private void OnEnable()
    {
        EnsureReferences();
        if (data != null) Refresh();
        UpdateFrontPreviewState(instant: true);
    }

    private void OnDisable()
    {
        if (frontPreviewRoutine != null)
        {
            StopCoroutine(frontPreviewRoutine);
            frontPreviewRoutine = null;
        }

        isDragging = false;
    }

    public void SetData(CardData newData)
    {
        data = newData;
        Refresh();
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        if (selectedIndicator != null) selectedIndicator.SetActive(selected);
    }

    public void RegisterDeck(CardDeck deck)
    {
        ownerDeck = deck;
        CacheHomePose();
    }

    public void CacheHomePose()
    {
        EnsureReferences();

        homeParent = transform.parent;
        homeSiblingIndex = transform.GetSiblingIndex();
        homeAnchoredPosition = rectTransform.anchoredPosition;
        homeSizeDelta = rectTransform.sizeDelta;
    }

    public void PrepareForDraw(Vector2 pilePosition)
    {
        ReturnToDeckHome(keepFaceState: false);
        SetFaceUp(false, instant: true);
        gameObject.SetActive(true);
        rectTransform.anchoredPosition = pilePosition;
    }

    public IEnumerator MoveHomeFromCurrentPosition(float duration)
    {
        Vector2 start = rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            rectTransform.anchoredPosition = Vector2.LerpUnclamped(start, homeAnchoredPosition, t);
            yield return null;
        }

        rectTransform.anchoredPosition = homeAnchoredPosition;
    }

    public void MoveToSlot(CardSelectionSlot slot)
    {
        if (slot == null) return;

        CurrentSlot = slot;
        SetSelected(true);
        SetSlotRole(slot.DisplayName);
        UpdateFrontPreviewState(instant: true);

        Transform slotTransform = slot.transform;
        transform.SetParent(slotTransform, false);

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = ((RectTransform)slotTransform).sizeDelta;
        rectTransform.localScale = Vector3.one;
    }

    public void ReturnToDeckHome(bool keepFaceState = true)
    {
        if (homeParent == null) CacheHomePose();

        CurrentSlot = null;
        SetSelected(false);
        SetSlotRole(string.Empty);

        transform.SetParent(homeParent, false);
        transform.SetSiblingIndex(Mathf.Clamp(homeSiblingIndex, 0, homeParent.childCount - 1));

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = homeAnchoredPosition;
        rectTransform.sizeDelta = homeSizeDelta;
        rectTransform.localScale = Vector3.one;

        if (!keepFaceState) SetFaceUp(false, instant: true);
        else UpdateFrontPreviewState(instant: true);
    }

    public void SetFaceUp(bool faceUp, bool instant = true)
    {
        EnsureReferences();
        IsFaceUp = faceUp;

        if (frontRoot != null) frontRoot.SetActive(faceUp);
        if (backRoot != null) backRoot.SetActive(!faceUp);
        if (cardBodyImage != null) cardBodyImage.color = faceUp ? frontColor : backColor;
        UpdateFrontPreviewState(instant);

        if (instant) rectTransform.localScale = Vector3.one;
    }

    public IEnumerator FlipFaceUp(float duration)
    {
        EnsureReferences();

        float halfDuration = duration * 0.5f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float x = Mathf.Lerp(1f, 0.04f, Mathf.Clamp01(elapsed / halfDuration));
            rectTransform.localScale = new Vector3(x, 1f, 1f);
            yield return null;
        }

        SetFaceUp(true, instant: false);

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float x = Mathf.Lerp(0.04f, 1f, Mathf.Clamp01(elapsed / halfDuration));
            rectTransform.localScale = new Vector3(x, 1f, 1f);
            yield return null;
        }

        rectTransform.localScale = Vector3.one;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanInteractInDeck()) return;

        rectTransform.localScale = Vector3.one * hoverScale;
        ShowFrontPreview(expanded: true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging || CurrentSlot != null) return;

        rectTransform.localScale = Vector3.one;
        rectTransform.anchoredPosition = homeAnchoredPosition;
        ShowFrontPreview(expanded: false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (ownerDeck == null || !ownerDeck.CanInteractWithCards) return;

        isDragging = true;
        ignoreNextClick = true;
        UpdateFrontPreviewState(instant: true);
        canvasGroup.blocksRaycasts = false;
        transform.SetParent(ownerDeck.DragLayer, true);
        transform.SetAsLastSibling();
        rectTransform.localScale = Vector3.one * hoverScale;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || ownerDeck == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            ownerDeck.DragLayer,
            eventData.position,
            ownerDeck.UICamera,
            out Vector2 localPoint);

        rectTransform.anchoredPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging || ownerDeck == null) return;

        isDragging = false;
        canvasGroup.blocksRaycasts = true;

        if (!ownerDeck.TryPlaceCardAtScreenPosition(this, eventData.position))
        {
            ownerDeck.ReturnCardToDeck(this);
        }

        StartCoroutine(AllowClickNextFrame());
    }

    private void HandleButtonClicked()
    {
        if (ignoreNextClick) return;
        OnClicked?.Invoke(this);
    }

    private bool CanInteractInDeck()
    {
        return ownerDeck != null && ownerDeck.CanInteractWithCards && CurrentSlot == null && !isDragging;
    }

    private IEnumerator AllowClickNextFrame()
    {
        yield return null;
        ignoreNextClick = false;
    }

    private void ShowFrontPreview(bool expanded)
    {
        if (!ShouldShowFrontPreview() || frontPreviewCard == null) return;

        if (frontPreviewRoutine != null) StopCoroutine(frontPreviewRoutine);
        frontPreviewRoot.SetActive(true);
        frontPreviewRoutine = StartCoroutine(AnimateFrontPreview(expanded ? frontPreviewExpandedY : frontPreviewCollapsedY));
    }

    private IEnumerator AnimateFrontPreview(float targetY)
    {
        Vector2 start = frontPreviewCard.anchoredPosition;
        Vector2 target = new Vector2(start.x, targetY);
        float elapsed = 0f;

        while (elapsed < frontPreviewSlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / frontPreviewSlideDuration));
            frontPreviewCard.anchoredPosition = Vector2.LerpUnclamped(start, target, t);
            yield return null;
        }

        frontPreviewCard.anchoredPosition = target;
        frontPreviewRoutine = null;
    }

    private void UpdateFrontPreviewState(bool instant)
    {
        if (frontPreviewRoot == null) return;

        bool shouldShow = ShouldShowFrontPreview();
        frontPreviewRoot.SetActive(shouldShow);

        if (instant && frontPreviewCard != null)
        {
            frontPreviewCard.anchoredPosition = new Vector2(frontPreviewCard.anchoredPosition.x, frontPreviewCollapsedY);
        }
    }

    private bool ShouldShowFrontPreview()
    {
        return frontPreviewRoot != null && !IsFaceUp && CurrentSlot == null && !isDragging;
    }

    private void SetSlotRole(string role)
    {
        if (slotRoleText == null) return;

        slotRoleText.text = role;
        slotRoleText.gameObject.SetActive(!string.IsNullOrEmpty(role));
    }

    private void EnsureReferences()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (button == null) button = GetComponent<Button>();
        if (cardBodyImage == null) cardBodyImage = GetComponent<Image>();
    }

    private void Refresh()
    {
        ApplyDataToFace(symbolImage, nameText, keywordText);
        ApplyDataToFace(previewSymbolImage, previewNameText, previewKeywordText);
    }

    private void ApplyDataToFace(Image targetSymbolImage, TMP_Text targetNameText, TMP_Text targetKeywordText)
    {
        if (targetSymbolImage != null) targetSymbolImage.sprite = data != null ? data.symbol : null;
        if (targetNameText != null) targetNameText.text = data != null ? data.cardName : string.Empty;
        if (targetKeywordText != null)
        {
            targetKeywordText.text = data != null && data.keywords != null
                ? string.Join(" / ", data.keywords)
                : string.Empty;
        }
    }
}
