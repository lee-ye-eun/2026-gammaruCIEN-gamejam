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
    public enum GameState { Observing, ClueFinding, Questioning, CardSelecting, ShowingCardResult, ShowingResultPanel }

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

    [Header("대화 (Observing / Questioning / ShowingCardResult 공용)")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private DialogueManager dialogueManager;

    [Header("단서 찾기")]
    [SerializeField] private GameObject clueFindingPanel;
    [SerializeField] private ClueFinder clueFinder;

    [Header("카드 덱")]
    [SerializeField] private GameObject cardDeckPanel; // CardDeck 컴포넌트가 붙어있는 오브젝트
    [SerializeField] private CardDeck cardDeck;

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
            case GameState.Questioning:
                PlaySfx("chhhhhik");
                EnterQuestioning();
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
    }

    // 손님 목록에서 다음 손님을 꺼내 맵의 손님 프리팹에 새로 할당하고 관찰 대사를 출력.
    // 더 이상 손님이 없거나 의심도가 임계치를 넘었으면 GameManager에 위임해 스토리 씬으로 이동.
    private void EnterObserving()
    {
        customerIndex++;

        bool noMoreCustomers = customers == null || customerIndex >= customers.Count;
        bool suspicionExceeded = GameManager.Instance != null && GameManager.Instance.IsSuspicionThresholdReached;

        if (noMoreCustomers || suspicionExceeded)
        {
            if (GameManager.Instance != null)
            {
                var trigger = suspicionExceeded ? GameManager.StoryTrigger.HighSuspicion : GameManager.StoryTrigger.CustomersExhausted;
                GameManager.Instance.GoToStoryScene(trigger);
            }
            return;
        }

        currentCustomer = customers[customerIndex];
        questionLogs.Clear();
        if (customerView != null) customerView.SetData(currentCustomer);
        // 새 라운드 시작: 카드덱을 여기서 한 번만 리셋해둔다. EnterCardSelecting()은 이 라운드 안에서 몇 번을
        // 다시 들어오든(질문하기로 돌아갔다 오든) cardDeck.CardsDrawn을 보고 재준비 여부를 판단하므로,
        // 라운드 경계가 되는 시점(=여기)에서만 리셋해야 다음 손님으로 넘어가도 다시 뽑힌다.
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

    private void EnterQuestioning()
    {
        SetPanels(dialogue: true, clueFinding: false, cardDeckOn: false, result: false);
        if (dialogueManager != null) dialogueManager.ShowQuestionTurn(currentCustomer);
    }

    // 질문 답변을 하나 보거나(자동) "질문 그만" 버튼으로 카드 덱에 처음 넘어올 때: 질문 횟수에 따라 현재 손님
    // 의심도에 보너스를 더하고 카드를 뽑는다. 이후 "질문하기" 버튼으로 다시 질문했다가 돌아오는 경우엔
    // 이미 뽑혀 있으므로(cardDeck.CardsDrawn) 보너스 없이 패널만 다시 보여준다.
    // 카드덱 자체의 리셋(PrepareForNewRound)은 라운드 시작 시점인 EnterObserving()에서 한 번만 한다.
    private void EnterCardSelecting()
    {
        bool alreadyDrawn = cardDeck != null && cardDeck.CardsDrawn;

        if (!alreadyDrawn && GameManager.Instance != null)
        {
            int askedCount = GameManager.Instance.QuestionAskedCount;
            if (askedCount == 2) GameManager.Instance.AddCurrentCustomerSuspicion(5);
            else if (askedCount == 3) GameManager.Instance.AddCurrentCustomerSuspicion(10);
            else if (askedCount >= 4) GameManager.Instance.AddCurrentCustomerSuspicion(20);
        }

        SetPanels(dialogue: false, clueFinding: false, cardDeckOn: true, result: false);
        if (cardDeck != null) cardDeck.DrawCards();
    }

    // 카드 제출 후 결과 대사 출력. 오답 개수에 따라 현재 손님 의심도를 가감한다.
    private void EnterShowingCardResult()
    {
        SetPanels(dialogue: true, clueFinding: false, cardDeckOn: false, result: false);

        int matchCount = cardDeck != null && currentCustomer != null ? cardDeck.CountCorrectSlots(currentCustomer) : 0;
        int wrongCount = 3 - matchCount;
        if (GameManager.Instance != null) GameManager.Instance.AddCurrentCustomerSuspicion(GetCardResultSuspicionDelta(wrongCount));

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

    // 결과창: 전체 의심도에 현재 손님 의심도를 반영하고 텍스트로 출력한 뒤, 다음 손님을 위해 라운드 스탯을 초기화한다.
    // 터치하면 다음 손님으로 넘어감(ResultManager가 처리)
    private void EnterShowingResultPanel()
    {
        SetPanels(dialogue: false, clueFinding: false, cardDeckOn: false, result: true);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CommitCurrentCustomerSuspicion();

            string text = $"손님의 의심도: {GameManager.Instance.CurrentCustomerSuspicion}\n전체의심도: {GameManager.Instance.SuspicionLevel}";
            if (resultManager != null) resultManager.ShowResult(text);

            GameManager.Instance.ResetRoundStats();
        }
    }
}
