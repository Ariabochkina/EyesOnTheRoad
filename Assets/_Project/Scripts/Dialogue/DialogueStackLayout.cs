using TMPro;
using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(50)]
public class DialogueStackLayout : MonoBehaviour
{
    [SerializeField] RectTransform panel;
    [SerializeField] TextMeshProUGUI speakerText;
    [SerializeField] RectTransform speakerBlock;
    [SerializeField] RectTransform answersRoot;
    [SerializeField] RectTransform timerFill;

    [Header("Отступы")]
    [SerializeField] float itemSpacing = 10f;
    [SerializeField] float gapBeforeTimer = 10f;
    [SerializeField] float bottomMargin = 90f;
    [SerializeField] float stackWidth = 760f;
    [SerializeField] float answerHeight = 36f;
    [SerializeField] float speakerExtraHeight = 4f;

    [Header("Таймер")]
    [SerializeField] float timerWidth = 420f;
    [SerializeField] float timerHeight = 5f;

    void LateUpdate()
    {
        Rebuild();
    }

    public void Rebuild()
    {
        if (panel == null || speakerText == null || speakerBlock == null || answersRoot == null)
            return;

        speakerText.alignment = TextAlignmentOptions.Left;
        speakerText.margin = Vector4.zero;

        float fontSize = Mathf.Max(1f, speakerText.fontSize);
        float naturalGap = fontSize * 0.2f;
        float extraPixels = Mathf.Max(0f, itemSpacing - naturalGap);
        speakerText.lineSpacing = (extraPixels / fontSize) * 100f;

        speakerText.ForceMeshUpdate(true, true);

        float speakerH = Mathf.Max(speakerText.preferredHeight + speakerExtraHeight, speakerText.fontSize * 1.2f);

        int answerCount = 0;
        for (int i = 0; i < answersRoot.childCount; i++)
        {
            if (answersRoot.GetChild(i).gameObject.activeSelf)
                answerCount++;
        }

        if (answerCount < 1)
            answerCount = 3;

        float answersH = answerCount * answerHeight + (answerCount - 1) * itemSpacing;
        float timerH = timerFill != null ? Mathf.Max(1f, timerHeight) : 0f;
        float timerW = Mathf.Max(1f, timerWidth);
        float timerGap = timerFill != null ? gapBeforeTimer : 0f;

        float y = bottomMargin;

        if (timerFill != null)
        {
            timerFill.anchorMin = new Vector2(0.5f, 0f);
            timerFill.anchorMax = new Vector2(0.5f, 0f);
            timerFill.pivot = new Vector2(0.5f, 0f);
            timerFill.anchoredPosition = new Vector2(0f, y);
            timerFill.sizeDelta = new Vector2(timerW, timerH);
            y += timerH + timerGap;
        }

        answersRoot.anchorMin = new Vector2(0.5f, 0f);
        answersRoot.anchorMax = new Vector2(0.5f, 0f);
        answersRoot.pivot = new Vector2(0.5f, 0f);
        answersRoot.sizeDelta = new Vector2(stackWidth, answersH);
        answersRoot.anchoredPosition = new Vector2(0f, y);

        int visible = 0;
        for (int i = 0; i < answersRoot.childCount; i++)
        {
            RectTransform child = answersRoot.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf)
                continue;

            child.anchorMin = new Vector2(0.5f, 1f);
            child.anchorMax = new Vector2(0.5f, 1f);
            child.pivot = new Vector2(0.5f, 1f);
            child.sizeDelta = new Vector2(stackWidth, answerHeight);
            child.anchoredPosition = new Vector2(0f, -visible * (answerHeight + itemSpacing));
            visible++;
        }

        y += answersH + itemSpacing;

        speakerBlock.anchorMin = new Vector2(0.5f, 0f);
        speakerBlock.anchorMax = new Vector2(0.5f, 0f);
        speakerBlock.pivot = new Vector2(0.5f, 0f);
        speakerBlock.sizeDelta = new Vector2(stackWidth, speakerH);
        speakerBlock.anchoredPosition = new Vector2(0f, y);

        RectTransform speakerRt = speakerText.rectTransform;
        speakerRt.anchorMin = Vector2.zero;
        speakerRt.anchorMax = Vector2.one;
        speakerRt.offsetMin = Vector2.zero;
        speakerRt.offsetMax = Vector2.zero;
    }
}
