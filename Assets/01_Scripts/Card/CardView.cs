using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class CardView : MonoBehaviour
{
    [Header("데이터")]
    [SerializeField] private CardData data;

    [Header("UI 참조")]
    [SerializeField] private Image symbolImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text keywordText;

    [Header("선택 상태")]
    [SerializeField] private Button button;
    [SerializeField] private GameObject selectedIndicator; // 선택 시 켜지는 테두리/체크 표시 (선택)

    public CardData Data => data;
    public bool IsSelected { get; private set; }

    public event Action<CardView> OnClicked;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(HandleButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClicked);
        }
    }

    private void OnEnable()
    {
        if (data != null) Refresh();
    }

    public void SetData(CardData newData)
    {
        data = newData;
        Refresh();
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        if (selectedIndicator != null) selectedIndicator.SetActive(selected);
    }

    private void HandleButtonClicked()
    {
        OnClicked?.Invoke(this);
    }

    private void Refresh()
    {
        if (symbolImage != null) symbolImage.sprite = data.symbol;
        if (nameText != null) nameText.text = data.cardName;
        if (keywordText != null) keywordText.text = string.Join(" / ", data.keywords);
    }
}
