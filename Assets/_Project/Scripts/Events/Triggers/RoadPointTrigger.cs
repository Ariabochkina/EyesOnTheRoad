using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RoadPointTrigger : StoryTrigger
{
    [Tooltip("На какой дистанции по дороге срабатывает (метры). 0 — старт машины")]
    [SerializeField] float distanceMeters = 180f;

    SplineDriver driver;
    bool passed;
    float passedAt;

    public float DistanceMeters => distanceMeters;

    void Awake()
    {
        driver = FindAnyObjectByType<SplineDriver>();
    }

    void Update()
    {
        if (passed || driver == null)
            return;

        if (driver.DistanceAlongRoad < distanceMeters)
            return;

        passed = true;
        passedAt = Time.time;
    }

    public override bool TryGetReadyTime(out float readyAt)
    {
        readyAt = passedAt;
        return passed;
    }

#if UNITY_EDITOR
    public bool EditorSnapToRoad()
    {
        SplineDriver sceneDriver = FindAnyObjectByType<SplineDriver>();
        if (sceneDriver == null)
        {
            Debug.LogWarning($"{name}: в сцене нет SplineDriver (машина).");
            return false;
        }

        if (!sceneDriver.TryEvaluateAtDistance(distanceMeters, out Vector3 point, out Vector3 tangent, out Vector3 up))
        {
            Debug.LogWarning($"{name}: не получилось взять точку на дороге. Проверь, что у Car назначен Road.");
            return false;
        }

        Vector3 side = Vector3.Cross(up, tangent).normalized;
        if (side.sqrMagnitude < 0.0001f)
            side = Vector3.right;

        Undo.RecordObject(transform, "Поставить маркер на дорогу");
        transform.position = point + side * 3f + up * 1f;
        EditorUtility.SetDirty(transform);
        SceneView.lastActiveSceneView?.Frame(new Bounds(transform.position, Vector3.one * 20f), false);
        SceneView.RepaintAll();
        return true;
    }

    void OnDrawGizmos()
    {
        SplineDriver sceneDriver = driver != null ? driver : FindAnyObjectByType<SplineDriver>();
        if (sceneDriver == null ||
            !sceneDriver.TryEvaluateAtDistance(distanceMeters, out Vector3 point, out Vector3 _, out Vector3 _))
            return;

        Gizmos.color = new Color(1f, 0.45f, 0.2f);
        Gizmos.DrawWireSphere(point, 1.2f);
        Gizmos.DrawSphere(transform.position, 0.6f);
        Gizmos.DrawLine(transform.position, point);

        Handles.Label(point + Vector3.up * 2f, $"{name}\n{distanceMeters:0} м");
    }
#endif
}
