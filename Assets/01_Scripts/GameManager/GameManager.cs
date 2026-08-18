using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("스탯")]
    [SerializeField] private int questionAskedCount; // 질문 횟수 (전체 누적)
    [SerializeField] private int suspicionLevel; // 의심도 (전체 누적)
    [SerializeField] private int currentCustomerSuspicion; // 현재 손님 의심도

    public int QuestionAskedCount => questionAskedCount;
    public int SuspicionLevel => suspicionLevel;
    public int CurrentCustomerSuspicion => currentCustomerSuspicion;
    public CustomerData CurrentCustomer => currentCustomer;

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

    [Header("대화 (Observing / Questioning / ShowingCardResult 공용)")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private DialogueManager dialogueManager;

    [Header("단서 찾기")]
    [SerializeField] private GameObject clueFindingPanel;

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

    public void AddSuspicion(int amount)
    {
        if (amount <= 0) return;

        suspicionLevel = Mathf.Clamp(suspicionLevel + amount, 0, 100);
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
        if (customerView != null) customerView.SetData(currentCustomer);

        SetPanels(dialogue: true, clueFinding: false, cardDeckOn: false, result: false);
        if (dialogueManager != null) dialogueManager.ShowDialogue(currentCustomer);
    }

    private void EnterClueFinding()
    {
        SetPanels(dialogue: false, clueFinding: true, cardDeckOn: false, result: false);
    }

    private void EnterQuestioning()
    {
        SetPanels(dialogue: true, clueFinding: false, cardDeckOn: false, result: false);
        if (dialogueManager != null) dialogueManager.ShowQuestionTurn(currentCustomer);
    }

    private void EnterCardSelecting()
    {
        SetPanels(dialogue: false, clueFinding: false, cardDeckOn: true, result: false);
        if (cardDeck != null) cardDeck.PrepareForNewRound();
    }

    // 카드 제출 후 결과 대사 출력 (대사창 재사용해서 한 줄만 보여주고 터치하면 결과창으로)
    private void EnterShowingCardResult()
    {
        SetPanels(dialogue: true, clueFinding: false, cardDeckOn: false, result: false);

        int matchCount = CalculateMatchCount();
        int wrongCount = MaxTarotAnswerCount - matchCount;
        int suspicionPenalty = GetCardResultSuspicionPenalty(wrongCount);
        currentCustomerSuspicion = suspicionPenalty;
        AddSuspicion(suspicionPenalty);

        string reaction = currentCustomer != null ? currentCustomer.GetReaction(matchCount) : string.Empty;
        if (dialogueManager != null) dialogueManager.ShowDialogue(reaction);
    }

    // 결과창: 현재 손님 의심도를 출력. 터치하면 다음 손님으로 넘어감(ResultManager가 처리)
    private void EnterShowingResultPanel()
    {
        SetPanels(dialogue: false, clueFinding: false, cardDeckOn: false, result: true);
        if (resultManager != null) resultManager.ShowResult($"이번 손님 의심도 +{currentCustomerSuspicion}\n전체 의심도 {suspicionLevel}/100");
    }

    // 손님이 전부 끝나면 표시 (점수/평판 등은 아직 미구현)
    private void EnterFinalResult()
    {
        SetPanels(dialogue: false, clueFinding: false, cardDeckOn: false, result: true);
        if (resultManager != null)
        {
            string finalText = suspicionLevel >= 100
                ? $"배드 엔딩\n의심도 {suspicionLevel}/100"
                : $"오늘의 상담 종료\n의심도 {suspicionLevel}/100";
            resultManager.ShowResult(finalText);
        }
    }

    private const int MaxTarotAnswerCount = 3;

    // 원인/현재/조언 슬롯의 카드와 손님 데이터의 정답 카드를 위치까지 비교한다.
    private int CalculateMatchCount()
    {
        if (cardDeck == null || currentCustomer == null) return 0;

        return cardDeck.CountCorrectSlots(currentCustomer);
    }

    private int GetCardResultSuspicionPenalty(int wrongCount)
    {
        switch (wrongCount)
        {
            case 0:
                return 0;
            case 1:
                return 15;
            case 2:
                return 25;
            default:
                return 35;
        }
    }
}
