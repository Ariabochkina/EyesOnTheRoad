using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventDirector : MonoBehaviour
{
    [Header("Связь")]
    [SerializeField] Transform story;
    [SerializeField] Transform ambient;
    [SerializeField] DialogueRunner dialogue;
    [SerializeField] CompanionEmotions emotions;
    [SerializeField] StoryFlags flags;

    [Header("Побочные события")]
    [Tooltip("Минимум секунд тишины перед побочным событием")]
    [SerializeField] float ambientGap = 8f;

    readonly List<StoryEvent> ambientEvents = new List<StoryEvent>();
    readonly HashSet<StoryEvent> approved = new HashSet<StoryEvent>();
    readonly HashSet<StoryEvent> finished = new HashSet<StoryEvent>();

    bool running;
    bool storyWaiting;
    float lastFinishedAt = float.NegativeInfinity;

    public bool Busy => running || (dialogue != null && dialogue.IsOpen);

    void Awake()
    {
        if (dialogue == null)
            dialogue = FindAnyObjectByType<DialogueRunner>();
        if (emotions == null)
            emotions = FindAnyObjectByType<CompanionEmotions>();
        if (flags == null)
            flags = FindAnyObjectByType<StoryFlags>();
    }

    void Start()
    {
        if (ambient != null)
        {
            foreach (StoryEvent ev in ambient.GetComponentsInChildren<StoryEvent>())
            {
                ambientEvents.Add(ev);
                ev.Arm();
            }
        }

        if (story != null)
            StartCoroutine(RunStory());
    }

    void Update()
    {
        foreach (StoryEvent ev in ambientEvents)
        {
            if (finished.Contains(ev) || !ev.TryGetReadyTime(out float readyAt))
                continue;

            if (!approved.Contains(ev))
            {
                if (!ev.ConditionMet(emotions, flags))
                {
                    Skip(ev, "условие не выполнено");
                    continue;
                }
                approved.Add(ev);
            }

            if (ev.IsExpired(readyAt))
            {
                Skip(ev, "слишком долго ждало");
                continue;
            }

            if (Busy || storyWaiting || Time.time - lastFinishedAt < ambientGap)
                continue;

            finished.Add(ev);
            StartCoroutine(Run(ev));
            break;
        }
    }

    IEnumerator RunStory()
    {
        for (int i = 0; i < story.childCount; i++)
        {
            Transform child = story.GetChild(i);
            if (!child.gameObject.activeInHierarchy || !child.TryGetComponent(out StoryEvent step))
                continue;

            yield return WaitAndRun(step);
        }

        Debug.Log("Story: все шаги пройдены");
    }

    IEnumerator WaitAndRun(StoryEvent step)
    {
        step.Arm();

        float readyAt;
        while (!step.TryGetReadyTime(out readyAt))
            yield return null;

        if (!step.ConditionMet(emotions, flags))
        {
            Skip(step, "условие не выполнено");
            yield break;
        }

        storyWaiting = true;
        while (Busy)
        {
            if (step.IsExpired(readyAt))
            {
                storyWaiting = false;
                Skip(step, "слишком долго ждал");
                yield break;
            }
            yield return null;
        }
        storyWaiting = false;

        yield return Run(step);
    }

    IEnumerator Run(StoryEvent ev)
    {
        running = true;
        Debug.Log($"Событие: {ev.name}");

        foreach (StoryAction action in ev.Actions)
        {
            if (action.WaitUntilDone)
                yield return StartCoroutine(action.Run());
            else
                StartCoroutine(action.Run());
        }

        running = false;
        lastFinishedAt = Time.time;
    }

    void Skip(StoryEvent ev, string reason)
    {
        finished.Add(ev);
        Debug.Log($"Событие {ev.name}: пропущено ({reason})");
    }
}
