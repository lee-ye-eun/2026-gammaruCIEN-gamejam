using UnityEngine;

[CreateAssetMenu(fileName = "NewCardData", menuName = "Tarot/Card Data")]
public class CardData : ScriptableObject
{
    [Header("기본 정보")]
    public string cardId;
    public string cardName;      // 예: "달"
    public Sprite symbol;        // 카드 문양 아트

    [Header("키워드")]
    public string[] keywords;    // 예: "비밀", "불안", "착각"

    [Header("설명 (선택)")]
    [TextArea]
    public string description;

    [Header("위치별 해석")]
    [TextArea]
    public string causeMeaning;
    [TextArea]
    public string presentMeaning;
    [TextArea]
    public string adviceMeaning;
}
