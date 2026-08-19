using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// 결과창(카드 결과 대사 다음, 매 손님마다 표시) 텍스트 출력 + 화면 터치 감지 담당.
// 결과 패널의 활성 상태는 GameFlowManager가 관리. 터치 시 미뤄둔 카드 결과 의심도를 실제로 반영하고,
// 그 반영으로 의심도가 임계치를 넘었으면(스토리 씬 전환은 GameManager.AddSuspicion()이 즉시 처리) 다음 손님으로
// 넘어가지 않고 멈춘다. 넘지 않았으면 평소처럼 GameFlowManager.Instance.ChangeState(Observing)을 호출한다.

public class Result : MonoBehaviour
{
    [Header("결과 텍스트")]
    [SerializeField] private TMP_Text resultText;

    private bool isTouchActive;

    private void Update()
    {
        if (!isTouchActive) return;
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            AdvanceTouch();
        }
    }

    public void ShowResult(string text)
    {
        if (resultText != null) resultText.text = text;
        isTouchActive = true;
    }

    private void AdvanceTouch()
    {
        isTouchActive = false; // 중복 트리거 방지
        if (GameFlowManager.Instance == null) return;

        GameFlowManager.Instance.CommitCardResultSuspicion();

        // 방금 반영으로 의심도가 임계치를 넘었으면 GameManager가 이미 스토리 씬 전환을 시작했다 - 다음 손님으로 넘어가지 않는다.
        if (GameManager.Instance != null && GameManager.Instance.IsSuspicionThresholdReached) return;

        GameFlowManager.Instance.ChangeState(GameFlowManager.GameState.Observing);
    }
}
