using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SuspicionGaugeView : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text valueText;

    // 게이지를 숨길 때 gameObject.SetActive(false)를 쓰면 Update()가 멈춰 스스로 다시 켜지지 못한다.
    // 그래서 CanvasGroup의 알파로 표시/숨김을 처리한다. 씬에 CanvasGroup이 없으면 런타임에 붙인다.
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    // SuspicionLevel은 질문/카드 결과가 생길 때마다 즉시 반영되는 실시간 값이라 그대로 쓰면 된다
    // (CurrentCustomerSuspicion을 더하면 이번 라운드분이 중복으로 잡힌다).
    private void Update()
    {
        int value = GameManager.Instance != null ? GameManager.Instance.SuspicionLevel : 0;
        value = Mathf.Clamp(value, 0, 100);

        if (fillImage != null) fillImage.fillAmount = value / 100f;
        if (valueText != null) valueText.text = $"의심도 {value}/100";

        ApplyVisibility();
    }

    // 플레이어에게 카드 뒷면이 보이는 동안에만 게이지를 노출한다.
    // 손님 등장 / 단서 찾기 / 투시경 ON / 카드 결과 / 결과창에서는 숨긴다.
    private void ApplyVisibility()
    {
        if (canvasGroup == null) return;

        bool visible = GameFlowManager.Instance != null && GameFlowManager.Instance.AreCardBacksVisible;
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }
}
