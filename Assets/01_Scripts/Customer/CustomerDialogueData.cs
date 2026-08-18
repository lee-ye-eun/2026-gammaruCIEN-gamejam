using System.Collections.Generic;

[System.Serializable]
public class CustomerQuestion
{
    public string questionText; // 예: "최근에 싸웠나요?"
    public string clueText;     // 이 질문을 골랐을 때 얻는 단서
}

// CustomerData의 dialogueCsv를 파싱한 결과. CSV가 유일한 텍스트 소스.
public class CustomerDialogueData
{
    public string worryText = string.Empty;
    public List<string> clues = new List<string>();
    public List<CustomerQuestion> questions = new List<CustomerQuestion>();
    public string[] reactions = new string[4]; // reactions[0~3] = 정답 일치 개수별 반응
}
