using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    public struct QuestionLogEntry
    {
        public string question;
        public string answer;
    }

    [Header("스탯")]
    [SerializeField] private int questionAskedCount; // 질문 횟수 (현재 손님 기준, 결과창에서 초기화)
    [SerializeField] private int clueFindCount; // 단서 찾기 횟수 (현재 손님 기준, 결과창에서 초기화)
    [SerializeField] private int suspicionLevel; // 의심도 (전체 누적)
    [SerializeField] private int currentCustomerSuspicion; // 현재 손님 의심도 (결과창에서 초기화)

    public int QuestionAskedCount => questionAskedCount;
    public int ClueFindCount => clueFindCount;
    public int SuspicionLevel => suspicionLevel;
    public int CurrentCustomerSuspicion => currentCustomerSuspicion;

    public static GameManager Instance { get; private set; }

    // 게임 전체 흐름 상태. 버튼/터치는 ChangeState(원하는 상태)를 직접 호출해서 전환한다.
    public enum GameState { Observing, ClueFinding, Questioning, CardSelecting, ShowingCardResult, ShowingResultPanel, FinalResult }

    // 현재 상태 저장 변수. 시작 상태는 Observing(손님 고민 대사 출력)
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
        ChangeState(GameState.Observing);
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
                EnterObserving();
                break;
            case GameState.ClueFinding:
                EnterClueFinding();
                break;
            case GameState.Questioning:
                EnterQuestioning();
                break;
            case GameState.CardSelecting:
                EnterCardSelecting();
                break;
            case GameState.ShowingCardResult:
                EnterShowingCardResult();
                break;
            case GameState.ShowingResultPanel:
                EnterShowingResultPanel();
                break;
            case GameState.FinalResult:
                EnterFinalResult();
                break;
        }
    }

    public void IncrementQuestionAskedCount()
    {
        questionAskedCount++;
    }

    public void IncrementClueFindCount()
    {
        clueFindCount++;
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
    // 더 이상 손님이 없으면 최종 결과(FinalResult, 점수/평판 등은 아직 미구현)로 전환.
    private void EnterObserving()
    {
        customerIndex++;

        if (customers == null || customerIndex >= customers.Count)
        {
            ChangeState(GameState.FinalResult);
            return;
        }

        currentCustomer = customers[customerIndex];
        questionLogs.Clear();
        if (customerView != null) customerView.SetData(currentCustomer);

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

    // 질문이 끝나고(질문창의 "다음" 버튼) 카드 덱으로 넘어올 때: 질문 횟수에 따라 현재 손님 의심도에 보너스를 더한다
    private void EnterCardSelecting()
    {
        if (questionAskedCount == 2) currentCustomerSuspicion += 5;
        else if (questionAskedCount == 3) currentCustomerSuspicion += 10;
        else if (questionAskedCount >= 4) currentCustomerSuspicion += 20;

        SetPanels(dialogue: false, clueFinding: false, cardDeckOn: true, result: false);
        if (cardDeck != null) cardDeck.PrepareForNewRound();
    }

    // 카드 제출 후 결과 대사 출력. 오답 개수에 따라 현재 손님 의심도를 가감한다.
    private void EnterShowingCardResult()
    {
        SetPanels(dialogue: true, clueFinding: false, cardDeckOn: false, result: false);

        int matchCount = CalculateMatchCount();
        int wrongCount = 3 - matchCount;
        currentCustomerSuspicion += GetCardResultSuspicionDelta(wrongCount);

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

        suspicionLevel += currentCustomerSuspicion;

        string text = $"손님의 의심도: {currentCustomerSuspicion}\n전체의심도: {suspicionLevel}";
        if (resultManager != null) resultManager.ShowResult(text);

        questionAskedCount = 0;
        clueFindCount = 0;
        currentCustomerSuspicion = 0;
    }

    // 손님이 전부 끝나면 표시 (점수/평판 등은 아직 미구현)
    private void EnterFinalResult()
    {
        SetPanels(dialogue: false, clueFinding: false, cardDeckOn: false, result: true);
    }

    // 슬롯(원인/현재/조언) 배치가 아직 없어서, 선택한 3장 중 정답 3장과 겹치는 개수로만 채점
    private int CalculateMatchCount()
    {
        if (cardDeck == null || currentCustomer == null) return 0;

        var answerCards = new HashSet<CardData>
        {
            currentCustomer.causeCard,
            currentCustomer.presentCard,
            currentCustomer.adviceCard
        };

        int count = 0;
        foreach (var selected in cardDeck.SelectedCards)
        {
            if (selected != null && answerCards.Contains(selected.Data)) count++;
        }

        return count;
    }
}
