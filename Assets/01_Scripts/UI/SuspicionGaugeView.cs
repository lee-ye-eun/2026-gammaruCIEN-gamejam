using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SuspicionGaugeView : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text labelText;

    private void Update()
    {
        int suspicion = GameManager.Instance != null ? GameManager.Instance.SuspicionLevel : 0;
        float normalized = Mathf.Clamp01(suspicion / 100f);

        if (fillImage != null) fillImage.fillAmount = normalized;
        if (labelText != null) labelText.text = $"의심도 {suspicion}/100";
    }
}
