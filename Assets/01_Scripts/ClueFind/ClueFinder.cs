using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// 단서 찾기 화면. 손님 프리팹 자식으로 있는 단서 이미지 3개를 클릭하면 ClueFindingPanel 안의 확대 레이어를 보여준다.
// "다음(질문하기로)" 버튼은 인스펙터에서 이 컴포넌트의 ConfirmClueFinding()을 참조하도록 연결한다.
public class ClueFinder : MonoBehaviour
{
    [Header("단서 이미지 3개 (손님 프리팹의 자식, Button 컴포넌트 필요)")]
    [SerializeField] private Button[] clueButtons;

    [Header("단서 확대 오버레이 (ClueFindingPanel 내부)")]
    [SerializeField] private GameObject clueZoomPanel;
    [SerializeField] private Image clueZoomImage;
    [SerializeField] private TMP_Text clueZoomText;
    [SerializeField] private Button clueZoomCloseButton;
    [SerializeField] private bool showClueTextInZoom;

    [Header("사운드 키")]
    [SerializeField] private string clueClickSfxKey = "piiik";

    private CustomerData currentCustomer;
    private bool isInteractable;
    private readonly System.Collections.Generic.List<Button> boundButtons = new System.Collections.Generic.List<Button>();
    private readonly System.Collections.Generic.List<UnityAction> boundActions = new System.Collections.Generic.List<UnityAction>();

    private void Awake()
    {
        BindClueButtons();

        if (clueZoomCloseButton != null)
        {
            clueZoomCloseButton.onClick.AddListener(CloseClueZoom);
        }

        HideClueZoom();
    }

    private void OnDestroy()
    {
        ClearBoundClueButtons();

        if (clueZoomCloseButton != null)
        {
            clueZoomCloseButton.onClick.RemoveListener(CloseClueZoom);
        }
    }

    // GameFlowManager가 단서 찾기 화면 진입 시 호출: 지금 손님의 단서 데이터를 기억해둔다
    public void ShowClues(CustomerData customer)
    {
        currentCustomer = customer;
        HideClueZoom();
    }

    // 단서 찾기 상태가 아닐 때 GameFlowManager가 호출: 클릭(interactable)과 호버(raycastTarget) 둘 다 막는다
    public void SetInteractable(bool enabled)
    {
        isInteractable = enabled;
        if (!enabled) HideClueZoom();
        SetClueButtonsEnabled(enabled);
    }

    public void CloseClueZoom()
    {
        HideClueZoom();
        SetClueButtonsEnabled(isInteractable);
    }

    private void BindClueButtons()
    {
        ClearBoundClueButtons();
        if (clueButtons == null) return;

        for (int i = 0; i < clueButtons.Length; i++)
        {
            Button button = clueButtons[i];
            if (button == null) continue;

            int index = i; // 클로저 캡처용
            UnityAction action = () => HandleClueButtonClicked(index);
            button.onClick.AddListener(action);
            boundButtons.Add(button);
            boundActions.Add(action);
        }
    }

    private void ClearBoundClueButtons()
    {
        for (int i = 0; i < boundButtons.Count; i++)
        {
            Button button = boundButtons[i];
            UnityAction action = i < boundActions.Count ? boundActions[i] : null;
            if (button != null && action != null) button.onClick.RemoveListener(action);
        }

        boundButtons.Clear();
        boundActions.Clear();
    }

    private void SetClueButtonsEnabled(bool enabled)
    {
        if (clueButtons == null) return;

        foreach (var button in clueButtons)
        {
            if (button == null) continue;

            button.interactable = enabled;
            if (button.targetGraphic != null) button.targetGraphic.raycastTarget = enabled;
        }
    }

    private void HandleClueButtonClicked(int index)
    {
        if (!isInteractable) return;

        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(clueClickSfxKey);
        var clues = currentCustomer != null ? currentCustomer.Dialogue.clues : null;
        string clueText = clues != null && index >= 0 && index < clues.Count ? clues[index] : string.Empty;

        if (GameManager.Instance != null) GameManager.Instance.IncrementClueFindCount();
        ShowClueZoom(index, clueText);
    }

    private void ShowClueZoom(int index, string clueText)
    {
        if (clueZoomPanel == null) return;

        Sprite clueSprite = GetClueSprite(index);
        if (clueZoomImage != null)
        {
            clueZoomImage.sprite = clueSprite;
            clueZoomImage.enabled = clueSprite != null;
        }

        if (clueZoomText != null)
        {
            clueZoomText.gameObject.SetActive(showClueTextInZoom && !string.IsNullOrWhiteSpace(clueText));
            clueZoomText.text = clueText;
        }

        clueZoomPanel.transform.SetAsLastSibling();
        clueZoomPanel.SetActive(true);
        SetClueButtonsEnabled(false);
    }

    private Sprite GetClueSprite(int index)
    {
        var visuals = currentCustomer != null ? currentCustomer.clueVisuals : null;
        if (visuals != null && index >= 0 && index < visuals.Length && visuals[index] != null)
        {
            return visuals[index].sprite;
        }

        if (clueButtons != null && index >= 0 && index < clueButtons.Length)
        {
            Image image = clueButtons[index] != null ? clueButtons[index].targetGraphic as Image : null;
            if (image != null) return image.sprite;
        }

        return null;
    }

    private void HideClueZoom()
    {
        if (clueZoomPanel != null) clueZoomPanel.SetActive(false);
        if (clueZoomImage != null)
        {
            clueZoomImage.sprite = null;
            clueZoomImage.enabled = false;
        }
        if (clueZoomText != null)
        {
            clueZoomText.text = string.Empty;
            clueZoomText.gameObject.SetActive(false);
        }
    }

    // 단서 찾기 화면의 "다음(질문하기로)" 버튼 onClick에 연결 -> 카드 선택 상태로. 이 상태부터는 질문 패널과
    // 카드덱 패널이 동시에 활성화되어 카드를 고르면서 언제든 질문할 수 있다.
    public void ConfirmClueFinding()
    {
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.ChangeState(GameFlowManager.GameState.CardSelecting);
    }
}
