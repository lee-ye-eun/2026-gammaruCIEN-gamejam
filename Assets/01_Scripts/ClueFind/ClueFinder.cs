using UnityEngine;
using UnityEngine.UI;

// 단서 찾기 화면. 손님 프리팹 자식으로 있는 단서 이미지 3개를 클릭하면 대사창에 해당 단서 텍스트를 보여준다.
// "다음(질문하기로)" 버튼은 인스펙터에서 이 컴포넌트의 ConfirmClueFinding()을 참조하도록 연결한다.
public class ClueFinder : MonoBehaviour
{
    [Header("단서 이미지 3개 (손님 프리팹의 자식, Button 컴포넌트 필요)")]
    [SerializeField] private Button[] clueButtons;

    private CustomerData currentCustomer;

    private void Awake()
    {
        for (int i = 0; i < clueButtons.Length; i++)
        {
            int index = i; // 클로저 캡처용
            if (clueButtons[i] != null) clueButtons[i].onClick.AddListener(() => HandleClueButtonClicked(index));
        }
    }

    private void OnDestroy()
    {
        foreach (var button in clueButtons)
        {
            if (button != null) button.onClick.RemoveAllListeners();
        }
    }

    // GameFlowManager가 단서 찾기 화면 진입 시 호출: 지금 손님의 단서 데이터를 기억해둔다
    public void ShowClues(CustomerData customer)
    {
        currentCustomer = customer;
    }

    // 단서 찾기 상태가 아닐 때 GameFlowManager가 호출: 클릭(interactable)과 호버(raycastTarget) 둘 다 막는다
    public void SetInteractable(bool enabled)
    {
        foreach (var button in clueButtons)
        {
            if (button == null) continue;

            button.interactable = enabled;
            if (button.targetGraphic != null) button.targetGraphic.raycastTarget = enabled;
        }
    }

    private void HandleClueButtonClicked(int index)
    {
        var clues = currentCustomer != null ? currentCustomer.Dialogue.clues : null;
        if (clues == null || index >= clues.Count) return;

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.ShowClueText(clues[index]);
        if (GameManager.Instance != null) GameManager.Instance.IncrementClueFindCount();
    }

    // 단서 찾기 화면의 "다음(질문하기로)" 버튼 onClick에 연결
    public void ConfirmClueFinding()
    {
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.ChangeState(GameFlowManager.GameState.Questioning);
    }
}
