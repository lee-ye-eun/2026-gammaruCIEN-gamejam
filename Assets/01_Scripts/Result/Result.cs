using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// 결과창(카드 결과 대사 다음, 매 손님마다 표시) 텍스트 출력 + 화면 터치 감지 담당.
// 결과 패널의 활성 상태는 GameFlowManager가 관리. 터치 시 GameFlowManager.Instance.ChangeState(Observing)을 직접 호출한다.
// (의심도 임계치 초과로 인한 스토리 씬 이동은 여기서 확인하지 않는다 - GameManager.AddSuspicion()이 임계치를 넘는
// 즉시 처리하므로, 이 시점엔 이미 전환이 시작되어 있거나 애초에 해당하지 않는다.)

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
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.ChangeState(GameFlowManager.GameState.Observing);
    }
}
