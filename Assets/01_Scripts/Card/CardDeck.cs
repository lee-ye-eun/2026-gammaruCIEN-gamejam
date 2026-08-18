using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 카드 덱 화면의 매니저 역할을 겸함: 카드 선택 상태(최대 3장)와 다음 버튼 활성화 관리.
// 카드 수집/펼치기는 CardCollector가 담당. 다음 버튼은 인스펙터에서 이 컴포넌트의 ConfirmSelection()을 참조하도록 연결한다.
public class CardDeck : MonoBehaviour
{
    private const int MaxSelectable = 3;

    [Header("카드 수집/펼치기 담당")]
    [SerializeField] private CardCollector cardCollector;

    [Header("3장 선택 완료 시 활성화할 버튼")]
    [SerializeField] private Button nextButton;

    private readonly List<CardView> selectedCards = new List<CardView>();

    public IReadOnlyList<CardView> SelectedCards => selectedCards;

    private void Awake()
    {
        if (cardCollector != null)
        {
            cardCollector.CollectCards();

            foreach (var card in cardCollector.Cards)
            {
                if (card != null) card.OnClicked += HandleCardClicked;
            }
        }

        if (nextButton != null) nextButton.interactable = false;
    }

    // 손님이 바뀔 때마다(카드 덱 패널이 다시 열릴 때마다) GameManager가 호출: 이전 선택 초기화 + 카드 다시 펼치기
    public void PrepareForNewRound()
    {
        ResetSelection();
        if (cardCollector != null) cardCollector.SpreadCards();
    }

    private void OnDestroy()
    {
        if (cardCollector != null)
        {
            foreach (var card in cardCollector.Cards)
            {
                if (card != null) card.OnClicked -= HandleCardClicked;
            }
        }
    }

    // 3장 선택 완료 시 활성화되는 다음 버튼 onClick에 연결 -> 결과 대사로 전환
    public void ConfirmSelection()
    {
        if (selectedCards.Count != MaxSelectable) return;
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.ShowingCardResult);
    }

    private void HandleCardClicked(CardView card)
    {
        if (selectedCards.Contains(card))
        {
            selectedCards.Remove(card);
            card.SetSelected(false);
        }
        else
        {
            if (selectedCards.Count >= MaxSelectable) return; // 3장 초과 선택 억제

            selectedCards.Add(card);
            card.SetSelected(true);
        }

        if (nextButton != null) nextButton.interactable = selectedCards.Count == MaxSelectable;
    }

    public void ResetSelection()
    {
        foreach (var card in selectedCards)
        {
            card.SetSelected(false);
        }
        selectedCards.Clear();

        if (nextButton != null) nextButton.interactable = false;
    }
}
