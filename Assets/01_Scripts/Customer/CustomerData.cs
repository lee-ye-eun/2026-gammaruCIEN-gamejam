using UnityEngine;

[System.Serializable]
public class CustomerQuestion
{
    [TextArea] public string questionText; // 예: "최근에 싸웠나요?"
    [TextArea] public string clueText;     // 이 질문을 골랐을 때 얻는 단서
}

[CreateAssetMenu(fileName = "NewCustomerData", menuName = "Tarot/Customer Data")]
public class CustomerData : ScriptableObject
{
    [Header("기본 정보")]
    public string customerName;
    public Sprite portrait;
    [TextArea] public string worryText;      // 고민 대사

    [Header("기본 단서 (항상 공개, 3~4개)")]
    [TextArea] public string[] baseClues;

    [Header("질문 (4개, 그중 2개만 선택 가능 - 선택 제한은 게임플레이 로직에서 처리)")]
    public CustomerQuestion[] questions;

    [Header("정답 카드 (원인 / 현재 / 조언)")]
    public CardData causeCard;
    public CardData presentCard;
    public CardData adviceCard;

    [Header("결과 반응 (정답 일치 개수별 대사)")]
    [TextArea] public string reaction0; // 0장 일치
    [TextArea] public string reaction1; // 1장 일치
    [TextArea] public string reaction2; // 2장 일치
    [TextArea] public string reaction3; // 3장 일치

    public string GetReaction(int matchCount)
    {
        switch (matchCount)
        {
            case 3: return reaction3;
            case 2: return reaction2;
            case 1: return reaction1;
            default: return reaction0;
        }
    }
}
