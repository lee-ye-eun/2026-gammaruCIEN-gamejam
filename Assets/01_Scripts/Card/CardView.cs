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
    [SerializeField] private GameObject selectedIndicator;

    [Header("연출")]
    [SerializeField] private Color backColor = new Color32(93, 25, 49, 255);
    [SerializeField] private Color frontColor = new Color32(248, 246, 238, 255);
    [SerializeField] private float hoverScale = 1.08f;

    [Header("사운드 키")]
    [SerializeField] private string cardPickUpSfxKey = "chak";

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
    private bool xRayVisible;

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
        Refresh();
    }

    private void OnDisable()
    {
        isDragging = false;
    }

    private void OnValidate()
    {
        Refresh();
    }

    public void SetData(CardData newData)
    {
        data = newData;
        Refresh();
    }

    public void RegisterDeck(CardDeck deck)
    {
        ownerDeck = deck;
        if (CurrentSlot == null || homeParent == null)
        {
            CacheHomePose();
        }
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
        ReturnToDeckHome(false);
        gameObject.SetActive(true);
        SetFaceUp(false, true);
        SetXRayVisible(false);
        rectTransform.anchoredPosition = pilePosition;
        rectTransform.localScale = Vector3.one;
    }

    public IEnumerator MoveHomeFromCurrentPosition(float duration)
    {
        EnsureReferences();

        Vector2 startPosition = rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, homeAnchoredPosition, t);
            yield return null;
        }

        rectTransform.anchoredPosition = homeAnchoredPosition;
    }

    public void MoveToSlot(CardSelectionSlot slot)
    {
        if (slot == null) return;

        EnsureReferences();
        CurrentSlot = slot;
        SetSelected(true);
        SetSlotRole(string.Empty);
        SetFaceUp(false, true);

        transform.SetParent(slot.transform, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = homeSizeDelta.sqrMagnitude > 0f ? homeSizeDelta : rectTransform.sizeDelta;
        rectTransform.localScale = Vector3.one;
    }

    public void ReturnToDeckHome(bool keepFaceState = true)
    {
        EnsureReferences();
        if (homeParent == null) CacheHomePose();
        if (homeParent == null) return;

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

        if (!keepFaceState)
        {
            SetFaceUp(false, true);
            SetXRayVisible(false);
        }
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;

        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(false);
        }
    }

    public void SetFaceUp(bool faceUp, bool instant = true)
    {
        EnsureReferences();
        IsFaceUp = faceUp;
        if (faceUp) xRayVisible = false;

        RefreshFaceVisibility();
        if (selectedIndicator != null) selectedIndicator.SetActive(false);

        if (instant) rectTransform.localScale = Vector3.one;
    }

    public void SetXRayVisible(bool visible)
    {
        EnsureReferences();
        xRayVisible = visible && !IsFaceUp;
        RefreshFaceVisibility();
    }

    public IEnumerator FlipFaceUp(float duration)
    {
        EnsureReferences();
        duration = Mathf.Max(0.01f, duration);
        float halfDuration = duration * 0.5f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            rectTransform.localScale = new Vector3(Mathf.Lerp(1f, 0.05f, t), 1f, 1f);
            yield return null;
        }

        SetFaceUp(true, false);
        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            rectTransform.localScale = new Vector3(Mathf.Lerp(0.05f, 1f, t), 1f, 1f);
            yield return null;
        }

        rectTransform.localScale = Vector3.one;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanInteractInDeck()) return;

        rectTransform.localScale = Vector3.one * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging || CurrentSlot != null) return;

        rectTransform.localScale = Vector3.one;
        rectTransform.anchoredPosition = homeAnchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanInteractInDeck()) return;

        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(cardPickUpSfxKey);
        isDragging = true;
        ignoreNextClick = true;

        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;

        if (ownerDeck != null && ownerDeck.DragLayer != null)
        {
            transform.SetParent(ownerDeck.DragLayer, false);
            transform.SetAsLastSibling();
        }

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = Vector3.one * hoverScale;
        SetDraggedPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        SetDraggedPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
        rectTransform.localScale = Vector3.one;

        if (CurrentSlot == null && ownerDeck != null)
        {
            ownerDeck.ReturnCardToDeck(this);
        }

        StartCoroutine(AllowClickAfterDrag());
    }

    private void HandleButtonClicked()
    {
        if (ignoreNextClick) return;

        OnClicked?.Invoke(this);
    }

    private void SetDraggedPosition(PointerEventData eventData)
    {
        if (ownerDeck == null || ownerDeck.DragLayer == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                ownerDeck.DragLayer,
                eventData.position,
                ownerDeck.UICamera,
                out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }
    }

    private IEnumerator AllowClickAfterDrag()
    {
        yield return null;
        ignoreNextClick = false;
    }

    private bool CanInteractInDeck()
    {
        return ownerDeck != null
            && ownerDeck.CanInteractWithCards
            && CurrentSlot == null
            && !isDragging
            && !IsFaceUp;
    }

    private void RefreshFaceVisibility()
    {
        bool showFront = IsFaceUp || xRayVisible;

        if (frontRoot != null) frontRoot.SetActive(showFront);
        if (backRoot != null) backRoot.SetActive(!showFront);
        if (cardBodyImage != null) cardBodyImage.color = showFront ? frontColor : backColor;
    }

    private void SetSlotRole(string roleText)
    {
        if (slotRoleText == null) return;

        bool hasRole = !string.IsNullOrEmpty(roleText);
        slotRoleText.gameObject.SetActive(hasRole);
        slotRoleText.text = roleText;
    }

    private void Refresh()
    {
        ApplyDataToFace(symbolImage, nameText, keywordText);
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

    private void EnsureReferences()
    {
        if (rectTransform == null) rectTransform = (RectTransform)transform;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        if (button == null) button = GetComponent<Button>();
        if (cardBodyImage == null) cardBodyImage = GetComponent<Image>();
    }
}
