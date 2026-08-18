using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("손님")]
    [SerializeField] private CustomerView customerView;
    [SerializeField] private CustomerData currentCustomer;

    [Header("대화 (1. 손님 대사 표시)")]
    [SerializeField] private DialogueManager dialogueManager;

    [Header("카드 덱 (2. 대사 넘김 시 활성화 + 셔플)")]
    [SerializeField] private GameObject cardDeckPanel; // CardDeck 컴포넌트가 붙어있는 오브젝트
    [SerializeField] private CardDeck cardDeck;
    [SerializeField] private Button nextButton; // CardDeck의 nextButton과 동일한 참조

    [Header("결과 (3. 결과 대사 출력)")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;

    private void Awake()
    {
        if (nextButton != null) nextButton.onClick.AddListener(OnNextButtonClicked);
        if (dialogueManager != null) dialogueManager.OnDialogueAdvanced += OnDialogueAdvanced;
    }

    private void OnDestroy()
    {
        if (nextButton != null) nextButton.onClick.RemoveListener(OnNextButtonClicked);
        if (dialogueManager != null) dialogueManager.OnDialogueAdvanced -= OnDialogueAdvanced;
    }

    private void Start()
    {
        StartCustomer();
    }

    private void StartCustomer()
    {
        if (customerView != null && currentCustomer != null) customerView.SetData(currentCustomer);

        if (cardDeckPanel != null) cardDeckPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        // 1. 손님 데이터에 지정된 고민 대사를 대화 매니저에 출력
        if (dialogueManager != null) dialogueManager.ShowDialogue(currentCustomer);
    }

    // 2. 대사 넘김(화면 클릭/터치) 시 대화창 닫고 카드 덱 활성화(첫 활성화 시 CardDeck.Awake에서 셔플 시작)
    private void OnDialogueAdvanced()
    {
        if (dialogueManager != null) dialogueManager.HideDialogue();
        if (cardDeckPanel != null) cardDeckPanel.SetActive(true);
    }

    // 3. 3장 선택 후 다음 버튼 클릭 시 결과 대사 출력
    private void OnNextButtonClicked()
    {
        if (cardDeckPanel != null) cardDeckPanel.SetActive(false);
        ShowResult();
    }

    private void ShowResult()
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText == null || currentCustomer == null) return;

        int matchCount = CalculateMatchCount();
        resultText.text = currentCustomer.GetReaction(matchCount);
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
