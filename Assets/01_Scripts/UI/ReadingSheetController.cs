using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ReadingSheetController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private RectTransform smallSheet;
    [SerializeField] private GameObject expandedOverlay;
    [SerializeField] private Button closeOverlayButton;
    [SerializeField] private float hoverScale = 1.12f;

    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        if (smallSheet == null) smallSheet = (RectTransform)transform;
        baseScale = smallSheet.localScale;

        if (expandedOverlay != null) expandedOverlay.SetActive(false);
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
    }

    public void Close()
    {
        if (expandedOverlay != null) expandedOverlay.SetActive(false);
        if (smallSheet != null) smallSheet.localScale = baseScale;
    }
}
