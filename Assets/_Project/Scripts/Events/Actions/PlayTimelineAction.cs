using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class PlayTimelineAction : StoryAction
{
    [SerializeField] PlayableDirector timeline;

    public override IEnumerator Run()
    {
        if (timeline == null)
        {
            Debug.LogWarning($"Событие {name}: Timeline не назначен.");
            yield break;
        }

        timeline.Play();
        yield return new WaitForSeconds((float)timeline.duration);
    }
}
