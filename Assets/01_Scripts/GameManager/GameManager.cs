using UnityEngine;
using UnityEngine.SceneManagement;

// 전역(씬을 넘어 유지되는) 게임 매니저. 스탯과 씬 이동만 담당한다.
// GameScene 내부의 진행(대화/단서찾기/카드덱/결과창 흐름)과 그 참조들은 GameFlowManager(씬 로컬)가 담당한다.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private const string StorySceneName = "StoryScene";
    private const int SuspicionThreshold = 100;

    // StoryScene으로 넘어간 이유. StoryDisplay가 이 값을 보고 어떤 StoryData를 보여줄지 고른다.
    public enum StoryTrigger { Prologue, CustomersExhausted, HighSuspicion }

    [SerializeField] private StoryTrigger lastStoryTrigger;
    public StoryTrigger LastStoryTrigger => lastStoryTrigger;

    [Header("스탯")]
    [SerializeField] private int questionAskedCount; // 질문 횟수 (현재 손님 기준, 결과창에서 초기화)
    [SerializeField] private int clueFindCount; // 단서 찾기 횟수 (현재 손님 기준, 결과창에서 초기화)
    [SerializeField] private int suspicionLevel; // 의심도 (전체 누적)
    [SerializeField] private int currentCustomerSuspicion; // 현재 손님 의심도 (결과창에서 초기화)

    public int QuestionAskedCount => questionAskedCount;
    public int ClueFindCount => clueFindCount;
    public int SuspicionLevel => suspicionLevel;
    public int CurrentCustomerSuspicion => currentCustomerSuspicion;
    public bool IsSuspicionThresholdReached => suspicionLevel >= SuspicionThreshold;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void IncrementQuestionAskedCount()
    {
        questionAskedCount++;
    }

    public void IncrementClueFindCount()
    {
        clueFindCount++;
    }

    public void AddCurrentCustomerSuspicion(int amount)
    {
        currentCustomerSuspicion += amount;
    }

    // 현재 손님 의심도를 전체 의심도에 반영한다 (currentCustomerSuspicion 자체는 아직 초기화하지 않음 - 결과창 텍스트에서 필요)
    public void CommitCurrentCustomerSuspicion()
    {
        suspicionLevel += currentCustomerSuspicion;
    }

    // 다음 손님으로 넘어가기 전, 이번 라운드에서 쓴 스탯을 전부 초기화
    public void ResetRoundStats()
    {
        questionAskedCount = 0;
        clueFindCount = 0;
        currentCustomerSuspicion = 0;
    }

    public void GoToStoryScene(StoryTrigger trigger)
    {
        lastStoryTrigger = trigger;
        SceneManager.LoadScene(StorySceneName);
    }
}
