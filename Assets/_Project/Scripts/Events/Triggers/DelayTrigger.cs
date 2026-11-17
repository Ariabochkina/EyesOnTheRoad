using UnityEngine;

public class DelayTrigger : StoryTrigger
{
    [Tooltip("Секунды с момента, когда до события дошла очередь (в Story — после конца предыдущего шага)")]
    [SerializeField] float seconds = 5f;

    float armedAt = float.PositiveInfinity;

    public override void Arm()
    {
        armedAt = Time.time;
    }

    public override bool TryGetReadyTime(out float readyAt)
    {
        readyAt = armedAt + seconds;
        return Time.time >= readyAt;
    }
}
