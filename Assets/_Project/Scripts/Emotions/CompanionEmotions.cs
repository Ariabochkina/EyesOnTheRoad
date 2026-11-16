using UnityEngine;
using UnityEngine.InputSystem;

public class CompanionEmotions : MonoBehaviour
{
    public enum State
    {
        QuietCloseness = 0,
        Clingy = 1,
        Indifferent = 2,
        Hostile = 3
    }

    [Header("Связь")]
    [SerializeField] DialogueRunner dialogue;

    [Header("Старт")]
    [SerializeField] float startAffinity = 50f;
    [SerializeField] float startTension = 50f;

    [Header("Пороги (высокий)")]
    [SerializeField] float highEnter = 70f;
    [SerializeField] float highExit = 55f;

    float affinity;
    float tension;
    bool affinityHigh;
    bool tensionHigh;
    State state;

    public float Affinity => affinity;
    public float Tension => tension;
    public State CurrentState => state;
    public bool AffinityHigh => affinityHigh;
    public bool TensionHigh => tensionHigh;

    void Awake()
    {
        if (dialogue == null)
            dialogue = FindAnyObjectByType<DialogueRunner>();

        ResetToStart();
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

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.digit5Key.wasPressedThisFrame || keyboard.f1Key.wasPressedThisFrame)
            ForceState(State.QuietCloseness);
        else if (keyboard.digit6Key.wasPressedThisFrame || keyboard.f2Key.wasPressedThisFrame)
            ForceState(State.Clingy);
        else if (keyboard.digit7Key.wasPressedThisFrame || keyboard.f3Key.wasPressedThisFrame)
            ForceState(State.Indifferent);
        else if (keyboard.digit8Key.wasPressedThisFrame || keyboard.f4Key.wasPressedThisFrame)
            ForceState(State.Hostile);
    }

    public void ResetToStart()
    {
        affinity = Mathf.Clamp(startAffinity, 0f, 100f);
        tension = Mathf.Clamp(startTension, 0f, 100f);
        affinityHigh = affinity >= highEnter;
        tensionHigh = tension >= highEnter;
        RefreshState(log: true);
    }

    public void ApplyDelta(float affinityDelta, float tensionDelta)
    {
        affinity = Mathf.Clamp(affinity + affinityDelta, 0f, 100f);
        tension = Mathf.Clamp(tension + tensionDelta, 0f, 100f);
        UpdateHighFlags();
        RefreshState(log: true);
    }

    public void ForceState(State forced)
    {
        switch (forced)
        {
            case State.QuietCloseness:
                affinity = 80f;
                tension = 30f;
                break;
            case State.Clingy:
                affinity = 80f;
                tension = 80f;
                break;
            case State.Indifferent:
                affinity = 30f;
                tension = 30f;
                break;
            case State.Hostile:
                affinity = 30f;
                tension = 80f;
                break;
        }

        affinityHigh = affinity >= highEnter;
        tensionHigh = tension >= highEnter;
        RefreshState(log: true);
        Debug.Log($"Эмоции: принудительно → {StateLabel(state)}");
    }

    void OnChoiceMade(DialogueLine line, DialogueRunner.Choice choice)
    {
        Vector2 delta = line.ShiftFor(choice);

        string choiceLabel = choice == DialogueRunner.Choice.Silence
            ? "молчание"
            : $"ответ {(int)choice + 1}";

        Debug.Log($"Эмоции: {choiceLabel} → Δ расположение {delta.x:+0;-0}, Δ накал {delta.y:+0;-0}");
        ApplyDelta(delta.x, delta.y);
    }

    void UpdateHighFlags()
    {
        if (affinity >= highEnter)
            affinityHigh = true;
        else if (affinity <= highExit)
            affinityHigh = false;

        if (tension >= highEnter)
            tensionHigh = true;
        else if (tension <= highExit)
            tensionHigh = false;
    }

    void RefreshState(bool log)
    {
        if (affinityHigh && !tensionHigh)
            state = State.QuietCloseness;
        else if (affinityHigh && tensionHigh)
            state = State.Clingy;
        else if (!affinityHigh && !tensionHigh)
            state = State.Indifferent;
        else
            state = State.Hostile;

        if (log)
        {
            Debug.Log(
                $"Эмоции: расположение {affinity:0.#}, накал {tension:0.#} → {StateLabel(state)}");
        }
    }

    public static string StateLabel(State value)
    {
        return value switch
        {
            State.QuietCloseness => "тихая близость",
            State.Clingy => "навязчивая привязанность",
            State.Indifferent => "безразличие",
            State.Hostile => "враждебность",
            _ => value.ToString()
        };
    }
}
