using System.Collections;
using UnityEngine;

public class PlaySoundAction : StoryAction
{
    public enum Mode
    {
        [InspectorName("Включить")] Play,
        [InspectorName("Выключить")] Stop
    }

    [SerializeField] AudioSource source;
    [SerializeField] Mode mode = Mode.Play;

    public override IEnumerator Run()
    {
        if (source == null)
        {
            Debug.LogWarning($"Событие {name}: Audio Source не назначен.");
            yield break;
        }

        if (mode == Mode.Stop)
        {
            source.Stop();
            yield break;
        }

        source.Play();

        if (source.loop)
            yield break;

        while (source.isPlaying)
            yield return null;
    }
}
