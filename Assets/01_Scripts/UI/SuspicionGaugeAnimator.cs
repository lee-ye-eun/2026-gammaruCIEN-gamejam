using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class SuspicionGaugeAnimator : MonoBehaviour
{
    private const float MaxDisplayedSuspicion = 100f;

    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text valueText;
    [SerializeField, Min(0f)] private float suspicionUnitsPerSecond = 40f;
    [SerializeField, Min(0f)] private float maxWidth;

    private float displayedSuspicion;
    private bool hasInitializedDisplay;
    private RectTransform fillRectTransform;

    private void Awake()
    {
        if (fillImage == null)
        {
            return;
        }

        fillRectTransform = fillImage.rectTransform;

        if (maxWidth <= 0f)
        {
            maxWidth = fillRectTransform.rect.width;
        }

        fillImage.fillAmount = 1f;

        if (GameManager.Instance != null)
        {
            displayedSuspicion = GetTargetSuspicion();
            hasInitializedDisplay = true;
            ApplyDisplayedWidth();
        }
    }

    private void Update()
    {
        if (fillImage == null || fillRectTransform == null || GameManager.Instance == null)
        {
            return;
        }

        float targetSuspicion = GetTargetSuspicion();

        if (!hasInitializedDisplay)
        {
            displayedSuspicion = targetSuspicion;
            hasInitializedDisplay = true;
        }
        else
        {
            displayedSuspicion = Mathf.MoveTowards(
                displayedSuspicion,
                targetSuspicion,
                Mathf.Max(0f, suspicionUnitsPerSecond) * Time.deltaTime);
        }

        ApplyDisplayedWidth();
    }

    private float GetTargetSuspicion()
    {
        return Mathf.Clamp(
            GameManager.Instance.SuspicionLevel,
            0f,
            MaxDisplayedSuspicion);
    }

    private void ApplyDisplayedWidth()
    {
        fillImage.fillAmount = 1f;

        float ratio = displayedSuspicion / MaxDisplayedSuspicion;
        float displayedWidth = maxWidth * ratio;
        fillRectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            displayedWidth);

        if (valueText != null)
        {
            valueText.text = $"의심도 {Mathf.RoundToInt(displayedSuspicion)}/100";
        }
    }
}
