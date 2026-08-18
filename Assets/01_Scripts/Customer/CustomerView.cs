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
        if (portraitImage != null) portraitImage.sprite = data.portrait;
    }
}
