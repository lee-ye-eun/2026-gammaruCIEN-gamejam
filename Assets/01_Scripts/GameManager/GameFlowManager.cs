using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private RawImage backgroundImage; // 손님별 배경. 씬에 맞춰둔 크기/위치는 건드리지 않고 텍스처만 교체한다.

    private CustomerData currentCustomer;
    private int customerIndex = -1;
    private int pendingCardResultSuspicionDelta; // 카드 결과로 정해진 의심도 변화량. 결과창 터치 시에만 실제로 반영됨.
    private readonly List<QuestionLogEntry> questionLogs = new List<QuestionLogEntry>();

    public string CurrentWorryText => currentCustomer != null ? currentCustomer.Dialogue.worryText : string.Empty;
    public IReadOnlyList<QuestionLogEntry> CurrentQuestionLogs => questionLogs;

    // 의심도 게이지 표시 조건: 카드를 고르는 중이면서 플레이어에게 카드 뒷면이 실제로 보이는 동안에만 true.
    // 손님 등장(Observing) / 단서 찾기(ClueFinding) / 카드 결과 / 결과창에서는 false이고, 투시경을 켠 동안에도 false.
    public bool AreCardBacksVisible => currentState == GameState.CardSelecting
        && cardDeck != null
        && cardDeck.AreCardBacksVisible;

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

    [Header("튜토리얼 (선택, 비워두면 무시됨)")]
    [SerializeField] private TutorialPanel tutorialPanel;

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

        // 튜토리얼이 아직 살아있으면(첫 손님이 끝나기 전) 이 상태에 맞는 스텝을 보여준다.
        if (tutorialPanel != null) tutorialPanel.NotifyStateChanged(newState);

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

        // 첫 손님(customerIndex == 0)이 끝나고 다음 손님으로 넘어가는 시점 -> 튜토리얼 종료.
        if (customerIndex > 0 && tutorialPanel != null) tutorialPanel.CompleteTutorial();

        currentCustomer = customers[customerIndex];
        questionLogs.Clear();
        if (customerView != null) customerView.SetData(currentCustomer);
        // 배경 텍스처만 갈아끼운다. 크기/위치는 씬에서 맞춰둔 값을 그대로 쓰므로 RectTransform은 손대지 않는다.
        if (backgroundImage != null && currentCustomer.background != null) backgroundImage.texture = currentCustomer.background;
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

    // 카드 제출 후: 선택한 카드 3장의 위치별 해석(원인/현재/조언, 화자는 주인공)을 순서대로 보여준 다음,
    // 손님 대사로 "..."(표정1) -> 맞춘 개수에 따른 손님 반응 대사(3개 전부면 표정2, 아니면 표정3)를 보여준다.
    // 의심도 변화량은 여기서 정해두기만 하고, 실제로 반영하지는 않는다
    // (결과창을 터치해 넘어가려는 시점에 CommitCardResultSuspicion()이 반영함).
    private void EnterShowingCardResult()
    {
        SetPanels(dialogue: true, clueFinding: false, cardDeckOn: false, result: false);

        int matchCount = cardDeck != null && currentCustomer != null ? cardDeck.CountCorrectSlots(currentCustomer) : 0;
        int wrongCount = 3 - matchCount;
        pendingCardResultSuspicionDelta = GetCardResultSuspicionDelta(wrongCount);

        string reaction = currentCustomer != null ? currentCustomer.GetReaction(matchCount) : string.Empty;
        CustomerData customerForExpression = currentCustomer;

        List<(string text, string speaker, System.Action onShow)> lines = GetSelectedCardMeaningLines();
        lines.Add(("...", DialogueManager.CustomerSpeakerLabel,
            () => SetCustomerExpression(customerForExpression != null ? customerForExpression.expression1 : null)));
        lines.Add((reaction, DialogueManager.CustomerSpeakerLabel,
            () => SetCustomerExpression(customerForExpression != null ? customerForExpression.GetResultExpression(matchCount) : null)));

        if (dialogueManager != null)
        {
            dialogueManager.ShowDialogueSequence(lines, () => ChangeState(GameState.ShowingResultPanel));
        }
    }

    private void SetCustomerExpression(Sprite expressionSprite)
    {
        if (customerView != null) customerView.SetExpression(expressionSprite);
    }

    // 선택된 3장의 카드를 슬롯 순서(원인/현재/조언)대로, 그 위치에 해당하는 해석 대사만 뽑아 모은다. 화자는 주인공.
    // 카드에 그 위치의 해석이 비어 있으면 건너뛴다.
    private List<(string text, string speaker, System.Action onShow)> GetSelectedCardMeaningLines()
    {
        var lines = new List<(string text, string speaker, System.Action onShow)>();
        if (cardDeck == null) return lines;

        IReadOnlyList<CardView> selected = cardDeck.SelectedCards;
        AddMeaningLine(lines, selected, 0, data => data.causeMeaning);
        AddMeaningLine(lines, selected, 1, data => data.presentMeaning);
        AddMeaningLine(lines, selected, 2, data => data.adviceMeaning);
        return lines;
    }

    private static void AddMeaningLine(List<(string text, string speaker, System.Action onShow)> lines, IReadOnlyList<CardView> selected, int index, System.Func<CardData, string> pickMeaning)
    {
        CardData data = index < selected.Count && selected[index] != null ? selected[index].Data : null;
        string line = data != null ? pickMeaning(data) : null;
        if (!string.IsNullOrWhiteSpace(line)) lines.Add((line, DialogueManager.ProtagonistSpeakerLabel, null));
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

    // 결과창: 카드 결과로 정해진 의심도 변화량을 미리보기로 안내만 하고(아직 반영 안 함), 다음 손님을 위해
    // 라운드 스탯을 초기화한다. 터치하면 CommitCardResultSuspicion()이 실제로 반영함(Result가 처리).
    private void EnterShowingResultPanel()
    {
        SetPanels(dialogue: false, clueFinding: false, cardDeckOn: false, result: true);

        if (GameManager.Instance != null)
        {
            // 현재 손님의 의심도 = 이번 라운드 질문으로 이미 반영된 값 + 아직 반영 전인 카드 결과 변화량.
            int previewCustomerSuspicion = Mathf.Max(0, GameManager.Instance.CurrentCustomerSuspicion + pendingCardResultSuspicionDelta);
            // 현재 의심도(전체) = 카드 결과 변화량까지 반영됐을 때의 미리보기 값.
            int previewSuspicionLevel = Mathf.Max(0, GameManager.Instance.SuspicionLevel + pendingCardResultSuspicionDelta);
            int beforeSuspicionLevel = Mathf.Max(0, previewSuspicionLevel - previewCustomerSuspicion);

            string text = $"의심도 변화: {beforeSuspicionLevel} -> {previewSuspicionLevel}";
            if (resultManager != null) resultManager.ShowResult(text);

            GameManager.Instance.ResetRoundStats();
        }
    }

    // 결과창을 터치해 다음으로 넘어가려 할 때 Result가 호출: 미뤄둔 카드 결과 의심도를 이제 실제로 반영한다.
    // 이 반영으로 의심도가 임계치를 처음 넘으면 GameManager.AddSuspicion() 내부에서 곧장 스토리 씬으로 이동한다.
    public void CommitCardResultSuspicion()
    {
        if (GameManager.Instance != null) GameManager.Instance.AddSuspicion(pendingCardResultSuspicionDelta);
        pendingCardResultSuspicionDelta = 0;
    }
}
