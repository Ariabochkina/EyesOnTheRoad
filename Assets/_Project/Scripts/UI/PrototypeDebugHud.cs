using UnityEngine;

public class PrototypeDebugHud : MonoBehaviour
{
    [SerializeField] SplineDriver driver;
    [SerializeField] CompanionEmotions emotions;
    [SerializeField] bool show = true;

    void Awake()
    {
        if (driver == null)
            driver = FindAnyObjectByType<SplineDriver>();
        if (emotions == null)
            emotions = FindAnyObjectByType<CompanionEmotions>();
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

        var style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 30,
            padding = new RectOffset(20, 20, 18, 18),
            richText = true
        };
        style.normal.textColor = Color.white;

        string text =
            $"Скорость: {kmh:0} км/ч\n" +
            $"Расположение: {affinity}\n" +
            $"Накал: {tension}\n" +
            $"Состояние: {state}\n" +
            "5–8: сменить состояние";

        GUI.Box(new Rect(16f, 16f, 440f, 220f), text, style);
    }
}
