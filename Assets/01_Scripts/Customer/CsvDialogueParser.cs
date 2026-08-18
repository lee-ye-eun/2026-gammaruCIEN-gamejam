using System.Collections.Generic;
using UnityEngine;

// "key,value" 행 형식 CSV(손님 1명당 1개)를 CustomerDialogueData로 파싱한다.
// 값은 큰따옴표로 감싼 콤마/이스케이프(""->")를 지원하지만, 값 내부 개행은 지원하지 않는다.
public static class CsvDialogueParser
{
    public static CustomerDialogueData Parse(TextAsset csv)
    {
        var data = new CustomerDialogueData();
        if (csv == null) return data;

        Dictionary<string, string> kv = ParseKeyValueRows(csv.text);

        if (kv.TryGetValue("worryText", out string worry)) data.worryText = worry;

        for (int i = 1; i <= 4; i++)
        {
            if (kv.TryGetValue($"clue{i}", out string clue) && !string.IsNullOrEmpty(clue))
            {
                data.clues.Add(clue);
            }
        }

        for (int i = 1; i <= 4; i++)
        {
            if (!kv.TryGetValue($"question{i}", out string question) || string.IsNullOrEmpty(question)) continue;

            kv.TryGetValue($"question{i}_clue", out string clue);
            data.questions.Add(new CustomerQuestion { questionText = question, clueText = clue ?? string.Empty });
        }

        for (int i = 0; i <= 3; i++)
        {
            if (kv.TryGetValue($"reaction{i}", out string reaction)) data.reactions[i] = reaction;
        }

        return data;
    }

    private static Dictionary<string, string> ParseKeyValueRows(string csvText)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(csvText)) return result;

        string[] lines = csvText.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (line.Length == 0) continue;
            if (line.Equals("key,value", System.StringComparison.OrdinalIgnoreCase)) continue; // 헤더 행 스킵

            int commaIndex = line.IndexOf(',');
            if (commaIndex < 0) continue;

            string key = line.Substring(0, commaIndex).Trim();
            if (key.Length == 0) continue;

            result[key] = UnquoteCsvField(line.Substring(commaIndex + 1));
        }

        return result;
    }

    private static string UnquoteCsvField(string field)
    {
        field = field.Trim();
        if (field.Length >= 2 && field[0] == '"' && field[field.Length - 1] == '"')
        {
            field = field.Substring(1, field.Length - 2).Replace("\"\"", "\"");
        }
        return field;
    }
}
