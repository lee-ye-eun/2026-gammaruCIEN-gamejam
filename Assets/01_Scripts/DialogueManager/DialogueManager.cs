using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

// 대사창/질문창(다이얼로그 패널의 두 자식) 출력 + 화면 터치 감지를 담당.
// 다이얼로그 패널 자체의 활성 상태는 GameManager가 관리하지만, 그 안에서 대사창/질문창 중 뭘 보여줄지는 여기서 결정한다.
// 흐름 전환은 이벤트가 아니라 GameManager.Instance.ChangeState(...)를 직접 호출해서 처리한다.
// 질문창의 "다음(질문 그만)" 버튼은 인스펙터에서 이 컴포넌트의 FinishQuestioning()을 참조하도록 연결한다.
public class DialogueManager : MonoBehaviour
{
    // 지금 대사창(일반 텍스트)을 보여주는 중인지, 질문 선택 중인지, 선택한 질문의 답을 보여주는 중인지
    private enum Mode { Normal, SelectingQuestion, ShowingAnswer }

    [Header("대사창 (다이얼로그 패널의 자식)")]
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private TMP_Text dialogueText;

    [Header("질문창 (다이얼로그 패널의 자식, 질문 차례에만 사용)")]
    [SerializeField] private GameObject questionBox;
    [SerializeField] private Button[] questionButtons; // 질문 4개 버튼 (라벨은 버튼 자식의 TMP_Text에서 자동으로 채움)

    private Mode mode;
    private bool isTouchActive;
    private CustomerData questioningCustomer;

    private void Awake()
    {
        for (int i = 0; i < questionButtons.Length; i++)
        {
            int index = i; // 클로저 캡처용
            if (questionButtons[i] != null) questionButtons[i].onClick.AddListener(() => HandleQuestionButtonClicked(index));
        }
    }

    private void OnDestroy()
    {
        foreach (var button in questionButtons)
        {
            if (button != null) button.onClick.RemoveAllListeners();
        }
    }

    private void Update()
    {
        if (!isTouchActive) return;
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            AdvanceTouch();
        }
    }

    // 임의의 대사 한 줄을 대사창에 출력한다 (손님 관찰 대사, 카드 결과 대사 등 공용)
    public void ShowDialogue(string text)
    {
        mode = Mode.Normal;
        SetBoxes(dialogueOn: true, questionOn: false);
        if (dialogueText != null) dialogueText.text = text;
        isTouchActive = true;
    }

    // 손님 데이터에 지정된 고민 대사를 출력
    public void ShowDialogue(CustomerData customer)
    {
        ShowDialogue(customer != null ? customer.Dialogue.worryText : string.Empty);
    }

    // 질문 차례 시작: 질문창부터 보여준다
    public void ShowQuestionTurn(CustomerData customer)
    {
        questioningCustomer = customer;
        mode = Mode.SelectingQuestion;
        isTouchActive = false; // 터치 감지는 답변을 보여줄 때만 켠다

        SetBoxes(dialogueOn: false, questionOn: true);
        PopulateQuestionButtons(customer);
    }

    private void SetBoxes(bool dialogueOn, bool questionOn)
    {
        if (dialogueBox != null) dialogueBox.SetActive(dialogueOn);
        if (questionBox != null) questionBox.SetActive(questionOn);
    }

    private void PopulateQuestionButtons(CustomerData customer)
    {
        var questions = customer != null ? customer.Dialogue.questions : null;

        for (int i = 0; i < questionButtons.Length; i++)
        {
            var button = questionButtons[i];
            if (button == null) continue;

            bool hasQuestion = questions != null && i < questions.Count;
            button.gameObject.SetActive(hasQuestion);
            if (!hasQuestion) continue;

            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = questions[i].questionText;
        }
    }

    private void HandleQuestionButtonClicked(int index)
    {
        if (mode != Mode.SelectingQuestion) return;

        var questions = questioningCustomer != null ? questioningCustomer.Dialogue.questions : null;
        if (questions == null || index >= questions.Count) return;

        mode = Mode.ShowingAnswer;
        SetBoxes(dialogueOn: true, questionOn: false);
        if (dialogueText != null) dialogueText.text = questions[index].clueText;
        isTouchActive = true; // 터치하면 다시 질문창으로

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterQuestionLog(questions[index].questionText, questions[index].clueText);
            GameManager.Instance.IncrementQuestionAskedCount();
        }
    }

    // 질문창의 "다음(질문 그만)" 버튼 onClick에 연결 -> 카드 덱으로 전환
    public void FinishQuestioning()
    {
        if (mode != Mode.SelectingQuestion) return;

        isTouchActive = false;
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.CardSelecting);
    }

    private void AdvanceTouch()
    {
        isTouchActive = false;

        if (mode == Mode.ShowingAnswer)
        {
            // 질문 차례 중 답변을 봤다면 다시 질문 선택으로 돌아간다 (GameManager 상태는 그대로 Questioning 유지)
            mode = Mode.SelectingQuestion;
            SetBoxes(dialogueOn: false, questionOn: true);
            return;
        }

        // 일반 대사(관찰 -> 단서 찾기, 카드 결과 -> 결과창) 넘김
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.CurrentState == GameManager.GameState.Observing)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.ClueFinding);
        }
        else if (GameManager.Instance.CurrentState == GameManager.GameState.ClueFinding)
        {
            // 단서 텍스트를 보다가 터치하면 단서 찾기 화면으로 복귀 (ClueFinding 상태는 그대로 유지)
            GameManager.Instance.ReturnToClueFinding();
        }
        else if (GameManager.Instance.CurrentState == GameManager.GameState.ShowingCardResult)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.ShowingResultPanel);
        }
    }
}
