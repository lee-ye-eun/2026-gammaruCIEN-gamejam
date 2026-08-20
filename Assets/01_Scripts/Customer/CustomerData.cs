using UnityEngine;

[System.Serializable]
public class ClueVisual
{
    public Sprite sprite;
    public Vector2 position; // 단서 이미지의 anchoredPosition
    public Sprite zoomSprite; // 단서를 터치했을 때 뜨는 확대(클로즈업) 이미지. 비워두면 sprite를 그대로 확대해서 보여준다.
}

[CreateAssetMenu(fileName = "NewCustomerData", menuName = "Tarot/Customer Data")]
public class CustomerData : ScriptableObject
{
    [Header("기본 정보")]
    public string customerId;
    public string customerName;
    public string ageGroup;
    public string gender;
    [TextArea]
    public string appearanceDescription;
    [TextArea]
    public string situationDescription;
    public Sprite portrait;

    [Header("표정 변화 (카드 결과 연출 중에만 사용, 평소엔 portrait)")]
    [Tooltip("주인공의 해석 대사 3개가 끝나고 손님 대사로 \"...\"이 출력되는 순간 표시")]
    public Sprite expression1;
    [Tooltip("맞춘 카드가 3개 전부일 때(정답) 표시")]
    public Sprite expression2;
    [Tooltip("맞춘 카드가 3개 미만일 때(오답 1개 이상) 표시")]
    public Sprite expression3;

    [Header("손님별 배경 아트 (Assets/03_Sprites/BackGround). 씬의 BackGround 크기/위치는 그대로 두고 텍스처만 교체된다")]
    public Texture background;

    [Header("대사 CSV (key,value 행: worryText / clue1~4 / question1~4,question1~4_clue / reaction0~3)")]
    [SerializeField] private TextAsset dialogueCsv;

    [Header("정답 카드 (원인 / 현재 / 조언)")]
    public CardData causeCard;
    public CardData presentCard;
    public CardData adviceCard;

    [Header("단서 이미지 3개 (스프라이트 + 좌표 + 확대 이미지). CSV의 clue1~3과 순서로 매칭됨")]
    public ClueVisual[] clueVisuals;

    private CustomerDialogueData cachedDialogue;

    // CSV를 최초 접근 시 한 번만 파싱해서 캐시
    public CustomerDialogueData Dialogue
    {
        get
        {
            if (cachedDialogue == null) cachedDialogue = CsvDialogueParser.Parse(dialogueCsv);
            return cachedDialogue;
        }
    }

    public string GetReaction(int matchCount)
    {
        int index = Mathf.Clamp(matchCount, 0, 3);
        return Dialogue.reactions[index] ?? string.Empty;
    }

    // 카드 결과 표정: 3개 전부 맞았으면 expression2, 그 미만이면(1~2개 또는 전부 오답) 무조건 expression3.
    public Sprite GetResultExpression(int matchCount)
    {
        return matchCount >= 3 ? expression2 : expression3;
    }
}
