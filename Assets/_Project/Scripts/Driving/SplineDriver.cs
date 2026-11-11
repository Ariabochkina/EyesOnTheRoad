using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

public class SplineDriver : MonoBehaviour
{
    [Header("Дорога")]
    [Tooltip("Объект со компонентом Spline Container. Перетащи его сюда из окна Hierarchy.")]
    [SerializeField] SplineContainer road;

    [Tooltip("Для пробного круга: в конце пути машина снова оказывается в начале. На настоящей трассе курсовой галочку снять.")]
    [SerializeField] bool loop = true;

    [Header("Скорость")]
    [Tooltip("Максимум в м/с. 33.3 ≈ 120 км/ч.")]
    [SerializeField] float maxSpeed = 33.3f;

    [Tooltip("Ускорение в м/с². Около 0.3 g — разгон легковушки без гонки.")]
    [SerializeField] float acceleration = 3.2f;

    [Tooltip("Торможение в м/с² при зажатой S. Около 0.65 g.")]
    [SerializeField] float braking = 6.5f;

    [Tooltip("Замедление без газа и тормоза. 0 = скорость держится, как раньше. Больше нуля — сама ползёт вниз.")]
    [SerializeField] float coastDeceleration = 0f;

    [Header("Полоса")]
    [Tooltip("Максимальный съезд влево и вправо от линии сплайна, в метрах.")]
    [SerializeField] float maxLaneOffset = 3f;

    [Tooltip("Скорость перестроения на месте, м/с. На высокой скорости скрипт сам чуть снижает отзыв.")]
    [SerializeField] float laneMoveSpeed = 2.2f;

    [Tooltip("Сглаживание перестроения в секундах. Больше — мягче.")]
    [SerializeField] float laneSmoothTime = 0.35f;

    [Header("Камера")]
    [Tooltip("Камера внутри или над кубом. Она должна быть дочерней к этому объекту. Пустое поле скрипт попробует заполнить сам.")]
    [SerializeField] Transform cabinCamera;

    [Tooltip("Вертикальное покачивание в метрах.")]
    [SerializeField] float bobAmplitude = 0.004f;

    [Tooltip("Частота покачивания в герцах на полной скорости.")]
    [SerializeField] float bobFrequency = 0.55f;

    [Tooltip("Максимальный наклон камеры при уходе в сторону, в градусах.")]
    [SerializeField] float maxRollDegrees = 0.35f;

    [Tooltip("Максимальный кивок камеры при газе и торможении, в градусах.")]
    [SerializeField] float maxPitchDegrees = 0.25f;

    [Tooltip("Насколько быстро камера догоняет нужный наклон, в секундах.")]
    [SerializeField] float cameraSwaySmoothTime = 0.35f;

    [Header("Отладка")]
    [Tooltip("Надпись скорости в углу Game. К игроку в курсовой это не относится.")]
    [SerializeField] bool showSpeed = true;

    float speed;
    float distanceAlongRoad;
    float laneOffset;
    float laneOffsetTarget;
    float laneSmoothVelocity;
    float previousSpeed;
    float previousLaneOffset;
    float bobTime;
    float cameraRoll;
    float cameraPitch;
    float cameraRollVelocity;
    float cameraPitchVelocity;

    Vector3 cameraRestPosition;
    Quaternion cameraRestRotation;
    bool cameraRestCached;

    bool missingRoadWarningShown;
    bool openSplineWarningShown;
    bool scaleWarningShown;

    GUIStyle speedStyle;

    public float Speed => speed;
    public float DistanceAlongRoad => distanceAlongRoad;
    public float LaneOffset => laneOffset;

    void Awake()
    {
        if (cabinCamera == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>();
            if (childCamera != null && childCamera.transform != transform)
                cabinCamera = childCamera.transform;
        }
    }

    void Start()
    {
        CacheCameraRest();

        if (TryGetComponent<Rigidbody>(out _))
        {
            Debug.LogWarning(
                "SplineDriver: на кубе есть Rigidbody. Удали этот компонент. Машина едет скриптом, без физики колёс.",
                this);
        }
    }

    void Update()
    {
        if (!TryGetRoad(out float roadLength))
            return;

        ReadDrivingInput(out float throttle, out float laneAxis);
        UpdateSpeed(throttle);
        UpdateLane(laneAxis);
        distanceAlongRoad = AdvanceDistance(distanceAlongRoad, roadLength);
        ApplyPose();
    }

    void LateUpdate()
    {
        ApplyCameraSway();
        previousSpeed = speed;
        previousLaneOffset = laneOffset;
    }

    void OnGUI()
    {
        if (!showSpeed)
            return;

        if (speedStyle == null)
        {
            speedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18
            };
            speedStyle.normal.textColor = Color.white;
        }

        float kmh = speed * 3.6f;
        GUI.Label(new Rect(16f, 16f, 520f, 32f), $"Скорость: {kmh:0} км/ч    путь: {distanceAlongRoad:0} м", speedStyle);
    }

    bool TryGetRoad(out float roadLength)
    {
        roadLength = 0f;

        if (road == null || road.Spline == null || road.Spline.Count < 2)
        {
            WarnOnce(
                ref missingRoadWarningShown,
                "SplineDriver: в поле Road ничего нет, или на сплайне меньше двух узлов. Перетащи объект Road в это поле.");
            return false;
        }

        Vector3 scale = road.transform.lossyScale;
        if (Mathf.Abs(scale.x - 1f) > 0.02f || Mathf.Abs(scale.y - 1f) > 0.02f || Mathf.Abs(scale.z - 1f) > 0.02f)
        {
            WarnOnce(
                ref scaleWarningShown,
                "SplineDriver: у объекта Road масштаб должен остаться 1, 1, 1. Иначе скорость и ширина дороги разъедутся.");
        }

        roadLength = road.CalculateLength();
        if (roadLength < 1f)
        {
            WarnOnce(
                ref missingRoadWarningShown,
                "SplineDriver: сплайн короче метра. Разведи узлы на сотни метров, иначе круга не получится.");
            return false;
        }

        if (loop && !road.Spline.Closed)
        {
            WarnOnce(
                ref openSplineWarningShown,
                "SplineDriver: сплайн не замкнут. На стыке конца и начала машину перенесёт в начало. У Spline Container включи Closed.");
        }

        return true;
    }

    static void WarnOnce(ref bool alreadyShown, string message)
    {
        if (alreadyShown)
            return;

        alreadyShown = true;
        Debug.LogWarning(message);
    }

    void ReadDrivingInput(out float throttle, out float laneAxis)
    {
        throttle = 0f;
        laneAxis = 0f;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.wKey.isPressed)
            throttle += 1f;
        if (keyboard.sKey.isPressed)
            throttle -= 1f;
        if (keyboard.dKey.isPressed)
            laneAxis += 1f;
        if (keyboard.aKey.isPressed)
            laneAxis -= 1f;
    }

    void UpdateSpeed(float throttle)
    {
        if (throttle > 0f)
            speed += acceleration * Time.deltaTime;
        else if (throttle < 0f)
            speed -= braking * Time.deltaTime;
        else
            speed -= coastDeceleration * Time.deltaTime;

        speed = Mathf.Clamp(speed, 0f, Mathf.Max(0f, maxSpeed));
    }

    void UpdateLane(float laneAxis)
    {
        float limit = Mathf.Max(0f, maxLaneOffset);
        float speed01 = maxSpeed > 0.01f ? Mathf.Clamp01(speed / maxSpeed) : 0f;
        float response = Mathf.Lerp(1f, 0.75f, speed01);
        laneOffsetTarget += laneAxis * laneMoveSpeed * response * Time.deltaTime;
        laneOffsetTarget = Mathf.Clamp(laneOffsetTarget, -limit, limit);
        laneOffset = Mathf.SmoothDamp(
            laneOffset,
            laneOffsetTarget,
            ref laneSmoothVelocity,
            Mathf.Max(0.01f, laneSmoothTime));
    }

    float AdvanceDistance(float current, float roadLength)
    {
        current += speed * Time.deltaTime;
        if (!loop)
            return Mathf.Min(current, roadLength);

        return Mathf.Repeat(current, roadLength);
    }

    void ApplyPose()
    {
        float t = SplineUtility.ConvertIndexUnit(
            road.Spline,
            distanceAlongRoad,
            PathIndexUnit.Distance,
            PathIndexUnit.Normalized);

        if (float.IsNaN(t) || float.IsInfinity(t))
            return;

        if (!road.Evaluate(t, out float3 worldPosition, out float3 worldTangent, out float3 worldUp))
            return;

        Vector3 forward = worldTangent;
        if (forward.sqrMagnitude < 0.0001f)
            return;

        forward.Normalize();
        Vector3 up = Vector3.ProjectOnPlane(worldUp, forward);
        if (up.sqrMagnitude < 0.0001f)
            up = Vector3.up;
        up.Normalize();

        Vector3 right = Vector3.Cross(up, forward);
        if (right.sqrMagnitude < 0.0001f)
            return;

        right.Normalize();
        transform.SetPositionAndRotation((Vector3)worldPosition + right * laneOffset, Quaternion.LookRotation(forward, up));
    }

    void CacheCameraRest()
    {
        if (cabinCamera == null)
            return;

        cameraRestPosition = cabinCamera.localPosition;
        cameraRestRotation = cabinCamera.localRotation;
        cameraRestCached = true;
    }

    void ApplyCameraSway()
    {
        if (!cameraRestCached || cabinCamera == null)
            return;

        float dt = Time.deltaTime;
        float speed01 = maxSpeed > 0.01f ? Mathf.Clamp01(speed / maxSpeed) : 0f;
        bobTime += dt * Mathf.Lerp(0.25f, 0.85f, speed01);

        float bob = Mathf.Sin(bobTime * bobFrequency * (Mathf.PI * 2f)) * bobAmplitude * speed01;
        float lateralSpeed = dt > 0.0001f ? (laneOffset - previousLaneOffset) / dt : 0f;
        float accelerationNow = dt > 0.0001f ? (speed - previousSpeed) / dt : 0f;

        float targetRoll = Mathf.Clamp(-lateralSpeed * 0.18f, -maxRollDegrees, maxRollDegrees);
        float targetPitch = Mathf.Clamp(-accelerationNow * 0.03f, -maxPitchDegrees, maxPitchDegrees);
        float smooth = Mathf.Max(0.05f, cameraSwaySmoothTime);

        cameraRoll = Mathf.SmoothDamp(cameraRoll, targetRoll, ref cameraRollVelocity, smooth);
        cameraPitch = Mathf.SmoothDamp(cameraPitch, targetPitch, ref cameraPitchVelocity, smooth);

        cabinCamera.localPosition = cameraRestPosition + new Vector3(0f, bob, 0f);
        cabinCamera.localRotation = cameraRestRotation * Quaternion.Euler(cameraPitch, 0f, cameraRoll);
    }
}
