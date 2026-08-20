using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 질문 차례에 질문창을 열린 위치로 표시한다.
// 역삼각형 탭 오브젝트는 기존 계층과 참조를 유지하되, 열린 질문창 위에서는 숨긴다.
//
// 다이얼로그 패널 루트에 붙인다. 질문창(questionPanel)은 질문 차례가 아닐 때 DialogueManager가 꺼버리므로,
// 항상 켜져 있는 루트에서 관리해야 탭의 표시 여부까지 따라갈 수 있다.
[DisallowMultipleComponent]
public class QuestionPanelDrawer : MonoBehaviour
{
    [Header("서랍으로 만들 질문창")]
    [SerializeField] private RectTransform questionPanel;

    [Header("역삼각형 탭 (프리팹의 QuestionDrawerTab)")]
    [SerializeField] private RectTransform tab;
    [SerializeField] private Button tabButton;
    [SerializeField] private bool matchTabColorToPanel = true; // 탭 색을 질문창 색에 맞춘다

    [Header("슬라이드")]
    [SerializeField] private float slideDuration = 0.25f;

    private RectTransform canvasRect;

    private Vector2 openPosition;
    private bool isOpen;
    private Coroutine slideRoutine;

    private void Awake()
    {
        if (questionPanel == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas = canvas.rootCanvas;
            canvasRect = (RectTransform)canvas.transform;
        }

        // 씬에 배치해둔 위치를 '열린 상태'로 삼는다. 닫힌 위치는 캔버스 크기에 의존하므로 그때그때 계산한다
        // (Awake 시점에는 CanvasScaler가 아직 안 돌아 rect.height가 0으로 나올 수 있다).
        openPosition = questionPanel.anchoredPosition;

        if (matchTabColorToPanel) ApplyPanelColorToTab();
        if (tabButton != null) tabButton.onClick.AddListener(Toggle);

        SetOpen(true, instant: true);
    }

    private void OnDestroy()
    {
        if (tabButton != null) tabButton.onClick.RemoveListener(Toggle);
    }

    // 역삼각형은 질문창과 같은 색으로. 나중에 질문창 색을 바꿔도 탭이 따라온다.
    private void ApplyPanelColorToTab()
    {
        var panelImage = questionPanel.GetComponent<Image>();
        var tabGraphic = tab != null ? tab.GetComponent<Graphic>() : null;
        if (panelImage != null && tabGraphic != null) tabGraphic.color = panelImage.color;
    }

    private void Update()
    {
        if (questionPanel == null || tab == null) return;

        // 질문 차례가 되면 질문창은 열린 위치에 그대로 보이고, 질문 차례가 아니면
        // DialogueManager가 questionPanel 자체를 꺼서 함께 숨긴다.
        bool questionTurn = questionPanel.gameObject.activeInHierarchy;
        bool tabVisible = questionTurn && !isOpen;
        if (tab.gameObject.activeSelf != tabVisible) tab.gameObject.SetActive(tabVisible);
    }

    public void Toggle()
    {
        SetOpen(!isOpen, instant: false);
    }

    // 질문창이 화면 위로 완전히 벗어나는 지점. 캔버스 높이를 못 읽으면 기준 해상도(1080)로 대체한다.
    private Vector2 ClosedPosition
    {
        get
        {
            float canvasHeight = canvasRect != null ? canvasRect.rect.height : 0f;
            if (canvasHeight <= 1f) canvasHeight = 1080f;
            return new Vector2(openPosition.x, canvasHeight * 0.5f + questionPanel.rect.height + 50f);
        }
    }

    private void SetOpen(bool open, bool instant)
    {
        isOpen = open;
        Vector2 destination = open ? openPosition : ClosedPosition;

        if (slideRoutine != null)
        {
            StopCoroutine(slideRoutine);
            slideRoutine = null;
        }

        if (instant || slideDuration <= 0f || !gameObject.activeInHierarchy)
        {
            questionPanel.anchoredPosition = destination;
            return;
        }

        slideRoutine = StartCoroutine(SlideTo(destination));
    }

    private IEnumerator SlideTo(Vector2 destination)
    {
        Vector2 start = questionPanel.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            questionPanel.anchoredPosition = Vector2.Lerp(start, destination, t * t * (3f - 2f * t)); // smoothstep
            yield return null;
        }

        questionPanel.anchoredPosition = destination;
        slideRoutine = null;
    }
}
