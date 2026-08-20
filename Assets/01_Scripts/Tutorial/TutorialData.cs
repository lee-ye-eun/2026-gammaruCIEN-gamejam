using UnityEngine;

// 튜토리얼 한 스텝: 어떤 GameFlowManager 상태일 때, 어디를 강조(터치 가능)하고 무슨 텍스트를 보여줄지.
[System.Serializable]
public class TutorialStep
{
    public GameFlowManager.GameState state;
    [TextArea]
    public string text;

    [Header("강조(터치 가능) 영역 - rootRect 기준 anchoredPosition/크기")]
    public Vector2 highlightPosition;
    public Vector2 highlightSize;
}

[CreateAssetMenu(fileName = "NewTutorialData", menuName = "Tarot/Tutorial Data")]
public class TutorialData : ScriptableObject
{
    public TutorialStep[] steps;
}
