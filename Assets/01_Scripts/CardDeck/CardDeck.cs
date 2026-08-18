using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 카드 선택 상태(최대 3장)와 다음 버튼 활성화만 담당. 카드 수집/펼치기는 CardCollector가 담당.
public class CardDeck : MonoBehaviour
{
    private const int MaxSelectable = 3;

    [Header("카드 수집/펼치기 담당")]
    [SerializeField] private CardCollector cardCollector;

    [Header("3장 선택 완료 시 활성화할 버튼")]
    [SerializeField] private Button nextButton;

    private readonly List<CardView> selectedCards = new List<CardView>();

    public IReadOnlyList<CardView> SelectedCards => selectedCards;

    // 3장이 선택된 상태에서 다음 버튼을 클릭하면 발행됨. 패널 전환/결과 표시는 구독자(GameManager)가 담당.
    public event Action OnNextConfirmed;

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

        if (nextButton != null)
        {
            nextButton.interactable = false;
            nextButton.onClick.AddListener(HandleNextButtonClicked);
        }
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

        if (nextButton != null) nextButton.onClick.RemoveListener(HandleNextButtonClicked);
    }

    private void HandleNextButtonClicked()
    {
        OnNextConfirmed?.Invoke();
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
