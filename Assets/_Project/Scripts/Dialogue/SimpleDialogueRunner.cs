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

    [Header("Связь")]
    [SerializeField] CompanionEmotions emotions;

    [Header("UI")]
    [SerializeField] GameObject panel;
    [SerializeField] TextMeshProUGUI speakerText;
    [SerializeField] SpeakerLineBackdrops speakerBackdrops;
    [SerializeField] DialogueStackLayout stackLayout;
    [SerializeField] RectTransform answersRoot;
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
    [SerializeField] float remarkSeconds = 7f;

    [Header("Старт")]
    [SerializeField] DialogueEpisode introEpisode;
    [SerializeField] float startDelaySeconds = 4f;
    [SerializeField] bool startAutomatically = true;
    [SerializeField] Key replayKey = Key.T;

    TextMeshProUGUI[] labels;
    Button[] buttons;

    DialogueEpisode episode;
    DialogueLine line;
    int lineIndex;
    float lineSeconds;
    float timeLeft;
    float startTimer;
    bool autoStartArmed;
    int selectedIndex;

    public bool IsOpen => episode != null;

    public event System.Action<DialogueLine, Choice> ChoiceMade;
    public event System.Action<DialogueEpisode> EpisodeFinished;

    void Awake()
    {
        labels = new[] { answerLabel1, answerLabel2, answerLabel3 };
        buttons = new[] { answerButton1, answerButton2, answerButton3 };

        if (emotions == null)
            emotions = FindAnyObjectByType<CompanionEmotions>();

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
                Play(introEpisode);
            }
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[replayKey].wasPressedThisFrame && !IsOpen)
            Play(introEpisode);

        if (line == null)
            return;

        timeLeft -= Time.deltaTime;
        RefreshTimerVisual();

        if (timeLeft <= 0f)
        {
            if (line.HasAnswers)
                Resolve(Choice.Silence);
            else
                ShowNextLine();
            return;
        }

        if (!line.HasAnswers || keyboard == null)
            return;

        if (keyboard.upArrowKey.wasPressedThisFrame)
            SetSelection(selectedIndex - 1);
        else if (keyboard.downArrowKey.wasPressedThisFrame)
            SetSelection(selectedIndex + 1);

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            OnAnswerClicked(0);
        else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            OnAnswerClicked(1);
        else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
            OnAnswerClicked(2);

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
            Resolve((Choice)selectedIndex);
    }

    public void Play(DialogueEpisode next)
    {
        if (next == null || next.lines == null || next.lines.Count == 0)
        {
            Debug.LogWarning("Диалог: эпизод не назначен или пустой.");
            return;
        }

        if (IsOpen || panel == null || speakerText == null)
            return;

        episode = next;
        lineIndex = -1;
        ApplyTypography();
        panel.SetActive(true);
        ShowNextLine();
    }

    void ShowNextLine()
    {
        lineIndex++;
        if (lineIndex >= episode.lines.Count)
        {
            FinishEpisode();
            return;
        }

        line = episode.lines[lineIndex];
        selectedIndex = 0;
        float seconds = line.seconds > 0f ? line.seconds : (line.HasAnswers ? replySeconds : remarkSeconds);
        lineSeconds = Mathf.Max(0.1f, seconds);
        timeLeft = lineSeconds;

        CompanionEmotions.State state = emotions != null ? emotions.CurrentState : CompanionEmotions.State.Indifferent;
        speakerText.text = line.TextFor(state);
        ShowAnswers();

        RebuildLayout();
        RefreshTimerVisual();
        StartCoroutine(RebuildLayoutNextFrame());
    }

    void Resolve(Choice choice)
    {
        if (line == null)
            return;

        ChoiceMade?.Invoke(line, choice);
        ShowNextLine();
    }

    void FinishEpisode()
    {
        DialogueEpisode finished = episode;
        episode = null;
        line = null;
        timeLeft = 0f;

        if (panel != null)
            panel.SetActive(false);

        EpisodeFinished?.Invoke(finished);
    }

    void ShowAnswers()
    {
        int count = line.AnswerCount;

        if (answersRoot != null)
            answersRoot.gameObject.SetActive(count > 0);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                buttons[i].gameObject.SetActive(i < count);
        }

        if (count > 0)
            RefreshAnswerVisuals();
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
                if (line != null && index < line.AnswerCount)
                    SetSelection(index);
            });
            trigger.triggers.Add(enter);
        }
    }

    void OnAnswerClicked(int index)
    {
        if (line == null || index >= line.AnswerCount)
            return;

        if (selectedIndex == index)
            Resolve((Choice)index);
        else
            SetSelection(index);
    }

    void SetSelection(int index)
    {
        int count = line.AnswerCount;
        if (count == 0)
            return;

        selectedIndex = (index % count + count) % count;
        RefreshAnswerVisuals();
    }

    void RefreshAnswerVisuals()
    {
        for (int i = 0; i < line.AnswerCount; i++)
        {
            if (labels[i] == null)
                continue;

            bool selected = i == selectedIndex;
            string number = (i + 1).ToString();
            string numberColor = ColorUtility.ToHtmlStringRGBA(selected ? numberSelectedColor : numberIdleColor);
            string bodyColor = ColorUtility.ToHtmlStringRGBA(selected ? answerSelectedColor : answerIdleColor);
            string answerCaps = (line.answers[i].text ?? string.Empty).ToUpper(CultureInfo.GetCultureInfo("ru-RU"));

            labels[i].richText = true;
            labels[i].text = $"<color=#{numberColor}>{number}</color>  <color=#{bodyColor}>{answerCaps}</color>";
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

    void RebuildLayout()
    {
        Canvas.ForceUpdateCanvases();
        if (stackLayout != null)
            stackLayout.Rebuild();
        if (speakerBackdrops != null)
            speakerBackdrops.Rebuild();
    }

    System.Collections.IEnumerator RebuildLayoutNextFrame()
    {
        yield return null;
        RebuildLayout();
    }

    void RefreshTimerVisual()
    {
        if (timerFill == null)
            return;

        timerFill.fillAmount = Mathf.Clamp01(timeLeft / lineSeconds);
    }
}
