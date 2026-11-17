using UnityEngine;

[RequireComponent(typeof(StoryEvent))]
public abstract class StoryTrigger : MonoBehaviour
{
    public virtual void Arm()
    {
    }

    public abstract bool TryGetReadyTime(out float readyAt);
}
