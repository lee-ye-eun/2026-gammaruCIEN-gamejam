using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Selectable))]
public class UIButtonPunch : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float hoverScale = 1.04f;
    [SerializeField] private float pressedScale = 0.94f;
    [SerializeField] private float animationSpeed = 18f;

    private RectTransform rectTransform;
    private Selectable selectable;
    private Coroutine scaleRoutine;
    private Vector3 baseScale;
    private bool pointerInside;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        selectable = GetComponent<Selectable>();
        baseScale = rectTransform.localScale;
    }

    private void OnEnable()
    {
        if (rectTransform == null) rectTransform = (RectTransform)transform;
        if (baseScale == Vector3.zero) baseScale = rectTransform.localScale;
        rectTransform.localScale = baseScale;
    }

    private void OnDisable()
    {
        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = null;

        if (rectTransform != null) rectTransform.localScale = baseScale == Vector3.zero ? Vector3.one : baseScale;
        pointerInside = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        if (CanAnimate()) AnimateTo(baseScale * hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        AnimateTo(baseScale);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (CanAnimate()) AnimateTo(baseScale * pressedScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        AnimateTo(pointerInside && CanAnimate() ? baseScale * hoverScale : baseScale);
    }

    private bool CanAnimate()
    {
        return selectable == null || selectable.IsInteractable();
    }

    private void AnimateTo(Vector3 targetScale)
    {
        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(ScaleRoutine(targetScale));
    }

    private IEnumerator ScaleRoutine(Vector3 targetScale)
    {
        Vector3 startScale = rectTransform.localScale;
        float elapsed = 0f;
        float duration = Mathf.Approximately(animationSpeed, 0f) ? 0f : 1f / animationSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        rectTransform.localScale = targetScale;
        scaleRoutine = null;
    }
}
