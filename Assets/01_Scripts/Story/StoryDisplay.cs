using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

// StoryScene에 배치. GameManager.Instance.LastStoryTrigger(왜 스토리로 넘어왔는지)를 보고
// 그에 대응하는 StoryData를 고른 뒤, 화면을 클릭할 때마다 컷(이미지+대사)을 순서대로 보여준다.
// 대사(발화자+텍스트)가 둘 다 빈 컷은 대사창 자체를 비활성화한다.
// 마지막 컷 다음에 또 클릭하면 그 StoryData에 지정된 다음 씬으로 이동한다.
public class StoryDisplay : MonoBehaviour
{
    [Header("출력 UI")]
    [SerializeField] private Image storyImage;
    [SerializeField] private GameObject dialogueBox; // 대사창 전체 (발화자란 + 텍스트란을 담은 오브젝트)
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("GameManager.LastStoryTrigger 별로 보여줄 스토리")]
    [SerializeField] private StoryData prologueStory;          // 프롤로그 -> 끝나면 GameScene으로
    [SerializeField] private StoryData customersExhaustedStory; // 손님을 다 응대함 -> 끝나면 Title로
    [SerializeField] private StoryData highSuspicionStory;      // 의심도가 임계치를 넘음 -> 끝나면 Title로

    private StoryData currentStory;
    private int slideIndex = -1;
    private bool isTouchActive;

    private void Start()
    {
        currentStory = ResolveStory();
        ShowNextSlide();
    }

    private void Update()
    {
        if (!isTouchActive) return;
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            ShowNextSlide();
        }
    }

    private StoryData ResolveStory()
    {
        if (GameManager.Instance == null) return customersExhaustedStory;

        switch (GameManager.Instance.LastStoryTrigger)
        {
            case GameManager.StoryTrigger.Prologue:
                return prologueStory;
            case GameManager.StoryTrigger.HighSuspicion:
                return highSuspicionStory;
            case GameManager.StoryTrigger.CustomersExhausted:
            default:
                return customersExhaustedStory;
        }
    }

    // 다음 컷을 보여준다. 더 이상 컷이 없으면 스토리를 끝내고 다음 씬으로 이동한다.
    private void ShowNextSlide()
    {
        isTouchActive = false;
        slideIndex++;

        if (currentStory == null || slideIndex >= currentStory.SlideCount)
        {
            FinishStory();
            return;
        }

        StoryLine line = currentStory.GetLine(slideIndex);

        // image/bgm 칸이 비어있으면 이전 컷 그대로 유지 (값이 있을 때만 교체)
        if (line != null && !string.IsNullOrEmpty(line.imageKey))
        {
            Sprite sprite = currentStory.GetImage(line.imageKey);
            if (storyImage != null && sprite != null) storyImage.sprite = sprite;
        }

        if (line != null && !string.IsNullOrEmpty(line.bgmKey) && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM(line.bgmKey);
        }

        bool hasDialogue = line != null && (!string.IsNullOrEmpty(line.speaker) || !string.IsNullOrEmpty(line.text));

        if (dialogueBox != null) dialogueBox.SetActive(hasDialogue);
        if (hasDialogue)
        {
            if (speakerText != null) speakerText.text = line.speaker;
            if (dialogueText != null) dialogueText.text = line.text;
        }

        isTouchActive = true;
    }

    private void FinishStory()
    {
        if (currentStory == null || string.IsNullOrEmpty(currentStory.nextSceneName)) return;
        if (GameManager.Instance != null) GameManager.Instance.LoadSceneWithLoading(currentStory.nextSceneName);
    }
}
