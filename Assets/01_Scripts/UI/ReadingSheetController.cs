using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ReadingSheetController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private RectTransform smallSheet;
    [SerializeField] private CanvasGroup smallSheetGroup;
    [SerializeField] private GameObject expandedOverlay;
    [SerializeField] private Button closeOverlayButton;
    [SerializeField] private float hoverScale = 1.12f;

    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        if (smallSheet == null) smallSheet = (RectTransform)transform;
        if (smallSheetGroup == null) smallSheetGroup = smallSheet.GetComponent<CanvasGroup>();
        if (smallSheetGroup == null) smallSheetGroup = smallSheet.gameObject.AddComponent<CanvasGroup>();

        baseScale = smallSheet.localScale;

        if (expandedOverlay != null) expandedOverlay.SetActive(false);
        SetSmallSheetVisible(true);

        if (closeOverlayButton != null) closeOverlayButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        if (closeOverlayButton != null) closeOverlayButton.onClick.RemoveListener(Close);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (smallSheet != null) smallSheet.localScale = baseScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (smallSheet != null) smallSheet.localScale = baseScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Open();
    }

    public void Open()
    {
        if (expandedOverlay != null) expandedOverlay.SetActive(true);
        SetSmallSheetVisible(false);
    }

    public void Close()
    {
        if (expandedOverlay != null) expandedOverlay.SetActive(false);
        if (smallSheet != null) smallSheet.localScale = baseScale;
        SetSmallSheetVisible(true);
    }

    private void SetSmallSheetVisible(bool visible)
    {
        if (smallSheetGroup == null) return;

        smallSheetGroup.alpha = visible ? 1f : 0f;
        smallSheetGroup.blocksRaycasts = visible;
        smallSheetGroup.interactable = visible;
    }
}
