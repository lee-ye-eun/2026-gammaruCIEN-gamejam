using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CustomerView : MonoBehaviour
{
    [Header("데이터")]
    [SerializeField] private CustomerData data;

    [Header("UI 참조")]
    [SerializeField] private Image portraitImage;

    [Header("단서 이미지 3개 (CustomerData.clueVisuals와 순서로 매칭, ClueFinder의 버튼과 동일한 오브젝트)")]
    [SerializeField] private Image[] clueImages;

    public CustomerData Data => data;

    private void OnEnable()
    {
        if (data != null) Refresh();
    }

    // 손님 데이터가 바뀔 때마다(매 턴 1회) 호출: 손님 스프라이트 + 단서 이미지 스프라이트/좌표를 한 번에 갱신
    public void SetData(CustomerData newData)
    {
        data = newData;
        Refresh();
    }

    private void Refresh()
    {
        SetPortraitSprite(data.portrait);
        RefreshClueVisuals();
    }

    // 카드 결과 연출 중 표정을 임시로 바꾼다 (다음 손님으로 넘어가 SetData가 다시 호출되면 기본 portrait로 되돌아감).
    public void SetExpression(Sprite expressionSprite)
    {
        if (expressionSprite == null) return;
        SetPortraitSprite(expressionSprite);
    }

    private void SetPortraitSprite(Sprite sprite)
    {
        if (portraitImage == null) return;

        portraitImage.sprite = sprite;
        portraitImage.SetNativeSize(); // 스프라이트의 원본 픽셀 크기에 맞춰 RectTransform 크기 자동 조정
    }

    private void RefreshClueVisuals()
    {
        var visuals = data != null ? data.clueVisuals : null;

        for (int i = 0; i < clueImages.Length; i++)
        {
            var image = clueImages[i];
            if (image == null) continue;

            bool hasVisual = visuals != null && i < visuals.Length && visuals[i] != null;
            image.gameObject.SetActive(hasVisual);
            if (!hasVisual) continue;

            image.sprite = visuals[i].sprite;
            image.SetNativeSize(); // 스프라이트의 원본 픽셀 크기에 맞춰 RectTransform 크기 자동 조정

            var rect = (RectTransform)image.transform;
            rect.anchoredPosition = visuals[i].position;
        }
    }
}
