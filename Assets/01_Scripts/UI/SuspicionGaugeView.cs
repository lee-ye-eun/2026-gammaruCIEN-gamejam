using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SuspicionGaugeView : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text valueText;

    // SuspicionLevel은 질문/카드 결과가 생길 때마다 즉시 반영되는 실시간 값이라 그대로 쓰면 된다
    // (CurrentCustomerSuspicion을 더하면 이번 라운드분이 중복으로 잡힌다).
    private void Update()
    {
        int value = GameManager.Instance != null ? GameManager.Instance.SuspicionLevel : 0;
        value = Mathf.Clamp(value, 0, 100);

        if (fillImage != null) fillImage.fillAmount = value / 100f;
        if (valueText != null) valueText.text = $"의심도 {value}/100";
    }
}
