using System.Collections;
using UnityEngine;

[RequireComponent(typeof(StoryEvent))]
public abstract class StoryAction : MonoBehaviour
{
    [Tooltip("Ждать, пока действие закончится, прежде чем начать следующее")]
    [SerializeField] bool waitUntilDone = true;

    public bool WaitUntilDone => waitUntilDone;

    public abstract IEnumerator Run();
}
