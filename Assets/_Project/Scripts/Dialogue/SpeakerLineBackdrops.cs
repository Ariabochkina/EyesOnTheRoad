using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DefaultExecutionOrder(100)]
public class SpeakerLineBackdrops : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI speakerText;
    [SerializeField] Image lineTemplate;
    [SerializeField] Vector2 padding = new Vector2(12f, 2f);

    readonly List<Image> pool = new List<Image>();
    string lastText;
    Vector2 lastRectSize;
    float lastLineSpacing;

    void Awake()
    {
        PrepareTemplate();
        CollectExisting();
    }

    void OnEnable()
    {
        PrepareTemplate();
        CollectExisting();
        Rebuild();
    }

    void LateUpdate()
    {
        if (speakerText == null)
            return;

        Vector2 size = speakerText.rectTransform.rect.size;
        bool changed = speakerText.text != lastText
                       || !Mathf.Approximately(size.x, lastRectSize.x)
                       || !Mathf.Approximately(size.y, lastRectSize.y)
                       || !Mathf.Approximately(speakerText.lineSpacing, lastLineSpacing);

        if (changed)
            Rebuild();
    }

    public void Rebuild()
    {
        if (speakerText == null || lineTemplate == null)
            return;

        PrepareTemplate();
        CollectExisting();

        speakerText.ForceMeshUpdate(true, true);
        lastText = speakerText.text;
        lastRectSize = speakerText.rectTransform.rect.size;
        lastLineSpacing = speakerText.lineSpacing;

        TMP_TextInfo info = speakerText.textInfo;
        if (info == null)
            return;

        int lineCount = Mathf.Max(0, info.lineCount);
        var lines = new List<(float minX, float maxX, float minY, float maxY)>(lineCount);

        float sharedLeft = float.MaxValue;
        for (int i = 0; i < lineCount; i++)
        {
            TMP_LineInfo line = info.lineInfo[i];
            if (line.characterCount <= 0 || !TryGetLineBounds(info, line, out float minX, out float maxX, out float minY, out float maxY))
            {
                lines.Add((0f, 0f, 0f, 0f));
                continue;
            }

            sharedLeft = Mathf.Min(sharedLeft, minX);
            lines.Add((minX, maxX, minY, maxY));
        }

        if (sharedLeft > float.MaxValue * 0.5f)
        {
            EnsurePool(0);
            return;
        }

        EnsurePool(lineCount);

        for (int i = 0; i < pool.Count; i++)
        {
            Image img = pool[i];
            if (img == null)
                continue;

            if (i >= lineCount || lines[i].maxX <= lines[i].minX)
            {
                img.gameObject.SetActive(false);
                continue;
            }

            float maxX = lines[i].maxX;
            float minY = lines[i].minY;
            float maxY = lines[i].maxY;

            float left = sharedLeft - padding.x * 0.5f;
            float right = maxX + padding.x * 0.5f;
            float bottom = minY - padding.y * 0.5f;
            float top = maxY + padding.y * 0.5f;

            Vector2 center = new Vector2((left + right) * 0.5f, (bottom + top) * 0.5f);
            Vector2 size = new Vector2(right - left, top - bottom);

            RectTransform rt = img.rectTransform;
            img.gameObject.SetActive(true);
            img.color = lineTemplate.color;
            img.raycastTarget = false;

            rt.SetParent(speakerText.rectTransform, false);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = center;
            rt.sizeDelta = new Vector2(Mathf.Max(8f, size.x), Mathf.Max(6f, size.y));
            rt.SetSiblingIndex(i);
        }
    }

    void PrepareTemplate()
    {
        if (lineTemplate == null)
            return;

        lineTemplate.gameObject.SetActive(false);
        lineTemplate.raycastTarget = false;
    }

    void CollectExisting()
    {
        pool.Clear();
        if (speakerText == null)
            return;

        for (int i = 0; i < speakerText.transform.childCount; i++)
        {
            Transform child = speakerText.transform.GetChild(i);
            if (lineTemplate != null && child == lineTemplate.transform)
                continue;
            if (!child.name.StartsWith("LineBackdrop"))
                continue;

            Image img = child.GetComponent<Image>();
            if (img != null)
                pool.Add(img);
        }

        if (speakerText.transform.parent == null)
            return;

        Transform parent = speakerText.transform.parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == speakerText.transform)
                continue;
            if (lineTemplate != null && child == lineTemplate.transform)
                continue;
            if (!child.name.StartsWith("LineBackdrop"))
                continue;
            child.gameObject.SetActive(false);
        }
    }

    void EnsurePool(int count)
    {
        while (pool.Count < count)
        {
            Image copy = Instantiate(lineTemplate, speakerText.rectTransform);
            copy.name = $"LineBackdrop_{pool.Count}";
            copy.gameObject.SetActive(false);
            pool.Add(copy);
        }

        for (int i = count; i < pool.Count; i++)
        {
            if (pool[i] != null)
                pool[i].gameObject.SetActive(false);
        }
    }

    static bool TryGetLineBounds(
        TMP_TextInfo info,
        TMP_LineInfo line,
        out float minX,
        out float maxX,
        out float minY,
        out float maxY)
    {
        minX = float.MaxValue;
        maxX = float.MinValue;
        minY = float.MaxValue;
        maxY = float.MinValue;
        bool any = false;

        for (int c = line.firstCharacterIndex; c <= line.lastCharacterIndex; c++)
        {
            if (c < 0 || c >= info.characterInfo.Length)
                continue;

            TMP_CharacterInfo ch = info.characterInfo[c];
            if (!ch.isVisible)
                continue;

            any = true;
            minX = Mathf.Min(minX, ch.bottomLeft.x, ch.topLeft.x);
            maxX = Mathf.Max(maxX, ch.bottomRight.x, ch.topRight.x);
            minY = Mathf.Min(minY, ch.bottomLeft.y, ch.bottomRight.y);
            maxY = Mathf.Max(maxY, ch.topLeft.y, ch.topRight.y);
        }

        return any;
    }
}
