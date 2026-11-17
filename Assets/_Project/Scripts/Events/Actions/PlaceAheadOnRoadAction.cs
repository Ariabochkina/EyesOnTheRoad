using System.Collections;
using UnityEngine;

public class PlaceAheadOnRoadAction : StoryAction
{
    [SerializeField] GameObject target;
    [SerializeField] float aheadMeters = 18f;
    [SerializeField] float sideMeters;

    SplineDriver driver;

    void Awake()
    {
        driver = FindAnyObjectByType<SplineDriver>();
    }

    public override IEnumerator Run()
    {
        if (target == null || driver == null)
        {
            Debug.LogWarning($"Событие {name}: нечего ставить перед машиной.");
            yield break;
        }

        float at = driver.DistanceAlongRoad + aheadMeters;
        if (!driver.TryEvaluateAtDistance(at, out Vector3 position, out Vector3 tangent, out Vector3 up))
            yield break;

        Vector3 side = Vector3.Cross(up, tangent).normalized;
        target.transform.SetPositionAndRotation(
            position + side * sideMeters,
            Quaternion.LookRotation(-tangent, up));
    }
}
