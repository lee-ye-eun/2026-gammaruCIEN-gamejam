using UnityEngine;

public class ClueFinder : MonoBehaviour
{
    // 단서 찾기 화면의 "다음(질문하기로)" 버튼 onClick에 연결
    public void ConfirmClueFinding()
    {
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.Questioning);
    }
}
