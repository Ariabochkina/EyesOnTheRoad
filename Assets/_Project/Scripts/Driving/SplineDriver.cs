using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

public class SplineDriver : MonoBehaviour
{
    [Header("Дорога")]
    [SerializeField] SplineContainer road;
    [SerializeField] bool loop = true;

    [Header("Скорость")]
    [SerializeField] float maxSpeed = 33.3f;
    [SerializeField] float acceleration = 3.2f;
    [SerializeField] float braking = 6.5f;
    [SerializeField] float coastDeceleration = 0f;

    [Header("Полоса")]
    [SerializeField] float maxLaneOffset = 3f;
    [SerializeField] float laneMoveSpeed = 2.2f;
    [SerializeField] float laneSmoothTime = 0.35f;

    [Header("Камера")]
    [SerializeField] Transform cabinCamera;
    [SerializeField] float bobAmplitude = 0.004f;
    [SerializeField] float bobFrequency = 0.55f;
    [SerializeField] float maxRollDegrees = 0.35f;
    [SerializeField] float maxPitchDegrees = 0.25f;
    [SerializeField] float cameraSwaySmoothTime = 0.35f;

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
    }

    void Update()
    {
        if (road == null || road.Spline == null || road.Spline.Count < 2)
            return;

        float roadLength = road.CalculateLength();
        if (roadLength < 1f)
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
