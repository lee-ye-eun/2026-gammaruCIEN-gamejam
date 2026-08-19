using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 전역(씬을 넘어 유지되는) 게임 매니저. 스탯과 씬 이동만 담당한다.
// GameScene 내부의 진행(대화/단서찾기/카드덱/결과창 흐름)과 그 참조들은 GameFlowManager(씬 로컬)가 담당한다.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private const string StorySceneName = "StoryScene";
    private const string LoadingSceneName = "LoadingScene";
    private const int SuspicionThreshold = 100;

    // StoryScene으로 넘어간 이유. StoryDisplay가 이 값을 보고 어떤 StoryData를 보여줄지 고른다.
    public enum StoryTrigger { Prologue, CustomersExhausted, HighSuspicion }

    [SerializeField] private StoryTrigger lastStoryTrigger;
    public StoryTrigger LastStoryTrigger => lastStoryTrigger;

    // LoadingScene을 거쳐 다음에 로드할 씬 이름. LoadingSceneController가 이 값을 읽어서 비동기로 로드한다.
    private string pendingSceneName;
    public string PendingSceneName => pendingSceneName;

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

    // 질문할 때마다 몇 번째 질문인지에 따라 의심도가 즉시 오른다: 1회차는 없음, 2~3회차는 +5, 4회차부터는 +10.
    public void IncrementQuestionAskedCount()
    {
        questionAskedCount++;
        AddSuspicion(GetQuestionSuspicionDelta(questionAskedCount));
    }

    public void IncrementClueFindCount()
    {
        clueFindCount++;
    }

    // 의심도 증감은 즉시 현재 손님 의심도와 전체 의심도 양쪽에 반영한다. 둘 다 음수로 내려가지 않는다(0이 최솟값).
    // 예: 15에서 -30 -> 0, 이후 +25 -> 25.
    // 전체 의심도가 임계치를 처음 넘는 그 순간 곧장 스토리 씬으로 이동한다 (여러 번 다시 넘지 않도록 이 프레임에만 발동).
    public void AddSuspicion(int amount)
    {
        bool wasBelowThreshold = suspicionLevel < SuspicionThreshold;

        currentCustomerSuspicion = Mathf.Max(0, currentCustomerSuspicion + amount);
        suspicionLevel = Mathf.Max(0, suspicionLevel + amount);

        if (wasBelowThreshold && IsSuspicionThresholdReached)
        {
            GoToStoryScene(StoryTrigger.HighSuspicion);
        }
    }

    private static int GetQuestionSuspicionDelta(int questionNumber)
    {
        if (questionNumber <= 1) return 0;
        if (questionNumber <= 3) return 5;
        return 10;
    }

    // 다음 손님으로 넘어가기 전, 이번 라운드에서 쓴 스탯을 전부 초기화
    public void ResetRoundStats()
    {
        questionAskedCount = 0;
        clueFindCount = 0;
        currentCustomerSuspicion = 0;
    }

    // Title 씬 도착 시 무조건 호출: 이전 플레이의 누적 스탯을 전부 초기화해 새 게임을 준비한다.
    public void ResetToInitialState()
    {
        ResetRoundStats();
        suspicionLevel = 0;
        lastStoryTrigger = StoryTrigger.Prologue;
    }

    public void GoToStoryScene(StoryTrigger trigger)
    {
        lastStoryTrigger = trigger;
        LoadSceneWithLoading(StorySceneName);
    }

    // LoadingScene을 먼저 띄우고, 그 안에서 targetSceneName을 비동기로 로드하게 한다.
    public void LoadSceneWithLoading(string targetSceneName)
    {
        pendingSceneName = targetSceneName;
        StartCoroutine(LoadLoadingSceneAsync());
    }

    // 이전 씬을 Additive로 로딩씬 "위에" 먼저 띄워서 화면에 보이게 한 다음, 그 이후에 이전 씬을 언로드한다.
    // Single 모드로 바로 로드하면 이전 씬(스프라이트 등 리소스가 많은 씬)의 언로드가 로딩씬이 뜨기도 전에
    // 동기적으로 일어나서, 로딩씬 진입 직전에 버벅이는 것처럼 보인다.
    private IEnumerator LoadLoadingSceneAsync()
    {
        Scene previousScene = SceneManager.GetActiveScene();

        yield return SceneManager.LoadSceneAsync(LoadingSceneName, LoadSceneMode.Additive);

        Scene loadingScene = SceneManager.GetSceneByName(LoadingSceneName);
        if (loadingScene.IsValid()) SceneManager.SetActiveScene(loadingScene);

        yield return SceneManager.UnloadSceneAsync(previousScene);
    }
}
