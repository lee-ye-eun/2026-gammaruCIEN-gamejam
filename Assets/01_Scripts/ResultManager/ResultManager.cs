using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// 결과 텍스트 출력 + 화면 터치 감지만 담당. 결과 패널의 활성 상태는 GameManager가 관리.
public class ResultManager : MonoBehaviour
{
    [Header("결과 텍스트")]
    [SerializeField] private TMP_Text resultText;

    // 결과가 표시된 상태에서 화면을 클릭/터치하면 발행됨
    public event Action OnResultAdvanced;

    private bool isResultActive;

    private void Update()
    {
        if (!isResultActive) return;
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            AdvanceResult();
        }
    }

    public void ShowResult(string text)
    {
        if (resultText != null) resultText.text = text;
        isResultActive = true;
    }

    private void AdvanceResult()
    {
        isResultActive = false; // 중복 트리거 방지
        OnResultAdvanced?.Invoke();
    }
}
