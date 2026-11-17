using System.Collections.Generic;
using UnityEngine;

public class StoryFlags : MonoBehaviour
{
    [SerializeField] DialogueRunner dialogue;

    readonly Dictionary<string, float> setTimes = new Dictionary<string, float>();

    public IEnumerable<string> All => setTimes.Keys;

    void Awake()
    {
        if (dialogue == null)
            dialogue = FindAnyObjectByType<DialogueRunner>();
    }

    void OnEnable()
    {
        if (dialogue != null)
            dialogue.ChoiceMade += OnChoiceMade;
    }

    void OnDisable()
    {
        if (dialogue != null)
            dialogue.ChoiceMade -= OnChoiceMade;
    }

    public bool Has(string flag)
    {
        return !string.IsNullOrWhiteSpace(flag) && setTimes.ContainsKey(flag.Trim());
    }

    public bool TryGetSetTime(string flag, out float time)
    {
        time = 0f;
        return !string.IsNullOrWhiteSpace(flag) && setTimes.TryGetValue(flag.Trim(), out time);
    }

    public void Set(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag))
            return;

        flag = flag.Trim();
        if (setTimes.ContainsKey(flag))
            return;

        setTimes[flag] = Time.time;
        Debug.Log($"Флаг: {flag}");
    }

    void OnChoiceMade(DialogueLine line, DialogueRunner.Choice choice)
    {
        List<string> flags = line.FlagsFor(choice);
        if (flags == null)
            return;

        foreach (string flag in flags)
            Set(flag);
    }
}
