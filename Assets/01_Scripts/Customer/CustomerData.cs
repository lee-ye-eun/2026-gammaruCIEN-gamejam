using UnityEngine;

[System.Serializable]
public class ClueVisual
{
    public Sprite sprite;
    public Vector2 position; // 단서 이미지의 anchoredPosition
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

    [Header("대사 CSV (key,value 행: worryText / clue1~4 / question1~4,question1~4_clue / reaction0~3)")]
    [SerializeField] private TextAsset dialogueCsv;

    [Header("정답 카드 (원인 / 현재 / 조언)")]
    public CardData causeCard;
    public CardData presentCard;
    public CardData adviceCard;

    [Header("단서 이미지 3개 (스프라이트 + 좌표). CSV의 clue1~3과 순서로 매칭됨")]
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
}
