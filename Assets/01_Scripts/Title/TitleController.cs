using UnityEngine;

// Title 씬에 배치. 버튼 onClick에서 각각 이 컴포넌트의 StartGame()/QuitGame()을 참조하도록 연결한다.
public class TitleController : MonoBehaviour
{
    // Title 씬에 도착하면(재도전이든 최초 실행이든) 무조건 이전 플레이의 누적 스탯을 초기화한다.
    private void Awake()
    {
        if (GameManager.Instance != null) GameManager.Instance.ResetToInitialState();
    }

    // "시작하기" 버튼 onClick에 연결 -> 프롤로그 스토리로 이동 (LoadingScene을 거쳐 비동기로 로드됨)
    public void StartGame()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("TitleController: GameManager.Instance가 없습니다. Title 씬에도 GameManager가 배치되어 있는지 확인하세요.");
            return;
        }

        GameManager.Instance.GoToStoryScene(GameManager.StoryTrigger.Prologue);
    }

    // "게임 종료" 버튼 onClick에 연결
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
