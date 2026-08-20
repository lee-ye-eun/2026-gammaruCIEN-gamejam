using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

// 튜토리얼 오버레이. GameFlowManager.ChangeState()가 상태를 바꿀 때마다 NotifyStateChanged가 호출되어,
// 그 상태에 해당하는 스텝들(TutorialData.steps 중 state가 같은 것들, 배열 순서대로) 중 첫 번째를 보여준다.
// 강조 영역을 1회 터치하면(블로커가 아니라 강조 영역 자체를 직접 터치 감지) 같은 상태의 다음 스텝으로 넘어간다.
// 이 터치 감지는 UI 레이캐스트를 막지 않으므로, 강조 영역 아래의 실제 게임 UI도 같은 터치로 정상 동작한다.
// 같은 상태에 더 보여줄 스텝이 없으면 텍스트박스와 블로커를 모두 꺼서 화면 전체(1920x1080)가 터치 가능해진다.
// 첫 손님이 끝나면(GameFlowManager가 CompleteTutorial()을 호출) 패널 전체가 비활성화된다.
//
// 씬 설정 시 주의:
// - 이 패널은 게임 내 다른 모든 패널보다 위에 그려져야 한다: Canvas(Override Sorting, 가장 높은 Sort Order) +
//   그 Canvas에 직접 붙인 GraphicRaycaster가 필요하다 (이 프로젝트의 다른 패널들과 동일한 패턴).
// - 딤 블로커 4개(dimTop/Bottom/Left/Right)는 rootRect의 자식이어야 하고, 각각
//   anchorMin = anchorMax = pivot = (0.5, 0.5)로 둬야 한다 (anchoredPosition이 rootRect.rect와
//   같은 로컬 좌표를 쓰게 하기 위함). highlightPosition/highlightSize도 이 좌표계 기준으로 입력한다.
public class TutorialPanel : MonoBehaviour
{
    [Header("데이터 (스텝마다 상태 / 텍스트 / 강조 영역)")]
    [SerializeField] private TutorialData data;

    [Header("전체 화면 기준 RectTransform")]
    [SerializeField] private RectTransform rootRect;

    [Header("딤 블로커 4방향 (강조 영역 밖을 덮어서 터치를 막음)")]
    [SerializeField] private Image dimTop;
    [SerializeField] private Image dimBottom;
    [SerializeField] private Image dimLeft;
    [SerializeField] private Image dimRight;

    [Header("안내 텍스트")]
    [SerializeField] private GameObject textBox;
    [SerializeField] private TMP_Text tutorialText;

    private GameFlowManager.GameState currentState;
    private int currentStepIndex;

    private void Update()
    {
        TutorialStep step = GetStep(currentState, currentStepIndex);
        if (step == null) return;

        if (Pointer.current == null || !Pointer.current.press.wasPressedThisFrame) return;

        // highlightSize가 (0,0)이면 "특정 강조 영역 없음" = 화면 전체가 곧 강조 영역이라 어디를 터치해도 넘어간다.
        bool hasHighlight = step.highlightSize.x > 0f && step.highlightSize.y > 0f;
        if (hasHighlight && !IsPointInsideHighlight(step, Pointer.current.position.ReadValue())) return;

        // 강조 영역을 터치했을 뿐, 여기서 이벤트를 막지 않는다 -> 아래 실제 게임 UI도 같은 터치를 그대로 받는다.
        currentStepIndex++;
        ShowCurrentStep();
    }

    // GameFlowManager.ChangeState()에서 상태가 바뀔 때마다 호출된다.
    public void NotifyStateChanged(GameFlowManager.GameState state)
    {
        if (!gameObject.activeInHierarchy) return; // CompleteTutorial() 이후엔 완전히 무시

        currentState = state;
        currentStepIndex = 0;
        ShowCurrentStep();
    }

    // 첫 손님이 끝나고 다음 손님으로 넘어갈 때 GameFlowManager가 호출 -> 튜토리얼을 완전히 종료한다.
    public void CompleteTutorial()
    {
        gameObject.SetActive(false);
    }

    private void ShowCurrentStep()
    {
        TutorialStep step = GetStep(currentState, currentStepIndex);
        bool hasStep = step != null;

        if (textBox != null) textBox.SetActive(hasStep);
        if (!hasStep)
        {
            SetBlockersActive(false); // 더 보여줄 스텝 없음 -> 블로커 없음 -> 화면 전체(1920x1080)가 곧 터치 가능 영역
            return;
        }

        if (tutorialText != null) tutorialText.text = step.text;

        // highlightSize가 (0,0)이면 특정 강조 없이 전체 화면을 그대로 둔다(텍스트만 보여줌).
        bool hasHighlight = step.highlightSize.x > 0f && step.highlightSize.y > 0f;
        SetBlockersActive(hasHighlight);
        if (hasHighlight) ApplyHighlight(step.highlightPosition, step.highlightSize);
    }

    // state가 같은 스텝들만 뽑아서 순서대로 index번째를 찾는다. 없으면 null.
    private TutorialStep GetStep(GameFlowManager.GameState state, int index)
    {
        var steps = data != null ? data.steps : null;
        if (steps == null) return null;

        int count = 0;
        foreach (var step in steps)
        {
            if (step == null || step.state != state) continue;
            if (count == index) return step;
            count++;
        }
        return null;
    }

    private void SetBlockersActive(bool active)
    {
        if (dimTop != null) dimTop.gameObject.SetActive(active);
        if (dimBottom != null) dimBottom.gameObject.SetActive(active);
        if (dimLeft != null) dimLeft.gameObject.SetActive(active);
        if (dimRight != null) dimRight.gameObject.SetActive(active);
    }

    // 강조(터치 가능) 영역만 비우고 나머지를 4방향 블로커로 덮는다.
    private void ApplyHighlight(Vector2 holeCenter, Vector2 holeSize)
    {
        if (rootRect == null) return;

        Rect full = rootRect.rect;
        Rect hole = GetHoleRect(holeCenter, holeSize);

        SetBlockerRect(dimTop, full.xMin, full.xMax, hole.yMax, full.yMax);
        SetBlockerRect(dimBottom, full.xMin, full.xMax, full.yMin, hole.yMin);
        SetBlockerRect(dimLeft, full.xMin, hole.xMin, hole.yMin, hole.yMax);
        SetBlockerRect(dimRight, hole.xMax, full.xMax, hole.yMin, hole.yMax);
    }

    private bool IsPointInsideHighlight(TutorialStep step, Vector2 screenPos)
    {
        if (rootRect == null) return false;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, screenPos, GetEventCamera(), out Vector2 local)) return false;

        return GetHoleRect(step.highlightPosition, step.highlightSize).Contains(local);
    }

    private Camera GetEventCamera()
    {
        Canvas canvas = rootRect != null ? rootRect.GetComponentInParent<Canvas>() : null;
        return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
    }

    private static Rect GetHoleRect(Vector2 center, Vector2 size)
    {
        return new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
    }

    private static void SetBlockerRect(Image blocker, float xMin, float xMax, float yMin, float yMax)
    {
        if (blocker == null) return;

        blocker.raycastTarget = true; // 터치 차단용이므로 항상 켜져 있어야 한다

        float width = Mathf.Max(0f, xMax - xMin);
        float height = Mathf.Max(0f, yMax - yMin);

        RectTransform rect = blocker.rectTransform;
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f);
    }
}
