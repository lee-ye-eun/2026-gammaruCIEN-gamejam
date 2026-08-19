using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 질문창을 화면 위로 숨겨두는 서랍(drawer)으로 만든다.
// 오른쪽 상단 끝의 납작한 역삼각형 탭을 누르면 질문창이 아래로 내려오고,
// 허공(질문창/탭 바깥)을 누르면 다시 위로 올라가며 사라진다.
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
    private Camera uiCamera;

    private Vector2 openPosition;
    private bool isOpen;
    private int openedFrame = -1; // 연 프레임. 같은 프레임의 클릭이 곧바로 '허공 클릭'으로 잡혀 닫히는 걸 막는다.
    private Coroutine slideRoutine;

    private void Awake()
    {
        if (questionPanel == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas = canvas.rootCanvas;
            canvasRect = (RectTransform)canvas.transform;
            uiCamera = canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        }

        // 씬에 배치해둔 위치를 '열린 상태'로 삼는다. 닫힌 위치는 캔버스 크기에 의존하므로 그때그때 계산한다
        // (Awake 시점에는 CanvasScaler가 아직 안 돌아 rect.height가 0으로 나올 수 있다).
        openPosition = questionPanel.anchoredPosition;

        if (matchTabColorToPanel) ApplyPanelColorToTab();
        if (tabButton != null) tabButton.onClick.AddListener(Toggle);

        SetOpen(false, instant: true);
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

        // 탭은 '질문 차례이면서 서랍이 닫혀 있을 때'만 보인다.
        // 질문창이 내려와 있는 동안에는 탭을 숨기고, 허공을 눌러서 닫는다.
        bool questionTurn = questionPanel.gameObject.activeInHierarchy;
        bool tabVisible = questionTurn && !isOpen;
        if (tab.gameObject.activeSelf != tabVisible) tab.gameObject.SetActive(tabVisible);
        if (!questionTurn || !isOpen) return;

        // 탭을 눌러 연 그 프레임의 클릭은 무시한다 (버튼 콜백과 Update의 실행 순서가 보장되지 않아, 열자마자 닫힐 수 있다).
        if (Time.frameCount == openedFrame) return;

        // 열려 있는 동안 허공을 누르면 닫는다 (질문창 안을 누른 경우는 제외).
        if (Pointer.current == null || !Pointer.current.press.wasPressedThisFrame) return;

        Vector2 screenPoint = Pointer.current.position.ReadValue();
        if (IsPointerOver(questionPanel, screenPoint)) return;

        SetOpen(false, instant: false);
    }

    private bool IsPointerOver(RectTransform target, Vector2 screenPoint)
    {
        return target != null && RectTransformUtility.RectangleContainsScreenPoint(target, screenPoint, uiCamera);
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
        if (open) openedFrame = Time.frameCount;
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
