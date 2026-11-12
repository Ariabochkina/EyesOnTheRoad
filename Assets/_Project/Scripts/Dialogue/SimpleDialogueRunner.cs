using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SimpleDialogueRunner : MonoBehaviour
{
    public enum Choice
    {
        Answer1 = 0,
        Answer2 = 1,
        Answer3 = 2,
        Silence = 3
    }

    [Header("UI")]
    [SerializeField] GameObject panel;
    [SerializeField] TextMeshProUGUI speakerText;
    [SerializeField] SpeakerLineBackdrops speakerBackdrops;
    [SerializeField] DialogueStackLayout stackLayout;
    [SerializeField] Button answerButton1;
    [SerializeField] Button answerButton2;
    [SerializeField] Button answerButton3;
    [SerializeField] TextMeshProUGUI answerLabel1;
    [SerializeField] TextMeshProUGUI answerLabel2;
    [SerializeField] TextMeshProUGUI answerLabel3;
    [SerializeField] Image timerFill;

    [Header("Цвета")]
    [SerializeField] Color speakerColor = new Color(0.95f, 0.95f, 0.93f, 1f);
    [SerializeField] Color answerIdleColor = new Color(0.62f, 0.52f, 0.22f, 0.95f);
    [SerializeField] Color answerSelectedColor = new Color(0.92f, 0.82f, 0.38f, 1f);
    [SerializeField] Color numberIdleColor = new Color(0.55f, 0.48f, 0.22f, 0.9f);
    [SerializeField] Color numberSelectedColor = new Color(0.95f, 0.95f, 0.9f, 1f);

    [Header("Шрифты")]
    [SerializeField] TMP_FontAsset speakerFont;
    [SerializeField] TMP_FontAsset answerFont;

    [Header("Текст")]
    [SerializeField] float speakerFontSize = 36f;
    [SerializeField] float answerFontSize = 30f;

    [Header("Таймер")]
    [SerializeField] float replySeconds = 12f;
    [SerializeField] float startDelaySeconds = 4f;

    [Header("Тест")]
    [SerializeField] bool startAutomatically = true;
    [SerializeField] Key replayKey = Key.T;

    const string SpeakerLine = "Ночью по этой трассе редко кто едет. Ты часто так поздно за рулём?";
    const string Answer1 = "Иногда. Главное - доехать.";
    const string Answer2 = "Не люблю болтать за рулём.";
    const string Answer3 = "А ты?";

    readonly string[] answers = { Answer1, Answer2, Answer3 };
    TextMeshProUGUI[] labels;
    Button[] buttons;

    bool isOpen;
    float timeLeft;
    float startTimer;
    bool autoStartArmed;
    int selectedIndex;

    public Choice? LastChoice { get; private set; }
    public bool IsOpen => isOpen;

    void Awake()
    {
        labels = new[] { answerLabel1, answerLabel2, answerLabel3 };
        buttons = new[] { answerButton1, answerButton2, answerButton3 };

        if (stackLayout == null && panel != null)
            stackLayout = panel.GetComponent<DialogueStackLayout>();

        if (speakerBackdrops == null && panel != null)
            speakerBackdrops = panel.GetComponentInChildren<SpeakerLineBackdrops>(true);

        if (panel != null)
            panel.SetActive(false);

        ApplyTypography();
        WireButtons();
    }

    void Start()
    {
        autoStartArmed = startAutomatically;
        startTimer = startDelaySeconds;
    }

    void Update()
    {
        if (autoStartArmed)
        {
            startTimer -= Time.deltaTime;
            if (startTimer <= 0f)
            {
                autoStartArmed = false;
                Open();
            }
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[replayKey].wasPressedThisFrame && !isOpen)
            Open();

        if (!isOpen)
            return;

        timeLeft -= Time.deltaTime;
        RefreshTimerVisual();

        if (timeLeft <= 0f)
        {
            Close(Choice.Silence);
            return;
        }

        if (keyboard == null)
            return;

        if (keyboard.upArrowKey.wasPressedThisFrame)
            SetSelection(selectedIndex - 1);
        else if (keyboard.downArrowKey.wasPressedThisFrame)
            SetSelection(selectedIndex + 1);

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            SetSelection(0);
        else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            SetSelection(1);
        else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
            SetSelection(2);

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
            ConfirmSelection();
    }

    void WireButtons()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;

            int index = i;
            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() => OnAnswerClicked(index));

            EventTrigger trigger = buttons[i].gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = buttons[i].gameObject.AddComponent<EventTrigger>();

            trigger.triggers.Clear();

            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ =>
            {
                if (isOpen)
                    SetSelection(index);
            });
            trigger.triggers.Add(enter);
        }
    }

    void OnAnswerClicked(int index)
    {
        if (!isOpen)
            return;

        if (selectedIndex == index)
            ConfirmSelection();
        else
            SetSelection(index);
    }

    public void Open()
    {
        if (panel == null || speakerText == null)
            return;

        LastChoice = null;
        isOpen = true;
        selectedIndex = 0;
        timeLeft = Mathf.Max(0.1f, replySeconds);

        ApplyTypography();
        speakerText.text = SpeakerLine;
        panel.SetActive(true);

        Canvas.ForceUpdateCanvases();
        if (stackLayout != null)
            stackLayout.Rebuild();
        if (speakerBackdrops != null)
            speakerBackdrops.Rebuild();

        RefreshAnswerVisuals();
        RefreshTimerVisual();
        StartCoroutine(RebuildBackdropsNextFrame());
    }

    System.Collections.IEnumerator RebuildBackdropsNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (stackLayout != null)
            stackLayout.Rebuild();
        if (speakerBackdrops != null)
            speakerBackdrops.Rebuild();
    }

    void ConfirmSelection()
    {
        Close((Choice)selectedIndex);
    }

    void Close(Choice choice)
    {
        if (!isOpen)
            return;

        isOpen = false;
        LastChoice = choice;
        timeLeft = 0f;

        if (panel != null)
            panel.SetActive(false);
    }

    void SetSelection(int index)
    {
        selectedIndex = (index % answers.Length + answers.Length) % answers.Length;
        RefreshAnswerVisuals();
    }

    void RefreshAnswerVisuals()
    {
        for (int i = 0; i < answers.Length; i++)
        {
            if (labels[i] == null)
                continue;

            bool selected = i == selectedIndex;
            string number = (i + 1).ToString();
            string numberPart = selected
                ? $"<color=#{ColorUtility.ToHtmlStringRGBA(numberSelectedColor)}>{number}</color>"
                : $"<color=#{ColorUtility.ToHtmlStringRGBA(numberIdleColor)}>{number}</color>";
            string bodyColor = ColorUtility.ToHtmlStringRGBA(selected ? answerSelectedColor : answerIdleColor);
            string answerCaps = answers[i].ToUpper(CultureInfo.GetCultureInfo("ru-RU"));

            labels[i].richText = true;
            labels[i].alignment = TextAlignmentOptions.Left;
            labels[i].fontSize = answerFontSize;
            if (answerFont != null)
                labels[i].font = answerFont;
            labels[i].text = $"{numberPart}  <color=#{bodyColor}>{answerCaps}</color>";
            labels[i].color = Color.white;
        }
    }

    void ApplyTypography()
    {
        if (speakerText != null)
        {
            if (speakerFont != null)
                speakerText.font = speakerFont;
            speakerText.color = speakerColor;
            speakerText.alignment = TextAlignmentOptions.Left;
            speakerText.fontSize = speakerFontSize;
        }

        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] == null)
                continue;
            if (answerFont != null)
                labels[i].font = answerFont;
            labels[i].alignment = TextAlignmentOptions.Left;
            labels[i].fontSize = answerFontSize;
        }
    }

    void RefreshTimerVisual()
    {
        if (timerFill == null)
            return;

        float total = Mathf.Max(0.1f, replySeconds);
        timerFill.fillAmount = Mathf.Clamp01(timeLeft / total);
    }
}
