using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// 대사 텍스트 출력 + 화면 터치 감지만 담당. 대화창 패널의 활성 상태는 GameManager가 관리.
public class DialogueManager : MonoBehaviour
{
    [Header("대화 텍스트")]
    [SerializeField] private TMP_Text dialogueText;

    // 대사가 표시된 상태에서 화면을 클릭/터치하면 발행됨
    public event Action OnDialogueAdvanced;

    private bool isDialogueActive;

    private void Update()
    {
        if (!isDialogueActive) return;
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            AdvanceDialogue();
        }
    }

    // 임의의 대사 한 줄을 출력하고 터치 감지를 시작한다 (손님 관찰 대사, 카드 결과 대사 등 공용)
    public void ShowDialogue(string text)
    {
        if (dialogueText != null) dialogueText.text = text;
        isDialogueActive = true;
    }

    // 손님 데이터에 지정된 고민 대사를 출력
    public void ShowDialogue(CustomerData customer)
    {
        ShowDialogue(customer != null ? customer.Dialogue.worryText : string.Empty);
    }

    private void AdvanceDialogue()
    {
        isDialogueActive = false; // 중복 트리거 방지
        OnDialogueAdvanced?.Invoke();
    }
}
