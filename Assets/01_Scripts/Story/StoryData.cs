using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StoryImageEntry
{
    public string key;
    public Sprite sprite;
    public Texture2D texture;
}

// 컷(행)마다 대사/이미지 키/BGM 키를 담은 CSV 하나로 스토리를 정의한다.
// 이미지는 CSV에 직접 넣을 수 없어서, CSV의 image 칸에는 키 이름만 적고 실제 스프라이트는
// imageEntries에서 키로 찾는다 (SoundManager의 SoundEntry와 동일한 방식).
// BGM도 마찬가지로 CSV의 bgm 칸에 키 이름을 적으면 SoundManager에 등록해둔 클립을 재생한다.
[CreateAssetMenu(fileName = "NewStoryData", menuName = "Tarot/Story Data")]
public class StoryData : ScriptableObject
{
    [Tooltip("컷 순서대로 \"speaker,image,bgm,text\" 행을 담은 CSV. 바뀔 때만 image/bgm 칸을 채우고, 안 바뀌면 비워둔다")]
    [SerializeField] private TextAsset dialogueCsv;

    [Tooltip("CSV의 image 칸에 적을 키 이름과 실제 스프라이트를 매칭")]
    [SerializeField] private List<StoryImageEntry> imageEntries = new List<StoryImageEntry>();

    [Tooltip("이 스토리를 끝까지 보여준 뒤 이동할 씬 이름. Build Settings에 등록되어 있어야 함")]
    public string nextSceneName;

    private List<StoryLine> cachedLines;
    private Dictionary<string, Texture2D> imageLookup;

    // CSV를 최초 접근 시 한 번만 파싱해서 캐시
    private List<StoryLine> Lines
    {
        get
        {
            if (cachedLines == null) cachedLines = StoryCsvParser.Parse(dialogueCsv);
            return cachedLines;
        }
    }

    private Dictionary<string, Texture2D> ImageLookup
    {
        get
        {
            if (imageLookup == null)
            {
                imageLookup = new Dictionary<string, Texture2D>();
                foreach (var entry in imageEntries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.key)) continue;

                    Texture2D texture = entry.texture;
                    if (texture == null && entry.sprite != null) texture = entry.sprite.texture;
                    if (texture == null) continue;

                    imageLookup[entry.key] = texture;
                }
            }
            return imageLookup;
        }
    }

    public int SlideCount => Lines != null ? Lines.Count : 0;

    public StoryLine GetLine(int index)
    {
        var lines = Lines;
        if (lines == null || index < 0 || index >= lines.Count) return null;
        return lines[index];
    }

    public Texture2D GetImage(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        return ImageLookup.TryGetValue(key, out Texture2D texture) ? texture : null;
    }
}
