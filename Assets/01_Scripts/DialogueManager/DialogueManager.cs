using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [Header("대화창 UI")]
    [SerializeField] private GameObject dialoguePanel;
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

    // 손님 데이터에 지정된 고민 대사를 대화창에 출력
    public void ShowDialogue(CustomerData customer)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (dialogueText != null && customer != null) dialogueText.text = customer.worryText;
        isDialogueActive = true;
    }

    public void HideDialogue()
    {
        isDialogueActive = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    private void AdvanceDialogue()
    {
        isDialogueActive = false; // 중복 트리거 방지
        OnDialogueAdvanced?.Invoke();
    }
}
