using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // 대화창이 지금 손님 관찰 대사를 보여주는 중인지, 카드 제출 후 결과 대사를 보여주는 중인지 구분
    private enum DialogueStage { Observing, ShowingCardResult }

    [Header("손님")]
    [SerializeField] private CustomerView customerView; // 맵에 있는 손님 프리팹
    [SerializeField] private List<CustomerData> customers = new List<CustomerData>(); // 미리 받아둔 손님 목록

    private CustomerData currentCustomer;
    private int customerIndex = -1;
    private DialogueStage dialogueStage;

    [Header("대화 (1. 손님 대사 / 3. 카드 결과 대사 공용)")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private DialogueManager dialogueManager;

    [Header("카드 덱 (2. 대사 넘김 시 활성화 + 펼치기)")]
    [SerializeField] private GameObject cardDeckPanel; // CardDeck 컴포넌트가 붙어있는 오브젝트
    [SerializeField] private CardDeck cardDeck;

    [Header("결과 (4. 손님이 전부 끝났을 때만 표시 - 점수 등은 아직 미구현)")]
    [SerializeField] private GameObject resultPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (dialogueManager != null) dialogueManager.OnDialogueAdvanced += OnDialogueAdvanced;
        if (cardDeck != null) cardDeck.OnNextConfirmed += OnCardDeckNextConfirmed;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (dialogueManager != null) dialogueManager.OnDialogueAdvanced -= OnDialogueAdvanced;
        if (cardDeck != null) cardDeck.OnNextConfirmed -= OnCardDeckNextConfirmed;
    }

    private void Start()
    {
        AdvanceToNextCustomer();
    }

    // 손님 목록에서 다음 손님을 꺼내 맵의 손님 프리팹에 새로 할당하고 관찰 대사부터 다시 시작.
    // 더 이상 손님이 없으면 결과창(점수 등, 아직 미구현) 표시.
    private void AdvanceToNextCustomer()
    {
        customerIndex++;

        if (customers == null || customerIndex >= customers.Count)
        {
            ShowFinalResult();
            return;
        }

        currentCustomer = customers[customerIndex];
        if (customerView != null) customerView.SetData(currentCustomer);

        if (cardDeckPanel != null) cardDeckPanel.SetActive(false);
        ShowObservationDialogue();
    }

    // 1. 손님의 고민 대사 출력
    private void ShowObservationDialogue()
    {
        dialogueStage = DialogueStage.Observing;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (dialogueManager != null) dialogueManager.ShowDialogue(currentCustomer);
    }

    // 3. 카드 제출 후 결과 대사 출력 (미구현: 지금은 대사창을 재사용해서 한 줄만 보여주고 터치하면 바로 다음으로)
    private void ShowCardResultDialogue()
    {
        dialogueStage = DialogueStage.ShowingCardResult;

        int matchCount = CalculateMatchCount();
        string reaction = currentCustomer != null ? currentCustomer.GetReaction(matchCount) : string.Empty;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (dialogueManager != null) dialogueManager.ShowDialogue(reaction);
    }

    // 2. 대사 넘김(화면 클릭/터치) 시: 관찰 대사였다면 카드 덱으로, 결과 대사였다면 다음 손님으로
    private void OnDialogueAdvanced()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        if (dialogueStage == DialogueStage.Observing)
        {
            if (cardDeckPanel != null) cardDeckPanel.SetActive(true);
            if (cardDeck != null) cardDeck.PrepareForNewRound();
        }
        else
        {
            AdvanceToNextCustomer();
        }
    }

    // 3장 선택 후 다음 버튼 클릭 시 카드 덱 패널 닫고 결과 대사(대사창 재사용) 출력
    private void OnCardDeckNextConfirmed()
    {
        if (cardDeckPanel != null) cardDeckPanel.SetActive(false);
        ShowCardResultDialogue();
    }

    // 4. 손님이 전부 끝나면 결과창 표시 (점수/평판 등은 아직 미구현)
    private void ShowFinalResult()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(true);
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
