using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class CustomerView : MonoBehaviour
{
    [Header("데이터")]
    [SerializeField] private CustomerData data;

    [Header("UI 참조")]
    [SerializeField] private Image portraitImage;

    [Header("임시 표시")]
    [SerializeField] private GameObject fallbackRoot;

    public CustomerData Data => data;

    private void OnEnable()
    {
        if (data != null) Refresh();
    }

    public void SetData(CustomerData newData)
    {
        data = newData;
        Refresh();
    }

    private void Refresh()
    {
        Sprite portrait = data != null ? data.portrait : null;

        if (portraitImage != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
            if (portrait != null) portraitImage.color = Color.white;
        }

        if (fallbackRoot != null) fallbackRoot.SetActive(portrait == null);
    }
}
