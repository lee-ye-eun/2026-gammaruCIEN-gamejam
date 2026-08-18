using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ReadingSheetController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI 참조")]
    [SerializeField] private GameObject smallSheet;
    [SerializeField] private GameObject overlay;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    [Header("의심도")]
    [SerializeField] private float freeReadSeconds = 7f;
    [SerializeField] private float suspicionPerSecond = 0.4f;

    [Header("Hover")]
    [SerializeField] private float hoverScale = 1.12f;

    private float readTimer;
    private float suspicionAccumulator;

    private void Awake()
    {
        if (openButton != null) openButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        Close();
    }

    private void OnDestroy()
    {
        if (openButton != null) openButton.onClick.RemoveListener(Open);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
    }

    private void Update()
    {
        if (overlay == null || !overlay.activeSelf) return;

        readTimer += Time.deltaTime;
        if (readTimer <= freeReadSeconds) return;

        suspicionAccumulator += suspicionPerSecond * Time.deltaTime;
        int wholeSuspicion = Mathf.FloorToInt(suspicionAccumulator);
        if (wholeSuspicion <= 0) return;

        suspicionAccumulator -= wholeSuspicion;
        if (GameManager.Instance != null) GameManager.Instance.AddSuspicion(wholeSuspicion);
    }

    public void Open()
    {
        readTimer = 0f;
        suspicionAccumulator = 0f;
        if (smallSheet != null) smallSheet.transform.localScale = Vector3.one;
        if (overlay != null)
        {
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
        }
    }

    public void Close()
    {
        readTimer = 0f;
        suspicionAccumulator = 0f;
        if (smallSheet != null) smallSheet.transform.localScale = Vector3.one;
        if (overlay != null) overlay.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (smallSheet != null && smallSheet.activeSelf) smallSheet.transform.localScale = Vector3.one * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (smallSheet != null) smallSheet.transform.localScale = Vector3.one;
    }
}
