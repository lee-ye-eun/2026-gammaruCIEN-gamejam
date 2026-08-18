using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardDeck : MonoBehaviour
{
    private const int MaxSelectable = 3;

    [Header("카드 22장 (씬에 배치된 CardView 프리팹 인스턴스)")]
    [SerializeField] private List<CardView> cards = new List<CardView>();

    [Header("3장 선택 완료 시 활성화할 버튼")]
    [SerializeField] private Button nextButton;

    [Header("정렬 애니메이션")]
    [SerializeField] private RectTransform cardContainer; // 카드들의 Layout Group이 걸린 부모 (비워두면 자기 자신 사용)
    [SerializeField] private float shuffleMoveDuration = 0.4f;
    [SerializeField] private AnimationCurve shuffleEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private readonly List<CardView> selectedCards = new List<CardView>();

    public IReadOnlyList<CardView> SelectedCards => selectedCards;

    private void Awake()
    {
        CollectCards();
        ShuffleCards();

        foreach (var card in cards)
        {
            if (card != null) card.OnClicked += HandleCardClicked;
        }

        if (nextButton != null) nextButton.interactable = false;
    }

    // 인스펙터 리스트의 빈 슬롯(null)을 정리하고, 아예 채워두지 않았다면 컨테이너 하위의 CardView를 자동으로 모은다.
    // 22장을 다 준비하지 못한 프로토타입 단계에서도 있는 카드만으로 동작하도록 하기 위함.
    private void CollectCards()
    {
        cards.RemoveAll(card => card == null);

        if (cards.Count == 0)
        {
            RectTransform container = cardContainer != null ? cardContainer : transform as RectTransform;
            if (container != null)
            {
                cards.AddRange(container.GetComponentsInChildren<CardView>(true));
            }
        }
    }

    private void ShuffleCards()
    {
        if (cards.Count == 0) return;

        // 정렬 전 각 카드의 현재 화면 위치를 기억해둔다
        var oldPositions = new Dictionary<CardView, Vector2>();
        foreach (var card in cards)
        {
            if (card != null) oldPositions[card] = ((RectTransform)card.transform).anchoredPosition;
        }

        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null) cards[i].transform.SetSiblingIndex(i);
        }

        // Layout Group을 즉시 재계산해서 바뀐 순서의 목표 위치를 얻는다
        RectTransform container = cardContainer != null ? cardContainer : transform as RectTransform;
        if (container != null) LayoutRebuilder.ForceRebuildLayoutImmediate(container);

        // 각 카드를 원래 위치로 되돌린 뒤, 새 위치까지 애니메이션으로 이동시킨다
        foreach (var card in cards)
        {
            if (card == null) continue;

            var rect = (RectTransform)card.transform;
            Vector2 targetPos = rect.anchoredPosition;

            if (oldPositions.TryGetValue(card, out Vector2 startPos))
            {
                rect.anchoredPosition = startPos;
                StartCoroutine(AnimateCardPosition(rect, startPos, targetPos));
            }
        }
    }

    private IEnumerator AnimateCardPosition(RectTransform rect, Vector2 from, Vector2 to)
    {
        float elapsed = 0f;
        while (elapsed < shuffleMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = shuffleEase.Evaluate(Mathf.Clamp01(elapsed / shuffleMoveDuration));
            rect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            yield return null;
        }

        rect.anchoredPosition = to;
    }

    private void OnDestroy()
    {
        foreach (var card in cards)
        {
            if (card != null) card.OnClicked -= HandleCardClicked;
        }
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
