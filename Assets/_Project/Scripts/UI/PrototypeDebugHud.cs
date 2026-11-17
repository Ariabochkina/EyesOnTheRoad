using UnityEngine;

public class PrototypeDebugHud : MonoBehaviour
{
    [SerializeField] SplineDriver driver;
    [SerializeField] CompanionEmotions emotions;
    [SerializeField] StoryFlags flags;
    [SerializeField] bool show = true;

    void Awake()
    {
        if (driver == null)
            driver = FindAnyObjectByType<SplineDriver>();
        if (emotions == null)
            emotions = FindAnyObjectByType<CompanionEmotions>();
        if (flags == null)
            flags = FindAnyObjectByType<StoryFlags>();
    }

    void OnGUI()
    {
        if (!show)
            return;

        float kmh = driver != null ? driver.Speed * 3.6f : 0f;
        string affinity = emotions != null ? emotions.Affinity.ToString("0.#") : "—";
        string tension = emotions != null ? emotions.Tension.ToString("0.#") : "—";
        string state = emotions != null
            ? CompanionEmotions.StateLabel(emotions.CurrentState)
            : "—";
        string flagList = flags != null ? string.Join(", ", flags.All) : "";

        var style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 30,
            padding = new RectOffset(20, 20, 18, 18),
            richText = true,
            wordWrap = true
        };
        style.normal.textColor = Color.white;

        float distance = driver != null ? driver.DistanceAlongRoad : 0f;
        string text =
            $"Скорость: {kmh:0} км/ч\n" +
            $"Дистанция: {distance:0} м\n" +
            $"Расположение: {affinity}\n" +
            $"Накал: {tension}\n" +
            $"Состояние: {state}\n" +
            $"Флаги: {(flagList.Length > 0 ? flagList : "—")}\n" +
            "5–8: сменить состояние";

        const float width = 460f;
        float height = style.CalcHeight(new GUIContent(text), width);
        GUI.Box(new Rect(16f, 16f, width, height), text, style);
    }
}
