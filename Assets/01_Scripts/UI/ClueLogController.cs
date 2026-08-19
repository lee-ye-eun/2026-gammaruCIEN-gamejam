using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ClueLogController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private RectTransform smallNote;
    [SerializeField] private CanvasGroup smallNoteGroup;
    [SerializeField] private GameObject expandedOverlay;
    [SerializeField] private Button closeOverlayButton;
    [SerializeField] private TMP_Text worryText;
    [SerializeField] private TMP_Text questionLogText;
    [SerializeField] private float hoverScale = 1.08f;

    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        if (smallNote == null) smallNote = (RectTransform)transform;
        if (smallNoteGroup == null) smallNoteGroup = smallNote.GetComponent<CanvasGroup>();
        if (smallNoteGroup == null) smallNoteGroup = smallNote.gameObject.AddComponent<CanvasGroup>();

        baseScale = smallNote.localScale;

        if (expandedOverlay != null) expandedOverlay.SetActive(false);
        SetSmallNoteVisible(true);

        if (closeOverlayButton != null) closeOverlayButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        if (closeOverlayButton != null) closeOverlayButton.onClick.RemoveListener(Close);
    }

    private void OnDisable()
    {
        if (expandedOverlay != null) expandedOverlay.SetActive(false);
        if (smallNote != null) smallNote.localScale = baseScale;
        SetSmallNoteVisible(true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (smallNote != null) smallNote.localScale = baseScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (smallNote != null) smallNote.localScale = baseScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Open();
    }

    public void Open()
    {
        RefreshLogText();

        if (expandedOverlay != null)
        {
            expandedOverlay.transform.SetAsLastSibling();
            expandedOverlay.SetActive(true);
        }

        SetSmallNoteVisible(false);
    }

    public void Close()
    {
        if (expandedOverlay != null) expandedOverlay.SetActive(false);
        if (smallNote != null) smallNote.localScale = baseScale;
        SetSmallNoteVisible(true);
    }

    private void RefreshLogText()
    {
        GameManager gameManager = GameManager.Instance;
        string worry = gameManager != null ? gameManager.CurrentWorryText : string.Empty;
        IReadOnlyList<GameManager.QuestionLogEntry> logs = gameManager != null ? gameManager.CurrentQuestionLogs : null;

        if (worryText != null)
        {
            worryText.text = string.IsNullOrWhiteSpace(worry)
                ? "손님의 고민\n아직 기록된 고민이 없습니다."
                : $"손님의 고민\n{worry}";
        }

        if (questionLogText == null) return;

        if (logs == null || logs.Count == 0)
        {
            questionLogText.text = "아직 확인한 질문이 없습니다.";
            return;
        }

        var builder = new StringBuilder();
        for (int i = 0; i < logs.Count; i++)
        {
            GameManager.QuestionLogEntry entry = logs[i];
            if (string.IsNullOrWhiteSpace(entry.question) && string.IsNullOrWhiteSpace(entry.answer)) continue;

            builder.Append("Q. ");
            builder.AppendLine(entry.question);
            builder.Append("A. ");
            builder.AppendLine(entry.answer);

            if (i < logs.Count - 1) builder.AppendLine();
        }

        questionLogText.text = builder.Length > 0 ? builder.ToString().TrimEnd() : "아직 확인한 질문이 없습니다.";
    }

    private void SetSmallNoteVisible(bool visible)
    {
        if (smallNoteGroup == null) return;

        smallNoteGroup.alpha = visible ? 1f : 0f;
        smallNoteGroup.blocksRaycasts = visible;
        smallNoteGroup.interactable = visible;
    }
}
