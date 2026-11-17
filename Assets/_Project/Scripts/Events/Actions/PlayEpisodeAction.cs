using System.Collections;
using UnityEngine;

public class PlayEpisodeAction : StoryAction
{
    [SerializeField] DialogueEpisode episode;

    DialogueRunner dialogue;

    void Awake()
    {
        dialogue = FindAnyObjectByType<DialogueRunner>();
    }

    public override IEnumerator Run()
    {
        if (episode == null || dialogue == null)
        {
            Debug.LogWarning($"Событие {name}: эпизод не назначен.");
            yield break;
        }

        while (dialogue.IsOpen)
            yield return null;

        dialogue.Play(episode);

        while (dialogue.IsOpen)
            yield return null;
    }
}
