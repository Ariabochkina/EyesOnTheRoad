using System.Collections;
using UnityEngine;

public class SetFlagAction : StoryAction
{
    [SerializeField] string flag;

    StoryFlags flags;

    void Awake()
    {
        flags = FindAnyObjectByType<StoryFlags>();
    }

    public override IEnumerator Run()
    {
        if (flags != null)
            flags.Set(flag);
        yield break;
    }
}
