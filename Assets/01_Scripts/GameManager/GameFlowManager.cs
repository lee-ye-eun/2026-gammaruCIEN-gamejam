using System.Collections.Generic;
using UnityEngine;

// GameScene 안에서의 진행(대화/단서찾기/질문/카드덱/결과창)과 그 씬 로컬 참조를 전담.
// 전역 상태(스탯)와 씬 전환은 GameManager(DontDestroyOnLoad)에 위임한다.
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [System.Serializable]
    public struct QuestionLogEntry
    {
        public string question;
        public string answer;
    }

    // GameScene 내부 흐름 상태. 버튼/터치는 ChangeState(원하는 상태)를 직접 호출해서 전환한다.
    // CardSelecting: 질문 패널과 카드덱 패널이 동시에 활성화되는 상태. 카드를 고르는 동안 언제든 질문할 수 있다.
    public enum GameState { Observing, ClueFinding, CardSelecting, ShowingCardResult, ShowingResultPanel }

    [SerializeField] private GameState currentState = GameState.Observing;
    public GameState CurrentState => currentState;

    [Header("손님")]
    [SerializeField] private CustomerView customerView; // 맵에 있는 손님 프리팹
    [SerializeField] private List<CustomerData> customers = new List<CustomerData>(); // 미리 받아둔 손님 목록

    private CustomerData currentCustomer;
    private int customerIndex = -1;
    private readonly List<QuestionLogEntry> questionLogs = new List<QuestionLogEntry>();

    public string CurrentWorryText => currentCustomer != null ? currentCustomer.Dialogue.worryText : string.Empty;
    public IReadOnlyList<QuestionLogEntry> CurrentQuestionLogs => questionLogs;

    [Header("대화 (Observing / CardSelecting의 질문 답변 / ShowingCardResult 공용)")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private DialogueManager dialogueManager;

    [Header("단서 찾기")]
    [SerializeField] private GameObject clueFindingPanel;
    [SerializeField] private ClueFinder clueFinder;

    [Header("카드 덱")]
    [SerializeField] private GameObject cardDeckPanel; // CardDeck 컴포넌트가 붙어있는 오브젝트
    [SerializeField] private CardDeck cardDeck;
    [SerializeField] private GameObject paperPanel; // 카드 선택 상태(CardSelecting)에서만 활성화

    [Header("결과창 (카드 결과 대사 다음, 매 손님마다 표시)")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Result resultManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayBGM("MainBGM");
        ChangeState(GameState.Observing);
    }

    private static void PlaySfx(string key)
    {
        if (string.IsNullOrEmpty(key) || SoundManager.Instance == null) return;

        SoundManager.Instance.PlaySFX(key);
    }

    // 상태 전환 함수. 버튼/터치 핸들러가 원하는 상태를 넘기면 그 상태의 진입 로직을 실행한다.
    public void ChangeState(GameState newState)
    {
        currentState = newState;

        // 단서 찾기 상태일 때만 단서 이미지 호버/클릭이 가능하도록
        if (clueFinder != null) clueFinder.SetInteractable(newState == GameState.ClueFinding);

        switch (newState)
        {
            case GameState.Observing:
                PlaySfx("doorbell");
                EnterObserving();
                break;
            case GameState.ClueFinding:
                PlaySfx("chhhhhik");
                EnterClueFinding();
                break;
            case GameState.CardSelecting:
                PlaySfx("chhhhhik");
                EnterCardSelecting();
                break;
            case GameState.ShowingCardResult:
                PlaySfx("baam");
                EnterShowingCardResult();
                break;
            case GameState.ShowingResultPanel:
                PlaySfx("brrr");
                EnterShowingResultPanel();
                break;
        }
    }

    public void RegisterQuestionLog(string question, string answer)
    {
        if (string.IsNullOrWhiteSpace(question) && string.IsNullOrWhiteSpace(answer)) return;

        for (int i = 0; i < questionLogs.Count; i++)
        {
            if (questionLogs[i].question != question) continue;

            questionLogs[i] = new QuestionLogEntry { question = question, answer = answer };
            return;
        }

        questionLogs.Add(new QuestionLogEntry { question = question, answer = answer });
    }

    private void SetPanels(bool dialogue, bool clueFinding, bool cardDeckOn, bool result)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(dialogue);
        if (clueFindingPanel != null) clueFindingPanel.SetActive(clueFinding);
        if (cardDeckPanel != null) cardDeckPanel.SetActive(cardDeckOn);
        if (resultPanel != null) resultPanel.SetActive(result);
        if (paperPanel != null) paperPanel.SetActive(cardDeckOn);
    }

    // 손님 목록에서 다음 손님을 꺼내 맵의 손님 프리팹에 새로 할당하고 관찰 대사를 출력.
    // 더 이상 손님이 없으면 GameManager에 위임해 스토리 씬으로 이동한다.
    // (의심도 임계치 초과는 더 이상 여기서 확인하지 않는다 - GameManager.AddSuspicion()이 임계치를 넘는 즉시 스토리 씬으로 보낸다.)
    private void EnterObserving()
    {
        customerIndex++;

        bool noMoreCustomers = customers == null || customerIndex >= customers.Count;
        if (noMoreCustomers)
        {
            if (GameManager.Instance != null) GameManager.Instance.GoToStoryScene(GameManager.StoryTrigger.CustomersExhausted);
            return;
        }

        currentCustomer = customers[customerIndex];
        questionLogs.Clear();
        if (customerView != null) customerView.SetData(currentCustomer);
        // 새 라운드 시작: 카드덱을 여기서 한 번만 리셋해둔다 (라운드당 CardSelecting은 한 번만 진입하므로 충분).
        if (cardDeck != null) cardDeck.PrepareForNewRound();

        SetPanels(dialogue: true, clueFinding: false, cardDeckOn: false, result: false);
        if (dialogueManager != null) dialogueManager.ShowDialogue(currentCustomer);
    }

    private void EnterClueFinding()
    {
        SetPanels(dialogue: false, clueFinding: true, cardDeckOn: false, result: false);
        if (clueFinder != null) clueFinder.ShowClues(currentCustomer);
    }

    // 단서 이미지 클릭 시(ClueFinder가 호출): 단서 찾기 패널 끄고 대사창에 단서 텍스트 출력. ClueFinding 상태는 그대로 유지.
    // 다이얼로그 패널이 떠 있는 동안엔 단서 이미지가 호버/클릭되지 않도록 막는다.
    public void ShowClueText(string text)
    {
        if (clueFindingPanel != null) clueFindingPanel.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (dialogueManager != null) dialogueManager.ShowDialogue(text);
        if (clueFinder != null) clueFinder.SetInteractable(false);
    }

    // 단서 텍스트를 보다가 화면 터치 시(DialogueManager가 호출): 단서 찾기 패널로 복귀. ClueFinding 상태는 그대로 유지.
    // 다이얼로그 패널이 닫혔으니 단서 이미지를 다시 호버/클릭 가능하게 되돌린다.
    public void ReturnToClueFinding()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (clueFindingPanel != null) clueFindingPanel.SetActive(true);
        if (clueFinder != null) clueFinder.SetInteractable(true);
    }

    // 단서 찾기 다음 진입점. 질문 패널과 카드덱 패널을 동시에 켜서, 카드를 고르는 동안 언제든 질문할 수 있게 한다.
    // 라운드당 한 번만 진입하므로 카드는 여기서 바로 뽑는다 (재준비는 라운드 시작 시점인 EnterObserving()이 담당).
    private void EnterCardSelecting()
    {
        SetPanels(dialogue: true, clueFinding: false, cardDeckOn: true, result: false);
        if (dialogueManager != null) dialogueManager.ShowQuestionTurn(currentCustomer);
        if (cardDeck != null) cardDeck.DrawCards();
    }

    // 카드 제출 후 결과 대사 출력. 맞춘 카드 개수에 따른 의심도 가감만 반영한다.
    // (질문에 따른 의심도는 더 이상 여기서 정산하지 않는다 - GameManager.IncrementQuestionAskedCount()에서 질문할 때마다 즉시 반영됨)
    private void EnterShowingCardResult()
    {
        SetPanels(dialogue: true, clueFinding: false, cardDeckOn: false, result: false);

        int matchCount = cardDeck != null && currentCustomer != null ? cardDeck.CountCorrectSlots(currentCustomer) : 0;
        int wrongCount = 3 - matchCount;
        if (GameManager.Instance != null) GameManager.Instance.AddSuspicion(GetCardResultSuspicionDelta(wrongCount));

        string reaction = currentCustomer != null ? currentCustomer.GetReaction(matchCount) : string.Empty;
        if (dialogueManager != null) dialogueManager.ShowDialogue(reaction);
    }

    private int GetCardResultSuspicionDelta(int wrongCount)
    {
        switch (wrongCount)
        {
            case 0: return -20;
            case 1: return 15;
            case 2: return 25;
            case 3: return 35;
            default: return 0;
        }
    }

    // 결과창: 이번 라운드 동안 오른 의심도(질문+카드 결과, 이미 전체 의심도에 실시간으로 반영되어 있음)를
    // 문구로 안내하고, 다음 손님을 위해 라운드 스탯을 초기화한다. 터치하면 다음 손님으로 넘어감(ResultManager가 처리)
    private void EnterShowingResultPanel()
    {
        SetPanels(dialogue: false, clueFinding: false, cardDeckOn: false, result: true);

        if (GameManager.Instance != null)
        {
            string text = $"의심도가 {GameManager.Instance.CurrentCustomerSuspicion}만큼 증가했습니다.";
            if (resultManager != null) resultManager.ShowResult(text);

            GameManager.Instance.ResetRoundStats();
        }
    }
}
