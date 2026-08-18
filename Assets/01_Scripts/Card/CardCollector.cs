using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 카드 수집 + 펼치기 애니메이션만 담당. 선택 로직은 CardDeck이 담당.
public class CardCollector : MonoBehaviour
{
    [Header("카드 (씬에 배치된 CardView 프리팹 인스턴스)")]
    [SerializeField] private List<CardView> cards = new List<CardView>();

    [Header("펼치기 애니메이션")]
    [SerializeField] private RectTransform cardContainer; // 카드들의 Layout Group이 걸린 부모 (반드시 연결 필요)
    [SerializeField] private RectTransform stackPoint; // 카드가 뭉쳐서 시작할 위치 (비워두면 컨테이너 원점)
    [SerializeField] private float spreadDuration = 0.4f;
    [SerializeField] private AnimationCurve spreadEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public IReadOnlyList<CardView> Cards => cards;

    // 인스펙터 리스트의 빈 슬롯(null)을 정리하고, 아예 채워두지 않았다면 컨테이너 하위의 CardView를 자동으로 모은다.
    // 22장을 다 준비하지 못한 프로토타입 단계에서도 있는 카드만으로 동작하도록 하기 위함.
    public void CollectAndSpread()
    {
        CollectCards();
        SpreadCards();
    }

    public void CollectCards()
    {
        cards.RemoveAll(card => card == null);

        if (cards.Count == 0 && cardContainer != null)
        {
            cards.AddRange(cardContainer.GetComponentsInChildren<CardView>(true));
        }
    }

    // 손님이 바뀔 때마다 다시 호출해서 카드를 재배치(펼치기)한다
    public void SpreadCards()
    {
        if (cards.Count == 0) return;

        // Layout Group을 즉시 재계산해서 각 카드의 목표(펼쳐진) 위치를 얻는다
        if (cardContainer != null) LayoutRebuilder.ForceRebuildLayoutImmediate(cardContainer);

        Vector2 pilePosition = stackPoint != null ? stackPoint.anchoredPosition : Vector2.zero;

        // 모든 카드를 한 지점에 뭉친 뒤, 각자의 목표 위치까지 애니메이션으로 펼친다
        foreach (var card in cards)
        {
            if (card == null) continue;

            var rect = (RectTransform)card.transform;
            Vector2 targetPos = rect.anchoredPosition;

            rect.anchoredPosition = pilePosition;
            StartCoroutine(AnimateCardPosition(rect, pilePosition, targetPos));
        }
    }

    private IEnumerator AnimateCardPosition(RectTransform rect, Vector2 from, Vector2 to)
    {
        float elapsed = 0f;
        while (elapsed < spreadDuration)
        {
            elapsed += Time.deltaTime;
            float t = spreadEase.Evaluate(Mathf.Clamp01(elapsed / spreadDuration));
            rect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            yield return null;
        }

        rect.anchoredPosition = to;
    }
}
