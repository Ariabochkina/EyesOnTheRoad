using System.Collections;
using UnityEngine;

public class RoadEventDirector : MonoBehaviour
{
    [Header("Связь")]
    [SerializeField] SplineDriver driver;
    [SerializeField] SimpleDialogueRunner dialogue;
    [SerializeField] CompanionEmotions emotions;
    [SerializeField] AudioSource radioSource;

    [Header("Радио")]
    [SerializeField] float radioDistance = 180f;
    [SerializeField] AudioClip radioClip;
    [SerializeField] float radioVolume = 0.55f;
    [SerializeField] DialogueEpisode radioEpisode;

    [Header("Человек на дороге")]
    [SerializeField] float roadsidePersonDistance = 480f;
    [SerializeField] GameObject personVisual;
    [SerializeField] float screamerSeconds = 1.8f;
    [SerializeField] float spawnAheadMeters = 22f;
    [SerializeField] float pauseAfterScreamer = 0.35f;
    [SerializeField] DialogueEpisode roadsidePersonEpisode;

    bool radioDone;
    bool personDone;
    bool loggedPersonWaiting;

    void Awake()
    {
        if (driver == null)
            driver = FindAnyObjectByType<SplineDriver>();
        if (dialogue == null)
            dialogue = FindAnyObjectByType<SimpleDialogueRunner>();
        if (emotions == null)
            emotions = FindAnyObjectByType<CompanionEmotions>();

        if (radioSource == null)
            radioSource = GetComponent<AudioSource>();

        if (radioSource != null)
        {
            radioSource.playOnAwake = false;
            radioSource.loop = true;
            radioSource.spatialBlend = 0f;
        }

        if (personVisual != null)
            personVisual.SetActive(false);
    }

    void Update()
    {
        if (driver == null || dialogue == null || dialogue.IsOpen)
            return;

        float distance = driver.DistanceAlongRoad;

        if (!radioDone && distance >= radioDistance)
            FireRadio();
        else if (!personDone && distance >= roadsidePersonDistance)
            TryFireRoadsidePerson();
    }

    void FireRadio()
    {
        radioDone = true;
        Debug.Log("Событие: радио");

        if (radioSource != null && radioClip != null)
        {
            radioSource.clip = radioClip;
            radioSource.volume = radioVolume;
            radioSource.Play();
        }

        dialogue.Play(radioEpisode);
    }

    void TryFireRoadsidePerson()
    {
        if (emotions == null || !emotions.TensionHigh)
        {
            if (!loggedPersonWaiting)
            {
                Debug.Log("Событие: человек на дороге ждёт высокого накала.");
                loggedPersonWaiting = true;
            }
            return;
        }

        personDone = true;
        Debug.Log("Событие: человек на дороге");
        StartCoroutine(RoadsidePersonRoutine());
    }

    IEnumerator RoadsidePersonRoutine()
    {
        ShowPersonAhead();
        yield return new WaitForSeconds(screamerSeconds);

        if (personVisual != null)
            personVisual.SetActive(false);

        yield return new WaitForSeconds(pauseAfterScreamer);

        while (dialogue.IsOpen)
            yield return null;

        dialogue.Play(roadsidePersonEpisode);
    }

    void ShowPersonAhead()
    {
        if (personVisual == null)
        {
            Debug.LogWarning("Событие: Person Visual не назначен.");
            return;
        }

        float at = driver.DistanceAlongRoad + spawnAheadMeters;
        if (!driver.TryEvaluateAtDistance(at, out Vector3 position, out Vector3 tangent, out Vector3 up))
            return;

        personVisual.transform.SetPositionAndRotation(position, Quaternion.LookRotation(-tangent, up));
        personVisual.SetActive(true);
    }
}
