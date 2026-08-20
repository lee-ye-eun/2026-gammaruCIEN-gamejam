using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

// StoryScene에 배치. GameManager.Instance.LastStoryTrigger(왜 스토리로 넘어왔는지)를 보고
// 그에 대응하는 StoryData를 고른 뒤, 화면을 클릭할 때마다 컷(이미지+대사)을 순서대로 보여준다.
// 대사(발화자+텍스트)가 둘 다 빈 컷은 대사창 자체를 비활성화한다.
// effect 칸에 fadeout/fadein을 적으면 화면 전체가 검게 덮이거나 걷히는 연출을, bgm 칸에 stop을 적으면 BGM 정지를 재생한다.
// image 칸은 오직 실제 스프라이트 키만 담는다 (연출 키워드와 섞이지 않음).
// 마지막 컷 다음에 또 클릭하면 그 StoryData에 지정된 다음 씬으로 이동한다.
public class StoryDisplay : MonoBehaviour
{
    private const string FadeOutKeyword = "fadeout"; // 화면이 검게 덮임
    private const string FadeInKeyword = "fadein";   // 검은 화면이 걷히며 드러남
    private const string StopBgmKeyword = "stop";    // BGM 정지

    [Header("출력 UI")]
    [SerializeField] private RawImage storyImage;
    [SerializeField] private GameObject dialogueBox; // 대사창 전체 (발화자란 + 텍스트란을 담은 오브젝트)
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("페이드 연출 (effect 칸에 fadeout/fadein 입력 시 재생)")]
    [SerializeField] private Image fadeOverlay; // 화면 전체를 덮는 검정 이미지 (알파를 애니메이션)
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("GameManager.LastStoryTrigger 별로 보여줄 스토리")]
    [SerializeField] private StoryData prologueStory;          // 프롤로그 -> 끝나면 GameScene으로
    [SerializeField] private StoryData customersExhaustedStory; // 손님을 다 응대함 -> 끝나면 Title로
    [SerializeField] private StoryData highSuspicionStory;      // 의심도가 임계치를 넘음 -> 끝나면 Title로

    private StoryData currentStory;
    private int slideIndex = -1;
    private bool isTouchActive;
    private Coroutine fadeRoutine;

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

        ApplyImage(line);
        ApplyBgm(line);
        ApplyEffect(line);

        bool hasDialogue = line != null && (!string.IsNullOrEmpty(line.speaker) || !string.IsNullOrEmpty(line.text));

        if (dialogueBox != null) dialogueBox.SetActive(hasDialogue);
        if (hasDialogue)
        {
            if (speakerText != null) speakerText.text = line.speaker;
            if (dialogueText != null) dialogueText.text = line.text;
        }

        // 페이드가 시작됐으면 그 코루틴이 끝난 뒤에 직접 isTouchActive를 풀어준다 (연출 중 클릭으로 건너뛰지 못하게)
        if (fadeRoutine == null) isTouchActive = true;
    }

    // image 칸이 비어있으면 이전 컷 그대로 유지. 값이 있으면 그 키에 해당하는 스프라이트로 교체.
    private void ApplyImage(StoryLine line)
    {
        if (line == null || string.IsNullOrEmpty(line.imageKey)) return;

        Texture2D texture = currentStory.GetImage(line.imageKey.Trim());
        if (storyImage != null && texture != null) storyImage.texture = texture;
    }

    // effect 칸이 비어있으면 아무 연출 없음. fadeout/fadein이면 화면 페이드 연출을 재생한다.
    private void ApplyEffect(StoryLine line)
    {
        if (line == null || string.IsNullOrEmpty(line.effectKey)) return;

        string key = line.effectKey.Trim();

        if (key.Equals(FadeOutKeyword, StringComparison.OrdinalIgnoreCase))
        {
            StartFade(1f);
        }
        else if (key.Equals(FadeInKeyword, StringComparison.OrdinalIgnoreCase))
        {
            StartFade(0f);
        }
    }

    // bgm 칸이 비어있으면 이전 곡 그대로 유지. stop이면 정지, 그 외엔 키로 재생.
    private void ApplyBgm(StoryLine line)
    {
        if (line == null || string.IsNullOrEmpty(line.bgmKey) || SoundManager.Instance == null) return;

        string key = line.bgmKey.Trim();

        if (key.Equals(StopBgmKeyword, StringComparison.OrdinalIgnoreCase))
        {
            SoundManager.Instance.StopBGM();
            return;
        }

        SoundManager.Instance.PlayBGM(key);
    }

    private void StartFade(float targetAlpha)
    {
        if (fadeOverlay == null) return;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        fadeOverlay.gameObject.SetActive(true);

        Color color = fadeOverlay.color;
        float startAlpha = color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / fadeDuration));
            fadeOverlay.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }

        fadeOverlay.color = new Color(color.r, color.g, color.b, targetAlpha);
        if (targetAlpha <= 0f) fadeOverlay.gameObject.SetActive(false);

        fadeRoutine = null;
        isTouchActive = true;
    }

    private void FinishStory()
    {
        if (currentStory == null || string.IsNullOrEmpty(currentStory.nextSceneName)) return;
        if (GameManager.Instance != null) GameManager.Instance.LoadSceneWithLoading(currentStory.nextSceneName);
    }

    // 스킵 버튼 onClick에 연결: 남은 컷을 건너뛰고 곧장 이 StoryData에 지정된 다음 씬으로 이동한다.
    public void SkipStory()
    {
        isTouchActive = false;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        FinishStory();
    }
}
