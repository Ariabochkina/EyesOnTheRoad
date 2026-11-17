using UnityEngine;

public class DialogueRunner : MonoBehaviour
{
    public enum Choice
    {
        Answer1 = 0,
        Answer2 = 1,
        Answer3 = 2,
        Silence = 3
    }

    [Header("Связь")]
    [SerializeField] CompanionEmotions emotions;
    [SerializeField] DialogueView view;

    [Header("Время реплики")]
    [SerializeField] float replySeconds = 12f;
    [SerializeField] float remarkSeconds = 7f;

    DialogueEpisode episode;
    DialogueLine line;
    int lineIndex;
    float lineSeconds;
    float timeLeft;

    public bool IsOpen => episode != null;

    public event System.Action<DialogueLine, Choice> ChoiceMade;
    public event System.Action<DialogueEpisode> EpisodeFinished;

    void Awake()
    {
        if (emotions == null)
            emotions = FindAnyObjectByType<CompanionEmotions>();
        if (view == null)
            view = FindAnyObjectByType<DialogueView>(FindObjectsInactive.Include);

        if (view != null)
        {
            view.AnswerConfirmed += OnAnswerConfirmed;
            view.Hide();
        }
    }

    void OnDestroy()
    {
        if (view != null)
            view.AnswerConfirmed -= OnAnswerConfirmed;
    }

    void Update()
    {
        if (line == null)
            return;

        timeLeft -= Time.deltaTime;
        view.SetTimer(timeLeft / lineSeconds);

        if (timeLeft > 0f)
            return;

        if (line.HasAnswers)
            Resolve(Choice.Silence);
        else
            ShowNextLine();
    }

    public void Play(DialogueEpisode next)
    {
        if (next == null || next.lines == null || next.lines.Count == 0)
        {
            Debug.LogWarning("Диалог: эпизод не назначен или пустой.");
            return;
        }

        if (IsOpen || view == null)
            return;

        episode = next;
        lineIndex = -1;
        ShowNextLine();
    }

    void ShowNextLine()
    {
        lineIndex++;
        if (lineIndex >= episode.lines.Count)
        {
            FinishEpisode();
            return;
        }

        line = episode.lines[lineIndex];
        float seconds = line.seconds > 0f ? line.seconds : (line.HasAnswers ? replySeconds : remarkSeconds);
        lineSeconds = Mathf.Max(0.1f, seconds);
        timeLeft = lineSeconds;

        CompanionEmotions.State state = emotions != null ? emotions.CurrentState : CompanionEmotions.State.Indifferent;
        view.Show(line.TextFor(state), line.answers, line.AnswerCount);
    }

    void OnAnswerConfirmed(int index)
    {
        if (line == null || index >= line.AnswerCount)
            return;

        Resolve((Choice)index);
    }

    void Resolve(Choice choice)
    {
        ChoiceMade?.Invoke(line, choice);
        ShowNextLine();
    }

    void FinishEpisode()
    {
        DialogueEpisode finished = episode;
        episode = null;
        line = null;
        view.Hide();
        EpisodeFinished?.Invoke(finished);
    }
}
