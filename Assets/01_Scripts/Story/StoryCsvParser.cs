using System;
using System.Collections.Generic;
using UnityEngine;

// 한 컷의 정보. speaker/text 둘 다 비어있으면 그 컷은 대사 없음(대사창 비활성화)으로 취급된다.
// imageKey/bgmKey/effectKey는 비어있으면 "이전 컷 그대로 유지"를 뜻한다 (값이 있는 행에서만 바뀜).
// imageKey는 실제 스프라이트 키만, effectKey는 fadeout/fadein 같은 연출 키워드만 담아 서로 섞이지 않는다.
public class StoryLine
{
    public string speaker = string.Empty;
    public string imageKey = string.Empty;
    public string bgmKey = string.Empty;
    public string effectKey = string.Empty;
    public string text = string.Empty;
}

// "speaker,image,bgm,effect,text" 5컬럼 CSV를 순서 그대로 파싱한다. 행의 순서(인덱스)가 곧 컷 번호라서
// CustomerData용 CsvDialogueParser(key,value 딕셔너리)와 달리 순서를 보존해야 한다.
// text는 마지막 컬럼이라 콤마가 들어있어도 이스케이프 없이 그대로 써도 된다 (앞 4개 콤마까지만 구분자로 씀).
// 대사 없는 컷은 빈 행(",,,,")으로 남겨두면 되고, 파일 맨 끝의 개행으로 생기는 마지막 빈 줄만 무시한다.
public static class StoryCsvParser
{
    private const int ColumnCount = 5;

    public static List<StoryLine> Parse(TextAsset csv)
    {
        var lines = new List<StoryLine>();
        if (csv == null || string.IsNullOrEmpty(csv.text)) return lines;

        string[] rawLines = csv.text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

        for (int i = 0; i < rawLines.Length; i++)
        {
            string line = rawLines[i];

            // 파일 끝의 개행으로 생기는 마지막 빈 줄만 무시
            if (i == rawLines.Length - 1 && line.Trim().Length == 0) continue;

            // 헤더 행은 맨 첫 줄일 때만 스킵 (중간의 빈/공란 행은 "대사 없는 컷"이라 건드리면 안 됨)
            if (i == 0 && line.Trim().Equals("speaker,image,bgm,effect,text", StringComparison.OrdinalIgnoreCase)) continue;

            lines.Add(ParseLine(line));
        }

        return lines;
    }

    private static StoryLine ParseLine(string line)
    {
        string[] fields = SplitColumns(line, ColumnCount);

        return new StoryLine
        {
            speaker = UnquoteCsvField(fields[0]),
            imageKey = UnquoteCsvField(fields[1]),
            bgmKey = UnquoteCsvField(fields[2]),
            effectKey = UnquoteCsvField(fields[3]),
            text = UnquoteCsvField(fields[4])
        };
    }

    // 앞쪽 (columnCount - 1)개의 콤마까지만 구분자로 쓰고, 마지막 컬럼은 나머지 전부를 그대로 가져온다.
    private static string[] SplitColumns(string line, int columnCount)
    {
        var result = new string[columnCount];
        int start = 0;

        for (int i = 0; i < columnCount - 1; i++)
        {
            int comma = line.IndexOf(',', start);
            if (comma < 0)
            {
                result[i] = line.Substring(start);
                for (int j = i + 1; j < columnCount; j++) result[j] = string.Empty;
                return result;
            }

            result[i] = line.Substring(start, comma - start);
            start = comma + 1;
        }

        result[columnCount - 1] = line.Substring(start);
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
