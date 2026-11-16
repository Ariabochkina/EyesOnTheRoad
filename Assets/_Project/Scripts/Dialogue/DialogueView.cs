using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TextMeshProUGUI speakerText;
    [SerializeField] SpeakerLineBackdrops speakerBackdrops;
    [SerializeField] DialogueStackLayout stackLayout;
    [SerializeField] RectTransform answersRoot;
    [SerializeField] Button[] answerButtons;
    [SerializeField] TextMeshProUGUI[] answerLabels;
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

    List<DialogueAnswer> answers;
    int answerCount;
    int selectedIndex;
    bool initialized;

    public event System.Action<int> AnswerConfirmed;

    void Awake()
    {
        Initialize();
    }

    void Initialize()
    {
        if (initialized)
            return;
        initialized = true;

        if (stackLayout == null)
            stackLayout = GetComponent<DialogueStackLayout>();
        if (speakerBackdrops == null)
            speakerBackdrops = GetComponentInChildren<SpeakerLineBackdrops>(true);

        ApplyTypography();
        WireButtons();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (answerCount == 0 || keyboard == null)
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
            Confirm(selectedIndex);
    }

    public void Show(string text, List<DialogueAnswer> lineAnswers, int count)
    {
        Initialize();
        gameObject.SetActive(true);

        answers = lineAnswers;
        answerCount = Mathf.Min(count, answerButtons.Length);
        selectedIndex = 0;

        speakerText.text = text;

        if (answersRoot != null)
            answersRoot.gameObject.SetActive(answerCount > 0);

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null)
                answerButtons[i].gameObject.SetActive(i < answerCount);
        }

        if (timerFill != null)
        {
            timerFill.gameObject.SetActive(answerCount > 0);
            timerFill.fillAmount = 1f;
        }

        RefreshAnswerVisuals();
        RebuildLayout();
        StartCoroutine(RebuildLayoutNextFrame());
    }

    public void SetTimer(float normalized)
    {
        if (timerFill != null)
            timerFill.fillAmount = Mathf.Clamp01(normalized);
    }

    public void Hide()
    {
        answerCount = 0;
        gameObject.SetActive(false);
    }

    void WireButtons()
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] == null)
                continue;

            int index = i;
            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => OnAnswerClicked(index));

            EventTrigger trigger = answerButtons[i].gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = answerButtons[i].gameObject.AddComponent<EventTrigger>();

            trigger.triggers.Clear();

            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ =>
            {
                if (index < answerCount)
                    SetSelection(index);
            });
            trigger.triggers.Add(enter);
        }
    }

    void OnAnswerClicked(int index)
    {
        if (index >= answerCount)
            return;

        if (selectedIndex == index)
            Confirm(index);
        else
            SetSelection(index);
    }

    void Confirm(int index)
    {
        if (index < answerCount)
            AnswerConfirmed?.Invoke(index);
    }

    void SetSelection(int index)
    {
        if (answerCount == 0)
            return;

        selectedIndex = (index % answerCount + answerCount) % answerCount;
        RefreshAnswerVisuals();
    }

    void RefreshAnswerVisuals()
    {
        for (int i = 0; i < answerCount; i++)
        {
            if (answerLabels[i] == null)
                continue;

            bool selected = i == selectedIndex;
            string number = (i + 1).ToString();
            string numberColor = ColorUtility.ToHtmlStringRGBA(selected ? numberSelectedColor : numberIdleColor);
            string bodyColor = ColorUtility.ToHtmlStringRGBA(selected ? answerSelectedColor : answerIdleColor);
            string answerCaps = (answers[i].text ?? string.Empty).ToUpper(CultureInfo.GetCultureInfo("ru-RU"));

            answerLabels[i].richText = true;
            answerLabels[i].text = $"<color=#{numberColor}>{number}</color>  <color=#{bodyColor}>{answerCaps}</color>";
            answerLabels[i].color = Color.white;
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

        foreach (TextMeshProUGUI label in answerLabels)
        {
            if (label == null)
                continue;
            if (answerFont != null)
                label.font = answerFont;
            label.alignment = TextAlignmentOptions.Left;
            label.fontSize = answerFontSize;
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

    IEnumerator RebuildLayoutNextFrame()
    {
        yield return null;
        RebuildLayout();
    }
}
