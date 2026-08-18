using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// 결과창(카드 결과 대사 다음, 매 손님마다 표시) 텍스트 출력 + 화면 터치 감지 담당.
// 결과 패널의 활성 상태는 GameManager가 관리. 터치 시 GameManager.Instance.ChangeState(Observing)을 직접 호출한다.
public class ResultManager : MonoBehaviour
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
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.Observing);
    }
}
