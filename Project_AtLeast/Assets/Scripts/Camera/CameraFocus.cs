using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFocus : MonoBehaviour
{
    public enum MotionState
    {
        Idle,
        ZoomingOut,
        Touring
    }

    public struct TourStop
    {
        public Transform building;
        public Vector3 lookCenter;
        public float radius;
        public bool clockwise;
    }

    public static CameraFocus Instance;

    [Header("Home / Overview")]
    [Tooltip("Optional. If set, zoom-out targets this pose. Otherwise the camera's starting transform is used.")]
    public Transform homeTransform;

    [Header("Zoom Out")]
    [Tooltip("Duration of the satellite pull-back from the current pose to home.")]
    public float zoomOutDuration = 2f;
    [Tooltip("Optional. If empty, uses sine ease: 0.5 - 0.5*cos(pi*t).")]
    public AnimationCurve zoomOutCurve;

    [Header("Tour — Orbit")]
    [Tooltip("Minimum horizontal orbit radius. Per-building fit distance can push this higher.")]
    public float orbitRadiusMin = 12f;
    [Tooltip("Multiplier on bounds height when fitting the building in the vertical FOV.")]
    public float fitMargin = 1.3f;
    [Tooltip("Degrees down from horizontal when looking at the building from the orbit ring.")]
    public float tourPitchDegrees = 25f;
    [Tooltip("Yaw swept during each building orbit, in degrees.")]
    public float orbitSweepDegrees = 180f;
    public float orbitDuration = 3f;
    [Tooltip("Optional. If empty, uses sine ease.")]
    public AnimationCurve orbitCurve;

    [Header("Tour — Approach")]
    [Tooltip("World units per second used to derive approach duration from distance.")]
    public float approachSpeed = 25f;
    public float approachMinDuration = 0.6f;
    public float approachMaxDuration = 3.5f;
    [Tooltip("Optional. If empty, uses sine ease.")]
    public AnimationCurve approachCurve;

    [Header("Focus On (card click)")]
    [Tooltip("Used when distance-derived approach duration would be zero.")]
    public float focusDuration = 0.8f;

    public MotionState State { get; private set; } = MotionState.Idle;

    readonly List<TourStop> _tourStops = new();
    readonly List<Renderer> _rendererBuffer = new();
    Coroutine _motion;
    float _orbitEndYaw;
    Camera _camera;

    Vector3 homePosition;
    Quaternion homeRotation;

    void Awake()
    {
        Instance = this;
        _camera = GetComponent<Camera>();

        if (homeTransform != null)
        {
            homePosition = homeTransform.position;
            homeRotation = homeTransform.rotation;
        }
        else
        {
            homePosition = transform.position;
            homeRotation = transform.rotation;
        }
    }

    /// <summary>Short move to an orbit-style look at a world point. Cancels any running motion.</summary>
    public void FocusOn(Vector3 worldPosition)
    {
        StopMotion();

        float radius = orbitRadiusMin;
        float yaw = EntryYaw(worldPosition);
        Vector3 endPos = OrbitPosition(worldPosition, yaw, radius);
        Quaternion endRot = LookAtBuilding(worldPosition, endPos);
        float duration = ApproachDuration(Vector3.Distance(transform.position, endPos));
        if (duration <= 0f)
            duration = focusDuration;

        State = MotionState.Touring;
        _motion = StartCoroutine(FocusRoutine(endPos, endRot, duration));
    }

    /// <summary>
    /// Start (or replace) an automatic tour over the given buildings, in list order.
    /// Bounds / fit radius are computed once here per stop. Loops until interrupted.
    /// Empty list is a no-op.
    /// </summary>
    public void StartTour(IReadOnlyList<Transform> buildings)
    {
        if (State == MotionState.ZoomingOut)
            return;

        StopMotion();

        _tourStops.Clear();
        if (buildings != null)
        {
            for (int i = 0; i < buildings.Count; i++)
            {
                Transform building = buildings[i];
                if (building == null)
                    continue;
                if (TryCreateStop(building, out TourStop stop))
                    _tourStops.Add(stop);
            }
        }

        if (_tourStops.Count == 0)
            return;

        _motion = StartCoroutine(TourRoutine());
    }

    public void StopTour()
    {
        if (State == MotionState.Touring)
            StopMotion();
    }

    /// <summary>
    /// Cancel any tour and pull back to the home/overview pose from the camera's
    /// current position and rotation (no snap). Repeat calls during zoom-out are ignored.
    /// </summary>
    public void ReturnHome()
    {
        if (State == MotionState.ZoomingOut || (State == MotionState.Idle && IsAtHome()))
            return;

        StopMotion();
        _motion = StartCoroutine(ZoomOutRoutine());
    }

    bool IsAtHome()
    {
        return Vector3.Distance(transform.position, homePosition) < 0.05f
            && Quaternion.Angle(transform.rotation, homeRotation) < 0.5f;
    }

    bool TryCreateStop(Transform building, out TourStop stop)
    {
        Vector3 lookCenter = building.position;
        float radius = orbitRadiusMin;

        if (TryGetRendererBounds(building, out Bounds bounds))
        {
            lookCenter = bounds.center;
            radius = FitOrbitRadius(bounds.extents.y);
        }

        var mapObj = building.GetComponent<CompanyMapObject>();
        stop = new TourStop
        {
            building = building,
            lookCenter = lookCenter,
            radius = radius,
            clockwise = mapObj == null || mapObj.orbitClockwise
        };
        return true;
    }

    float FitOrbitRadius(float extentsY)
    {
        float halfFovRad = 30f * Mathf.Deg2Rad;
        if (_camera != null)
            halfFovRad = _camera.fieldOfView * 0.5f * Mathf.Deg2Rad;

        float tanHalf = Mathf.Tan(halfFovRad);
        float fitDistance = tanHalf > 1e-6f
            ? (extentsY * fitMargin) / tanHalf
            : orbitRadiusMin;

        return Mathf.Max(orbitRadiusMin, fitDistance);
    }

    bool TryGetRendererBounds(Transform root, out Bounds bounds)
    {
        _rendererBuffer.Clear();
        root.GetComponentsInChildren(true, _rendererBuffer);

        bool found = false;
        bounds = default;
        for (int i = 0; i < _rendererBuffer.Count; i++)
        {
            Renderer r = _rendererBuffer[i];
            if (r == null)
                continue;

            if (!found)
            {
                bounds = r.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return found;
    }

    void StopMotion()
    {
        if (_motion != null)
        {
            StopCoroutine(_motion);
            _motion = null;
        }

        State = MotionState.Idle;
    }

    IEnumerator FocusRoutine(Vector3 endPos, Quaternion endRot, float duration)
    {
        yield return AnimatePose(
            transform.position, transform.rotation,
            endPos, endRot,
            duration, approachCurve);
        _motion = null;
        State = MotionState.Idle;
    }

    IEnumerator ZoomOutRoutine()
    {
        State = MotionState.ZoomingOut;
        yield return AnimatePose(
            transform.position, transform.rotation,
            homePosition, homeRotation,
            zoomOutDuration, zoomOutCurve);
        _motion = null;
        State = MotionState.Idle;
    }

    IEnumerator TourRoutine()
    {
        State = MotionState.Touring;

        int count = _tourStops.Count;
        bool single = count == 1;
        int index = 0;

        while (true)
        {
            TourStop stop = _tourStops[index];
            if (stop.building == null)
            {
                if (single)
                    yield break;

                index = (index + 1) % count;
                yield return null;
                continue;
            }

            Vector3 center = stop.lookCenter;
            float radius = stop.radius;
            float yaw = EntryYaw(center);
            Vector3 entryPos = OrbitPosition(center, yaw, radius);
            Quaternion entryRot = LookAtBuilding(center, entryPos);

            float approachDur = ApproachDuration(Vector3.Distance(transform.position, entryPos));
            yield return AnimatePose(
                transform.position, transform.rotation,
                entryPos, entryRot,
                approachDur, approachCurve);

            if (single)
            {
                while (true)
                {
                    yield return OrbitSweep(center, radius, yaw, stop.clockwise);
                    yaw = _orbitEndYaw;
                }
            }

            yield return OrbitSweep(center, radius, yaw, stop.clockwise);

            index = (index + 1) % count;
        }
    }

    IEnumerator OrbitSweep(Vector3 center, float radius, float startYaw, bool clockwise)
    {
        float signedSweepRad = (clockwise ? -1f : 1f) * orbitSweepDegrees * Mathf.Deg2Rad;
        float endYaw = startYaw + signedSweepRad;
        float duration = Mathf.Max(0.0001f, orbitDuration);

        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / duration);
            float e = EvaluateEase(t, orbitCurve);
            float yaw = Mathf.LerpUnclamped(startYaw, endYaw, e);
            Vector3 pos = OrbitPosition(center, yaw, radius);
            transform.SetPositionAndRotation(pos, LookAtBuilding(center, pos));
            yield return null;
        }

        _orbitEndYaw = endYaw;
    }

    IEnumerator AnimatePose(
        Vector3 startPos, Quaternion startRot,
        Vector3 endPos, Quaternion endRot,
        float duration, AnimationCurve curve)
    {
        if (duration <= 0f)
        {
            transform.SetPositionAndRotation(endPos, endRot);
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / duration);
            float e = EvaluateEase(t, curve);
            transform.SetPositionAndRotation(
                Vector3.LerpUnclamped(startPos, endPos, e),
                Quaternion.SlerpUnclamped(startRot, endRot, e));
            yield return null;
        }
    }

    float ApproachDuration(float distance)
    {
        if (approachSpeed <= 0f)
            return approachMaxDuration;
        return Mathf.Clamp(distance / approachSpeed, approachMinDuration, approachMaxDuration);
    }

    static float EvaluateEase(float t, AnimationCurve curve)
    {
        t = Mathf.Clamp01(t);
        if (curve != null && curve.length > 0)
            return Mathf.Clamp01(curve.Evaluate(t));
        return 0.5f - 0.5f * Mathf.Cos(Mathf.PI * t);
    }

    float EntryYaw(Vector3 center)
    {
        Vector3 flat = transform.position - center;
        flat.y = 0f;
        if (flat.sqrMagnitude < 1e-8f)
        {
            flat = -transform.forward;
            flat.y = 0f;
            if (flat.sqrMagnitude < 1e-8f)
                flat = Vector3.forward;
        }

        return Mathf.Atan2(flat.z, flat.x);
    }

    Vector3 OrbitPosition(Vector3 center, float yawRadians, float radius)
    {
        float height = radius * Mathf.Tan(tourPitchDegrees * Mathf.Deg2Rad);
        return center + new Vector3(
            Mathf.Cos(yawRadians) * radius,
            height,
            Mathf.Sin(yawRadians) * radius);
    }

    static Quaternion LookAtBuilding(Vector3 center, Vector3 cameraPos)
    {
        Vector3 to = center - cameraPos;
        if (to.sqrMagnitude < 1e-8f)
            return Quaternion.identity;
        return Quaternion.LookRotation(to, Vector3.up);
    }
}
