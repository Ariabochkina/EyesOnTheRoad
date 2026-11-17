using UnityEngine;

public class StoryEvent : MonoBehaviour
{
    public enum Mood
    {
        [InspectorName("Любое")] Any,
        [InspectorName("Высокий накал")] TensionHigh,
        [InspectorName("Низкий накал")] TensionLow,
        [InspectorName("Высокое расположение")] AffinityHigh,
        [InspectorName("Низкое расположение")] AffinityLow,
        [InspectorName("Тихая близость")] QuietCloseness,
        [InspectorName("Навязчивая привязанность")] Clingy,
        [InspectorName("Безразличие")] Indifferent,
        [InspectorName("Враждебность")] Hostile
    }

    [Header("Условие")]
    [SerializeField] Mood mood = Mood.Any;
    [SerializeField] string requiredFlag;
    [SerializeField] string blockedFlag;

    [Header("Очередь")]
    [Tooltip("Сколько секунд событие может ждать, пока занято другим. 0 — ждать сколько угодно")]
    [SerializeField] float maxWait;

    StoryTrigger[] triggers;
    StoryAction[] actions;
    float armedAt;

    public StoryAction[] Actions => actions;

    void Awake()
    {
        triggers = GetComponents<StoryTrigger>();
        actions = GetComponents<StoryAction>();
    }

    public void Arm()
    {
        armedAt = Time.time;
        foreach (StoryTrigger trigger in triggers)
            trigger.Arm();
    }

    public bool TryGetReadyTime(out float readyAt)
    {
        if (triggers.Length == 0)
        {
            readyAt = armedAt;
            return true;
        }

        readyAt = float.MinValue;
        foreach (StoryTrigger trigger in triggers)
        {
            if (!trigger.TryGetReadyTime(out float at))
                return false;
            readyAt = Mathf.Max(readyAt, at);
        }
        return true;
    }

    public bool IsExpired(float readyAt)
    {
        return maxWait > 0f && Time.time - readyAt > maxWait;
    }

    public bool ConditionMet(CompanionEmotions emotions, StoryFlags flags)
    {
        if (!string.IsNullOrWhiteSpace(requiredFlag) && (flags == null || !flags.Has(requiredFlag)))
            return false;
        if (!string.IsNullOrWhiteSpace(blockedFlag) && flags != null && flags.Has(blockedFlag))
            return false;

        if (mood == Mood.Any)
            return true;
        if (emotions == null)
            return false;

        switch (mood)
        {
            case Mood.TensionHigh: return emotions.TensionHigh;
            case Mood.TensionLow: return !emotions.TensionHigh;
            case Mood.AffinityHigh: return emotions.AffinityHigh;
            case Mood.AffinityLow: return !emotions.AffinityHigh;
            case Mood.QuietCloseness: return emotions.CurrentState == CompanionEmotions.State.QuietCloseness;
            case Mood.Clingy: return emotions.CurrentState == CompanionEmotions.State.Clingy;
            case Mood.Indifferent: return emotions.CurrentState == CompanionEmotions.State.Indifferent;
            case Mood.Hostile: return emotions.CurrentState == CompanionEmotions.State.Hostile;
            default: return true;
        }
    }
}
