using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class GameJamPrototypeUI : MonoBehaviour
{
    private const float ReferenceWidth = 1280f;
    private const float ReferenceHeight = 720f;
    private const int MaxQuestionCount = 2;

    private static readonly Color PageColor = new Color32(245, 245, 249, 255);
    private static readonly Color PaperColor = new Color32(255, 255, 255, 255);
    private static readonly Color InkColor = new Color32(26, 24, 24, 255);
    private static readonly Color MutedInkColor = new Color32(86, 84, 84, 255);
    private static readonly Color TableColor = new Color32(181, 160, 130, 255);
    private static readonly Color FrontTableColor = new Color32(202, 239, 236, 255);
    private static readonly Color CardBackColor = new Color32(93, 25, 49, 255);
    private static readonly Color ButtonColor = new Color32(237, 237, 237, 255);
    private static readonly Color SuspicionColor = new Color32(220, 72, 72, 255);

    private readonly string[] slotLabels = { "원인", "현재", "조언" };
    private readonly string[] answerCards = { "달", "연인", "태양" };

    private RectTransform root;
    private RectTransform dragLayer;
    private RectTransform gameFrame;
    private GameObject startScreen;
    private GameObject gameScreen;
    private GameObject dialogueLayer;
    private GameObject questionLayer;
    private GameObject cardLayer;
    private GameObject resultLayer;
    private GameObject readingOverlay;
    private TextMeshProUGUI dialogueText;
    private TextMeshProUGUI clueText;
    private TextMeshProUGUI resultBodyText;
    private TextMeshProUGUI suspicionText;
    private RectTransform suspicionFill;
    private Button drawCardsButton;
    private Button dealCardsButton;
    private Button decideButton;
    private GameObject drawPile;

    private readonly RectTransform[] slotRects = new RectTransform[3];
    private readonly PrototypeCardWidget[] slottedCards = new PrototypeCardWidget[3];
    private readonly List<PrototypeCardWidget> cards = new List<PrototypeCardWidget>();
    private readonly List<Button> questionButtons = new List<Button>();

    private int suspicion;
    private int questionCount;
    private float readingTimer;
    private float suspicionPrecise;
    private bool readingSheetOpen;
    private bool cardsDrawn;
    private bool resolvingSelection;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateOnSceneLoad()
    {
        if (FindObjectOfType<GameJamPrototypeUI>() != null) return;

        var go = new GameObject("GameJamPrototypeUI");
        DontDestroyOnLoad(go);
        go.AddComponent<GameJamPrototypeUI>();
    }

    private void Awake()
    {
        Build();
    }

    private void Update()
    {
        if (!readingSheetOpen) return;

        readingTimer += Time.deltaTime;
        if (readingTimer <= 7f) return;

        suspicionPrecise += Time.deltaTime * 0.4f;
        int rounded = Mathf.FloorToInt(suspicionPrecise);
        if (rounded > suspicion)
        {
            suspicion = Mathf.Clamp(rounded, 0, 100);
            UpdateSuspicionGauge();
        }
    }

    public RectTransform DragLayer => dragLayer;

    public bool TryPlaceCardAtScreenPosition(PrototypeCardWidget card, Vector2 screenPosition)
    {
        if (!cardsDrawn || resolvingSelection) return false;

        for (int i = 0; i < slotRects.Length; i++)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(slotRects[i], screenPosition, null))
            {
                PlaceCard(card, i);
                return true;
            }
        }

        return false;
    }

    public void PlaceCardInFirstEmptySlot(PrototypeCardWidget card)
    {
        if (!cardsDrawn || resolvingSelection) return;

        if (card.CurrentSlotIndex >= 0)
        {
            ReturnCard(card);
            return;
        }

        for (int i = 0; i < slottedCards.Length; i++)
        {
            if (slottedCards[i] == null)
            {
                PlaceCard(card, i);
                return;
            }
        }
    }

    public void RestoreCardAfterDrag(PrototypeCardWidget card)
    {
        if (card.CurrentSlotIndex >= 0)
        {
            MoveCardToSlot(card, card.CurrentSlotIndex);
            return;
        }

        card.ReturnHome();
    }

    private void Build()
    {
        EnsureEventSystem();
        BuildCanvas();
        BuildStartScreen();
        BuildGameScreen();
        dragLayer.SetAsLastSibling();
        ShowStartScreen();
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    private void BuildCanvas()
    {
        var canvasObject = new GameObject("GameJam Prototype Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.matchWidthOrHeight = 0.5f;

        root = canvasObject.GetComponent<RectTransform>();
        Stretch(root);

        CreatePanel("Page Background", root, Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight), PageColor);
        dragLayer = CreateEmpty("DragLayer", root, Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight));
        dragLayer.SetAsLastSibling();
    }

    private void BuildStartScreen()
    {
        startScreen = CreateScreen("StartScreen");
        var frame = CreatePanel("StartFrame", startScreen.transform, Vector2.zero, new Vector2(1040f, 540f), PaperColor);
        frame.gameObject.AddComponent<Outline>().effectColor = InkColor;

        CreateText("Title", frame, "게임제목", 58, FontStyles.Normal, InkColor, TextAlignmentOptions.Center, new Vector2(0f, 186f), new Vector2(600f, 90f));

        var tent = CreateEmpty("Tent", frame, new Vector2(0f, -20f), new Vector2(620f, 330f));
        AddPolygon("TentRoofWhite", tent, new Color32(250, 250, 250, 255),
            new Vector2(-310f, -40f), new Vector2(0f, 170f), new Vector2(310f, -40f));
        AddPolygon("TentRoofRedLeft", tent, new Color32(255, 72, 88, 255),
            new Vector2(-230f, -40f), new Vector2(0f, 170f), new Vector2(-80f, -40f));
        AddPolygon("TentRoofRedRight", tent, new Color32(255, 72, 88, 255),
            new Vector2(80f, -40f), new Vector2(0f, 170f), new Vector2(230f, -40f));
        AddPolygon("TentBodyRedLeft", tent, new Color32(255, 72, 88, 255),
            new Vector2(-260f, -150f), new Vector2(-210f, -40f), new Vector2(-90f, -40f), new Vector2(-130f, -150f));
        AddPolygon("TentBodyWhiteMid", tent, new Color32(250, 250, 250, 255),
            new Vector2(-120f, -150f), new Vector2(-70f, -40f), new Vector2(70f, -40f), new Vector2(120f, -150f));
        AddPolygon("TentBodyRedRight", tent, new Color32(255, 72, 88, 255),
            new Vector2(130f, -150f), new Vector2(90f, -40f), new Vector2(210f, -40f), new Vector2(260f, -150f));
        AddPolygon("TentDoor", tent, new Color32(118, 68, 58, 255),
            new Vector2(-54f, -150f), new Vector2(-35f, -70f), new Vector2(0f, -50f), new Vector2(35f, -70f), new Vector2(54f, -150f));
        CreatePanel("TentBase", tent, new Vector2(0f, -158f), new Vector2(620f, 18f), new Color32(240, 177, 80, 255));
        AddLine("TentOutline", tent, InkColor, 3f,
            new Vector2(-310f, -158f), new Vector2(-230f, -40f), new Vector2(0f, 170f), new Vector2(230f, -40f), new Vector2(310f, -158f), new Vector2(-310f, -158f));

        var startButton = CreateButton("StartButton", frame, "Press to Start", new Vector2(0f, -228f), new Vector2(340f, 58f), new Color(1f, 1f, 1f, 0f));
        startButton.onClick.AddListener(ShowDialogueScreen);
    }

    private void BuildGameScreen()
    {
        gameScreen = CreateScreen("GameScreen");
        gameFrame = CreatePanel("GameFrame", gameScreen.transform, Vector2.zero, new Vector2(1080f, 600f), PaperColor);
        gameFrame.gameObject.AddComponent<Outline>().effectColor = InkColor;

        CreatePanel("RoomBack", gameFrame, new Vector2(0f, 118f), new Vector2(1080f, 300f), PaperColor);
        CreatePanel("TableTop", gameFrame, new Vector2(0f, -82f), new Vector2(1080f, 280f), TableColor);
        CreatePanel("TableFront", gameFrame, new Vector2(0f, -238f), new Vector2(1080f, 88f), FrontTableColor);
        CreatePanel("BottomFloor", gameFrame, new Vector2(0f, -292f), new Vector2(1080f, 56f), PaperColor);

        BuildCustomer();
        BuildSuspicionGauge();
        BuildReadingSheet();
        BuildDialogueLayer();
        BuildQuestionLayer();
        BuildCardLayer();
        BuildResultLayer();

        gameScreen.SetActive(false);
    }

    private void BuildCustomer()
    {
        var customer = CreateEmpty("CustomerSilhouette", gameFrame, new Vector2(-340f, 105f), new Vector2(250f, 280f));
        AddLine("CustomerLine", customer, InkColor, 4f,
            new Vector2(-92f, -78f), new Vector2(-84f, 4f), new Vector2(-66f, 58f), new Vector2(-28f, 86f),
            new Vector2(-68f, 96f), new Vector2(-74f, 146f), new Vector2(-54f, 184f), new Vector2(18f, 196f),
            new Vector2(55f, 184f), new Vector2(78f, 105f), new Vector2(44f, 95f), new Vector2(82f, 70f),
            new Vector2(104f, 8f), new Vector2(112f, -78f));
    }

    private void BuildSuspicionGauge()
    {
        var gauge = CreatePanel("SuspicionGauge", gameFrame, new Vector2(300f, 230f), new Vector2(330f, 38f), new Color32(218, 218, 218, 255));
        gauge.gameObject.AddComponent<Outline>().effectColor = new Color32(160, 160, 160, 255);
        suspicionFill = CreatePanel("SuspicionFill", gauge, new Vector2(-165f, 0f), new Vector2(0f, 38f), SuspicionColor);
        suspicionFill.anchorMin = new Vector2(0f, 0f);
        suspicionFill.anchorMax = new Vector2(0f, 1f);
        suspicionFill.pivot = new Vector2(0f, 0.5f);
        suspicionFill.anchoredPosition = Vector2.zero;
        suspicionFill.sizeDelta = new Vector2(0f, 0f);
        suspicionText = CreateText("SuspicionText", gauge, "의심도 0/100", 20, FontStyles.Normal, InkColor, TextAlignmentOptions.Center, Vector2.zero, new Vector2(330f, 38f));
    }

    private void BuildReadingSheet()
    {
        var sheetButton = CreateButton("ReadingSheetSmall", gameFrame, "", new Vector2(-488f, -258f), new Vector2(95f, 118f), PaperColor);
        sheetButton.transform.localRotation = Quaternion.Euler(0f, 0f, -16f);
        sheetButton.gameObject.AddComponent<Outline>().effectColor = InkColor;
        sheetButton.onClick.AddListener(OpenReadingSheet);
        var hover = sheetButton.gameObject.AddComponent<PrototypeHoverScale>();
        hover.Initialize(1.12f);

        AddLine("SmallSheetLines", sheetButton.transform, InkColor, 4f,
            new Vector2(-32f, 26f), new Vector2(-18f, 20f), new Vector2(-6f, 24f), new Vector2(12f, 12f), new Vector2(32f, 16f),
            new Vector2(-36f, -4f), new Vector2(-18f, -10f), new Vector2(6f, -8f), new Vector2(24f, -18f),
            new Vector2(-34f, -34f), new Vector2(-18f, -42f), new Vector2(4f, -38f), new Vector2(28f, -48f));

        readingOverlay = CreateScreen("ReadingSheetOverlay");
        readingOverlay.transform.SetParent(gameFrame, false);
        var overlayRect = readingOverlay.GetComponent<RectTransform>();
        Stretch(overlayRect);
        var dim = CreateButton("DimBackground", readingOverlay.transform, "", Vector2.zero, gameFrame.sizeDelta, new Color(0f, 0f, 0f, 0.75f));
        dim.onClick.AddListener(CloseReadingSheet);

        var largeSheet = CreatePanel("ReadingSheetLarge", readingOverlay.transform, Vector2.zero, new Vector2(470f, 520f), PaperColor);
        largeSheet.gameObject.AddComponent<Outline>().effectColor = InkColor;
        CreateText("ReadingTitle", largeSheet, "타로 카드 해석표", 30, FontStyles.Bold, InkColor, TextAlignmentOptions.Center, new Vector2(0f, 205f), new Vector2(390f, 48f));
        CreateText("ReadingBody", largeSheet,
            "달        비밀\n\n연인      애정\n\n태양      솔직\n\n탑        갈등\n\n은둔자    기다림\n\n악마      집착",
            25, FontStyles.Normal, InkColor, TextAlignmentOptions.Left, new Vector2(18f, 0f), new Vector2(330f, 330f));
        CreateText("ReadingHint", largeSheet, "배경을 클릭하면 닫힙니다", 18, FontStyles.Normal, MutedInkColor, TextAlignmentOptions.Center, new Vector2(0f, -226f), new Vector2(360f, 36f));

        readingOverlay.SetActive(false);
    }

    private void BuildDialogueLayer()
    {
        dialogueLayer = CreateLayer("DialogueLayer");
        var dialogueBox = CreatePanel("DialogueBox", dialogueLayer.transform, new Vector2(150f, 116f), new Vector2(480f, 150f), new Color32(218, 218, 218, 255));
        dialogueText = CreateText("DialogueText", dialogueBox, "", 26, FontStyles.Normal, InkColor, TextAlignmentOptions.MidlineLeft, Vector2.zero, new Vector2(410f, 116f));
        var next = CreateButton("DialogueNextButton", dialogueLayer.transform, "계속", new Vector2(394f, -60f), new Vector2(120f, 44f), ButtonColor);
        next.onClick.AddListener(ShowQuestionScreen);
    }

    private void BuildQuestionLayer()
    {
        questionLayer = CreateLayer("QuestionLayer");
        CreateText("QuestionHeader", questionLayer.transform, "질문을 최대 2개까지 고를 수 있습니다", 24, FontStyles.Bold, InkColor, TextAlignmentOptions.Center, new Vector2(130f, 236f), new Vector2(480f, 42f));

        string[] questions =
        {
            "남자친구의 행동이 언제부터 달라졌나요?",
            "곧 특별한 날이 있나요?",
            "직접 물어봤나요?",
            "꽃다발은 누구에게 받은 건가요?"
        };

        for (int i = 0; i < questions.Length; i++)
        {
            var button = CreateButton("QuestionButton_" + (i + 1), questionLayer.transform, questions[i], new Vector2(130f, 160f - i * 56f), new Vector2(520f, 42f), new Color32(218, 218, 218, 255));
            int index = i;
            button.onClick.AddListener(() => AskQuestion(index));
            questionButtons.Add(button);
        }

        clueText = CreateText("ClueText", questionLayer.transform, "", 22, FontStyles.Normal, InkColor, TextAlignmentOptions.Center, new Vector2(130f, -96f), new Vector2(520f, 72f));
        drawCardsButton = CreateButton("DrawCardsButton", questionLayer.transform, "카드 뽑기", new Vector2(394f, -162f), new Vector2(136f, 46f), ButtonColor);
        drawCardsButton.onClick.AddListener(ShowCardSelectionScreen);
    }

    private void BuildCardLayer()
    {
        cardLayer = CreateLayer("CardLayer");

        for (int i = 0; i < slotLabels.Length; i++)
        {
            float x = -116f + i * 116f;
            CreateText(slotLabels[i] + "Label", cardLayer.transform, slotLabels[i], 21, FontStyles.Bold, InkColor, TextAlignmentOptions.Center, new Vector2(x, -5f), new Vector2(88f, 28f));
            var slot = CreatePanel(slotLabels[i] + "Slot", cardLayer.transform, new Vector2(x, -66f), new Vector2(78f, 116f), new Color(1f, 1f, 1f, 0.08f));
            var outline = slot.gameObject.AddComponent<Outline>();
            outline.effectColor = InkColor;
            outline.effectDistance = new Vector2(2f, -2f);
            slotRects[i] = slot;
        }

        decideButton = CreateButton("DecideButton", cardLayer.transform, "선택 결정", new Vector2(334f, -70f), new Vector2(136f, 48f), ButtonColor);
        decideButton.interactable = false;
        decideButton.onClick.AddListener(ShowResultScreen);

        drawPile = CreateEmpty("DrawPile", cardLayer.transform, new Vector2(334f, -170f), new Vector2(100f, 140f)).gameObject;
        CreatePanel("DeckCardBack_1", drawPile.transform, new Vector2(-8f, 8f), new Vector2(82f, 126f), CardBackColor).gameObject.AddComponent<Outline>().effectColor = InkColor;
        CreatePanel("DeckCardBack_2", drawPile.transform, new Vector2(0f, 0f), new Vector2(82f, 126f), CardBackColor).gameObject.AddComponent<Outline>().effectColor = InkColor;
        CreatePanel("DeckCardBack_3", drawPile.transform, new Vector2(8f, -8f), new Vector2(82f, 126f), CardBackColor).gameObject.AddComponent<Outline>().effectColor = InkColor;

        dealCardsButton = CreateButton("DealCardsButton", cardLayer.transform, "카드 드로우", new Vector2(334f, -258f), new Vector2(142f, 44f), ButtonColor);
        dealCardsButton.onClick.AddListener(() => StartCoroutine(DrawCardsRoutine()));

        string[,] cardDefinitions =
        {
            { "달", "비밀" },
            { "연인", "애정" },
            { "태양", "솔직" },
            { "탑", "갈등" },
            { "은둔자", "기다림" },
            { "악마", "집착" }
        };

        for (int i = 0; i < cardDefinitions.GetLength(0); i++)
        {
            float x = -330f + i * 132f;
            var card = CreateCard(cardDefinitions[i, 0], cardDefinitions[i, 1], new Vector2(x, -170f));
            card.gameObject.SetActive(false);
            cards.Add(card);
        }
    }

    private void BuildResultLayer()
    {
        resultLayer = CreateLayer("ResultLayer");
        var resultBox = CreatePanel("ResultBox", resultLayer.transform, new Vector2(0f, -28f), new Vector2(520f, 220f), new Color32(218, 218, 218, 255));
        resultBox.gameObject.AddComponent<Outline>().effectColor = InkColor;
        CreateText("ResultTitle", resultBox, "해석 결과", 32, FontStyles.Bold, InkColor, TextAlignmentOptions.Center, new Vector2(0f, 70f), new Vector2(420f, 44f));
        resultBodyText = CreateText("ResultGuide", resultBox, "선택한 카드에 맞춰 손님에게 점괘를 말합니다.", 22, FontStyles.Normal, InkColor, TextAlignmentOptions.Center, new Vector2(0f, -30f), new Vector2(450f, 130f));
    }

    private void ShowStartScreen()
    {
        startScreen.SetActive(true);
        gameScreen.SetActive(false);
    }

    private void ShowDialogueScreen()
    {
        startScreen.SetActive(false);
        gameScreen.SetActive(true);
        SetLayer(dialogueLayer);
        dialogueText.text = "남자친구가 뭔가 숨기는 것 같아요.\n혹시 제 고민을 봐주실 수 있나요?";
        ResetRound();
    }

    private void ShowQuestionScreen()
    {
        SetLayer(questionLayer);
        clueText.text = "질문을 고르면 단서가 공개됩니다.";
    }

    private void ShowCardSelectionScreen()
    {
        SetLayer(cardLayer);
        CloseReadingSheet();
        PrepareCardsForDraw();
    }

    private void ShowResultScreen()
    {
        if (!cardsDrawn || resolvingSelection) return;
        if (!AllSlotsFilled()) return;

        StartCoroutine(FlipSelectedCardsThenShowResult());
    }

    private void ShowResultAfterCardsFlip()
    {
        int wrongCount = CountWrongCards();
        int suspicionAdd = wrongCount == 0 ? 0 : wrongCount == 1 ? 15 : wrongCount == 2 ? 25 : 35;
        AddSuspicion(suspicionAdd);

        SetLayer(resultLayer);
        if (resultBodyText != null)
        {
            resultBodyText.text = BuildResultText(wrongCount, suspicionAdd);
            resultBodyText.fontSize = 22;
            resultBodyText.alignment = TextAlignmentOptions.Center;
        }
    }

    private IEnumerator DrawCardsRoutine()
    {
        if (cardsDrawn || resolvingSelection) yield break;

        resolvingSelection = true;
        cardsDrawn = false;
        if (dealCardsButton != null) dealCardsButton.interactable = false;

        Vector2 pilePosition = drawPile != null ? drawPile.GetComponent<RectTransform>().anchoredPosition : new Vector2(334f, -170f);

        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].PrepareForDraw(pilePosition);
            yield return StartCoroutine(cards[i].MoveHomeFromCurrentPosition(0.22f));
            yield return new WaitForSeconds(0.04f);
        }

        if (drawPile != null) drawPile.SetActive(false);
        if (dealCardsButton != null) dealCardsButton.gameObject.SetActive(false);
        cardsDrawn = true;
        resolvingSelection = false;
        UpdateDecideButton();
    }

    private IEnumerator FlipSelectedCardsThenShowResult()
    {
        resolvingSelection = true;
        if (decideButton != null) decideButton.interactable = false;

        for (int i = 0; i < slottedCards.Length; i++)
        {
            if (slottedCards[i] == null) continue;

            yield return StartCoroutine(slottedCards[i].FlipFaceUp(0.26f));
            yield return new WaitForSeconds(0.08f);
        }

        yield return new WaitForSeconds(0.32f);
        resolvingSelection = false;
        ShowResultAfterCardsFlip();
    }

    private void SetLayer(GameObject activeLayer)
    {
        dialogueLayer.SetActive(activeLayer == dialogueLayer);
        questionLayer.SetActive(activeLayer == questionLayer);
        cardLayer.SetActive(activeLayer == cardLayer);
        resultLayer.SetActive(activeLayer == resultLayer);
    }

    private void ResetRound()
    {
        questionCount = 0;
        PrepareCardsForDraw();
        foreach (var button in questionButtons) button.interactable = true;
        if (decideButton != null) decideButton.interactable = false;
        if (drawCardsButton != null) drawCardsButton.interactable = true;
        if (clueText != null) clueText.text = "";
    }

    private void AskQuestion(int index)
    {
        if (questionCount >= MaxQuestionCount) return;

        questionCount++;
        if (questionCount > 1) AddSuspicion(5);

        string[] clues =
        {
            "일주일 정도 됐어요. 말수가 줄었지만 계속 곁에 있어요.",
            "이틀 뒤가 제 생일이에요.",
            "조금만 기다려달라고 했어요.",
            "꽃다발은 남자친구가 준 거예요."
        };

        if (clueText != null) clueText.text = clues[Mathf.Clamp(index, 0, clues.Length - 1)];
        if (index >= 0 && index < questionButtons.Count) questionButtons[index].interactable = false;

        if (questionCount >= MaxQuestionCount)
        {
            foreach (var button in questionButtons) button.interactable = false;
        }
    }

    private void OpenReadingSheet()
    {
        readingSheetOpen = true;
        readingTimer = 0f;
        suspicionPrecise = suspicion;
        readingOverlay.SetActive(true);
        readingOverlay.transform.SetAsLastSibling();
        dragLayer.SetAsLastSibling();
    }

    private void CloseReadingSheet()
    {
        if (readingOverlay == null) return;

        readingSheetOpen = false;
        readingOverlay.SetActive(false);
    }

    private void PlaceCard(PrototypeCardWidget card, int slotIndex)
    {
        if (!cardsDrawn || resolvingSelection) return;
        if (slotIndex < 0 || slotIndex >= slottedCards.Length) return;

        int previousSlot = card.CurrentSlotIndex;
        if (previousSlot >= 0 && previousSlot < slottedCards.Length && slottedCards[previousSlot] == card)
        {
            slottedCards[previousSlot] = null;
        }

        var displacedCard = slottedCards[slotIndex];
        if (displacedCard != null && displacedCard != card)
        {
            ReturnCard(displacedCard);
        }

        slottedCards[slotIndex] = card;
        card.SetSlot(slotIndex, slotLabels[slotIndex]);
        MoveCardToSlot(card, slotIndex);
        UpdateDecideButton();
    }

    private void MoveCardToSlot(PrototypeCardWidget card, int slotIndex)
    {
        card.transform.SetParent(slotRects[slotIndex], false);
        var rect = card.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(78f, 116f);
        rect.localScale = Vector3.one;
    }

    private void ReturnCard(PrototypeCardWidget card)
    {
        int slotIndex = card.CurrentSlotIndex;
        if (slotIndex >= 0 && slotIndex < slottedCards.Length && slottedCards[slotIndex] == card)
        {
            slottedCards[slotIndex] = null;
        }

        card.SetSlot(-1, string.Empty);
        card.ReturnHome();
        UpdateDecideButton();
    }

    private void UpdateDecideButton()
    {
        bool ready = cardsDrawn && !resolvingSelection && AllSlotsFilled();

        if (decideButton != null) decideButton.interactable = ready;
    }

    private bool AllSlotsFilled()
    {
        for (int i = 0; i < slottedCards.Length; i++)
        {
            if (slottedCards[i] == null) return false;
        }

        return true;
    }

    private void PrepareCardsForDraw()
    {
        cardsDrawn = false;
        resolvingSelection = false;

        for (int i = 0; i < slottedCards.Length; i++)
        {
            slottedCards[i] = null;
        }

        foreach (var card in cards)
        {
            card.SetSlot(-1, string.Empty);
            card.ReturnHome();
            card.SetFaceUp(false);
            card.gameObject.SetActive(false);
        }

        if (drawPile != null) drawPile.SetActive(true);
        if (dealCardsButton != null)
        {
            dealCardsButton.gameObject.SetActive(true);
            dealCardsButton.interactable = true;
        }

        if (decideButton != null) decideButton.interactable = false;
    }

    private int CountWrongCards()
    {
        int wrong = 0;
        for (int i = 0; i < answerCards.Length; i++)
        {
            if (slottedCards[i] == null || slottedCards[i].CardName != answerCards[i]) wrong++;
        }

        return wrong;
    }

    private string BuildResultText(int wrongCount, int suspicionAdd)
    {
        string reaction = wrongCount == 0
            ? "손님: ...소름 돋았어요. 딱 맞아요."
            : wrongCount == 1
                ? "손님: 어느 정도는 맞는 것 같아요."
                : wrongCount == 2
                    ? "손님: 음... 조금 애매한데요?"
                    : "손님: 정말 타로를 볼 줄 아는 거 맞나요?";

        return $"{reaction}\n\n정답: 원인=달, 현재=연인, 조언=태양\n의심도 +{suspicionAdd} / 현재 {suspicion}/100";
    }

    private void AddSuspicion(int amount)
    {
        suspicion = Mathf.Clamp(suspicion + amount, 0, 100);
        suspicionPrecise = suspicion;
        UpdateSuspicionGauge();
    }

    private void UpdateSuspicionGauge()
    {
        if (suspicionFill != null)
        {
            suspicionFill.sizeDelta = new Vector2(330f * Mathf.Clamp01(suspicion / 100f), 0f);
        }

        if (suspicionText != null)
        {
            suspicionText.text = suspicion >= 100 ? "의심도 100/100 - 배드 엔딩" : $"의심도 {suspicion}/100";
        }
    }

    private PrototypeCardWidget CreateCard(string cardName, string keyword, Vector2 position)
    {
        var cardRoot = CreatePanel("Card_" + cardName, cardLayer.transform, position, new Vector2(82f, 126f), CardBackColor);
        cardRoot.gameObject.AddComponent<Outline>().effectColor = InkColor;

        var frontPeek = CreatePanel("FrontPeek", cardRoot, new Vector2(0f, -92f), new Vector2(82f, 64f), new Color32(238, 238, 238, 255));
        frontPeek.SetAsFirstSibling();

        var cardLabel = CreateText("CardLabel", cardRoot, $"{cardName}\n{keyword}", 20, FontStyles.Bold, PaperColor, TextAlignmentOptions.Center, Vector2.zero, new Vector2(78f, 86f));
        cardLabel.gameObject.SetActive(false);

        var roleLabel = CreateText("RoleLabel", cardRoot, "", 18, FontStyles.Bold, new Color32(255, 221, 84, 255), TextAlignmentOptions.Center, new Vector2(0f, 48f), new Vector2(76f, 24f));
        roleLabel.gameObject.SetActive(false);

        var widget = cardRoot.gameObject.AddComponent<PrototypeCardWidget>();
        widget.Initialize(this, cardName, keyword, cardLabel, roleLabel, cardLayer.transform as RectTransform, position, new Vector2(82f, 126f));
        return widget;
    }

    private GameObject CreateScreen(string name)
    {
        var rect = CreateEmpty(name, root, Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight));
        Stretch(rect);
        return rect.gameObject;
    }

    private GameObject CreateLayer(string name)
    {
        var rect = CreateEmpty(name, gameFrame, Vector2.zero, gameFrame.sizeDelta);
        Stretch(rect);
        return rect.gameObject;
    }

    private RectTransform CreateEmpty(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private RectTransform CreatePanel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var rect = CreateEmpty(name, parent, position, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return rect;
    }

    private Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size, Color color)
    {
        var rect = CreatePanel(name, parent, position, size, color);
        var button = rect.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.08f);
        colors.disabledColor = new Color32(175, 175, 175, 150);
        button.colors = colors;

        if (!string.IsNullOrEmpty(label))
        {
            CreateText("Label", rect, label, 22, FontStyles.Normal, InkColor, TextAlignmentOptions.Center, Vector2.zero, size - new Vector2(16f, 8f));
        }

        return button;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, FontStyles style, Color color, TextAlignmentOptions alignment, Vector2 position, Vector2 rectSize)
    {
        var rect = CreateEmpty(name, parent, position, rectSize);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.color = color;
        label.alignment = alignment;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Overflow;
        return label;
    }

    private void AddLine(string name, Transform parent, Color color, float thickness, params Vector2[] points)
    {
        var rect = CreateEmpty(name, parent, Vector2.zero, parent.GetComponent<RectTransform>().sizeDelta);
        var line = rect.gameObject.AddComponent<PrototypeLineGraphic>();
        line.raycastTarget = false;
        line.color = color;
        line.Thickness = thickness;
        line.SetPoints(points);
    }

    private void AddPolygon(string name, Transform parent, Color color, params Vector2[] points)
    {
        var rect = CreateEmpty(name, parent, Vector2.zero, parent.GetComponent<RectTransform>().sizeDelta);
        var polygon = rect.gameObject.AddComponent<PrototypePolygonGraphic>();
        polygon.raycastTarget = false;
        polygon.color = color;
        polygon.SetPoints(points);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }
}

public class PrototypeCardWidget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private static readonly Color CardBackColor = new Color32(93, 25, 49, 255);
    private static readonly Color CardFrontColor = new Color32(250, 247, 236, 255);
    private static readonly Color InkColor = new Color32(26, 24, 24, 255);
    private static readonly Color PaperColor = new Color32(255, 255, 255, 255);

    private GameJamPrototypeUI owner;
    private RectTransform rect;
    private RectTransform homeParent;
    private Vector2 homePosition;
    private Vector2 homeSize;
    private TextMeshProUGUI frontLabel;
    private TextMeshProUGUI roleLabel;
    private CanvasGroup canvasGroup;
    private Image cardImage;
    private bool faceUp;
    private bool dragging;

    public string CardName { get; private set; }
    public int CurrentSlotIndex { get; private set; } = -1;

    public void Initialize(GameJamPrototypeUI owner, string cardName, string keyword, TextMeshProUGUI frontLabel, TextMeshProUGUI roleLabel, RectTransform homeParent, Vector2 homePosition, Vector2 homeSize)
    {
        this.owner = owner;
        this.frontLabel = frontLabel;
        this.roleLabel = roleLabel;
        this.homeParent = homeParent;
        this.homePosition = homePosition;
        this.homeSize = homeSize;
        CardName = cardName;

        rect = GetComponent<RectTransform>();
        cardImage = GetComponent<Image>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        SetFaceUp(false);
    }

    public void SetSlot(int slotIndex, string role)
    {
        CurrentSlotIndex = slotIndex;
        if (roleLabel != null)
        {
            roleLabel.text = role;
            roleLabel.gameObject.SetActive(slotIndex >= 0);
        }
    }

    public void ReturnHome()
    {
        transform.SetParent(homeParent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = homePosition;
        rect.sizeDelta = homeSize;
        rect.localScale = Vector3.one;
        if (!faceUp && frontLabel != null) frontLabel.gameObject.SetActive(false);
    }

    public void PrepareForDraw(Vector2 pilePosition)
    {
        gameObject.SetActive(true);
        transform.SetParent(homeParent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pilePosition;
        rect.sizeDelta = homeSize;
        rect.localScale = Vector3.one;
        SetFaceUp(false);
    }

    public IEnumerator MoveHomeFromCurrentPosition(float duration)
    {
        Vector2 start = rect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            rect.anchoredPosition = Vector2.LerpUnclamped(start, homePosition, t);
            yield return null;
        }

        rect.anchoredPosition = homePosition;
    }

    public void SetFaceUp(bool visible)
    {
        faceUp = visible;

        if (cardImage != null) cardImage.color = faceUp ? CardFrontColor : CardBackColor;
        if (frontLabel != null)
        {
            frontLabel.color = faceUp ? InkColor : PaperColor;
            frontLabel.gameObject.SetActive(faceUp);
        }
    }

    public IEnumerator FlipFaceUp(float duration)
    {
        float halfDuration = duration * 0.5f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float x = Mathf.Lerp(1f, 0.04f, Mathf.Clamp01(elapsed / halfDuration));
            rect.localScale = new Vector3(x, 1f, 1f);
            yield return null;
        }

        SetFaceUp(true);

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float x = Mathf.Lerp(0.04f, 1f, Mathf.Clamp01(elapsed / halfDuration));
            rect.localScale = new Vector3(x, 1f, 1f);
            yield return null;
        }

        rect.localScale = Vector3.one;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CurrentSlotIndex >= 0 || dragging) return;

        rect.localScale = Vector3.one * 1.08f;
        rect.anchoredPosition = homePosition + new Vector2(0f, -42f);
        if (!faceUp && frontLabel != null)
        {
            frontLabel.color = PaperColor;
            frontLabel.gameObject.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (CurrentSlotIndex >= 0 || dragging) return;

        rect.localScale = Vector3.one;
        rect.anchoredPosition = homePosition;
        if (!faceUp && frontLabel != null) frontLabel.gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (dragging) return;

        owner.PlaceCardInFirstEmptySlot(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = true;
        if (!faceUp && frontLabel != null)
        {
            frontLabel.color = PaperColor;
            frontLabel.gameObject.SetActive(true);
        }
        canvasGroup.blocksRaycasts = false;
        transform.SetParent(owner.DragLayer, true);
        rect.localScale = Vector3.one * 1.08f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(owner.DragLayer, eventData.position, null, out Vector2 localPoint);
        rect.anchoredPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
        canvasGroup.blocksRaycasts = true;

        if (!owner.TryPlaceCardAtScreenPosition(this, eventData.position))
        {
            owner.RestoreCardAfterDrag(this);
        }
    }
}

public class PrototypeHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private float hoverScale = 1.1f;

    public void Initialize(float scale)
    {
        hoverScale = scale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = Vector3.one * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
    }
}

public class PrototypeLineGraphic : MaskableGraphic
{
    private readonly List<Vector2> points = new List<Vector2>();

    public float Thickness { get; set; } = 2f;

    public void SetPoints(params Vector2[] newPoints)
    {
        points.Clear();
        points.AddRange(newPoints);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points.Count < 2) return;

        for (int i = 0; i < points.Count - 1; i++)
        {
            AddSegment(vh, points[i], points[i + 1]);
        }
    }

    private void AddSegment(VertexHelper vh, Vector2 start, Vector2 end)
    {
        Vector2 direction = (end - start).normalized;
        if (direction == Vector2.zero) return;

        Vector2 normal = new Vector2(-direction.y, direction.x) * (Thickness * 0.5f);
        int index = vh.currentVertCount;

        AddVertex(vh, start - normal);
        AddVertex(vh, start + normal);
        AddVertex(vh, end + normal);
        AddVertex(vh, end - normal);

        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index, index + 2, index + 3);
    }

    private void AddVertex(VertexHelper vh, Vector2 position)
    {
        var vertex = UIVertex.simpleVert;
        vertex.color = color;
        vertex.position = position;
        vh.AddVert(vertex);
    }
}

public class PrototypePolygonGraphic : MaskableGraphic
{
    private readonly List<Vector2> points = new List<Vector2>();

    public void SetPoints(params Vector2[] newPoints)
    {
        points.Clear();
        points.AddRange(newPoints);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points.Count < 3) return;

        for (int i = 0; i < points.Count; i++)
        {
            var vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = points[i];
            vh.AddVert(vertex);
        }

        for (int i = 1; i < points.Count - 1; i++)
        {
            vh.AddTriangle(0, i, i + 1);
        }
    }
}
