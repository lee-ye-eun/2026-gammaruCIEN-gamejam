using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Custom/UI_Outline 셰이더를 쓰는 Image에 붙여서 호버 시 테두리를 부드럽게 켜고 끈다.
// Image의 머티리얼을 인스턴스화해서 쓰기 때문에 같은 셰이더를 쓰는 다른 이미지에는 영향 없음.
[RequireComponent(typeof(Image))]
public class HoverOutline : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Color outlineColor = new Color(0.2f, 0.9f, 1f, 1f); // 네온 헤일로 색 (기본: 시안)
    [SerializeField] private float outlineWidth = 10f; // 호버 시 글로우 반경(px)
    [SerializeField] private float fadeDuration = 0.12f;

    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

    private Image image;
    private Material materialInstance;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        image = GetComponent<Image>();
        materialInstance = new Material(image.material);
        image.material = materialInstance;

        materialInstance.SetColor(OutlineColorId, outlineColor);
        materialInstance.SetFloat(OutlineWidthId, 0f);
    }

    private void OnDestroy()
    {
        if (materialInstance != null) Destroy(materialInstance);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        FadeTo(outlineWidth);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        FadeTo(0f);
    }

    private void FadeTo(float targetWidth)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(targetWidth));
    }

    private IEnumerator FadeRoutine(float targetWidth)
    {
        float startWidth = materialInstance.GetFloat(OutlineWidthId);
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            materialInstance.SetFloat(OutlineWidthId, Mathf.Lerp(startWidth, targetWidth, t));
            yield return null;
        }

        materialInstance.SetFloat(OutlineWidthId, targetWidth);
    }
}
