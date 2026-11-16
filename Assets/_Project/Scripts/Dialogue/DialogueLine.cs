using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public const int MaxAnswers = 3;

    [TextArea(2, 5)] public string text;

    [Header("Варианты по состоянию")]
    [TextArea(2, 5)] public string quietCloseness;
    [TextArea(2, 5)] public string clingy;
    [TextArea(2, 5)] public string indifferent;
    [TextArea(2, 5)] public string hostile;

    [Header("Ответы")]
    public List<DialogueAnswer> answers = new List<DialogueAnswer>();

    [Header("Молчание")]
    public float silenceAffinity;
    public float silenceTension;

    [Tooltip("0 - время из настроек Simple Dialogue Runner")]
    public float seconds;

    public int AnswerCount => answers == null ? 0 : Mathf.Min(answers.Count, MaxAnswers);
    public bool HasAnswers => AnswerCount > 0;

    public string TextFor(CompanionEmotions.State state)
    {
        string variant = state switch
        {
            CompanionEmotions.State.QuietCloseness => quietCloseness,
            CompanionEmotions.State.Clingy => clingy,
            CompanionEmotions.State.Indifferent => indifferent,
            CompanionEmotions.State.Hostile => hostile,
            _ => null
        };

        return string.IsNullOrWhiteSpace(variant) ? text : variant;
    }

    public Vector2 ShiftFor(DialogueRunner.Choice choice)
    {
        int index = (int)choice;
        if (choice == DialogueRunner.Choice.Silence || index >= AnswerCount)
            return new Vector2(silenceAffinity, silenceTension);

        DialogueAnswer answer = answers[index];
        return new Vector2(answer.affinity, answer.tension);
    }
}

[System.Serializable]
public class DialogueAnswer
{
    public string text;
    public float affinity;
    public float tension;
}
