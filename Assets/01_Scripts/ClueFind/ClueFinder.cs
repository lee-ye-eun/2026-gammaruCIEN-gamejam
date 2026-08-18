using UnityEngine;
using UnityEngine.UI;

public class ClueFinder : MonoBehaviour
{
    [SerializeField] private Button confirmButton;

    private void Awake()
    {
        if (confirmButton != null) confirmButton.onClick.AddListener(ConfirmClueFinding);
    }

    private void OnDestroy()
    {
        if (confirmButton != null) confirmButton.onClick.RemoveListener(ConfirmClueFinding);
    }

    // 단서 찾기 화면의 "다음(질문하기로)" 버튼 onClick에 연결
    public void ConfirmClueFinding()
    {
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.Questioning);
    }
}
