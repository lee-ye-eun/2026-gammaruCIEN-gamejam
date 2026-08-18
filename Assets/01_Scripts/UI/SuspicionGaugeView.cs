using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SuspicionGaugeView : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private bool includeCurrentCustomerSuspicion = true;

    private void Update()
    {
        int value = 0;

        if (GameManager.Instance != null)
        {
            value = GameManager.Instance.SuspicionLevel;
            if (includeCurrentCustomerSuspicion)
            {
                value += GameManager.Instance.CurrentCustomerSuspicion;
            }
        }

        value = Mathf.Clamp(value, 0, 100);

        if (fillImage != null) fillImage.fillAmount = value / 100f;
        if (valueText != null) valueText.text = $"의심도 {value}/100";
    }
}
