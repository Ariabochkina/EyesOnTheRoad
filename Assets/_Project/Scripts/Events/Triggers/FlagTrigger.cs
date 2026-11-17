using UnityEngine;

public class FlagTrigger : StoryTrigger
{
    [SerializeField] string flag;

    StoryFlags flags;

    void Awake()
    {
        flags = FindAnyObjectByType<StoryFlags>();
    }

    public override bool TryGetReadyTime(out float readyAt)
    {
        readyAt = 0f;
        return flags != null && flags.TryGetSetTime(flag, out readyAt);
    }
}
