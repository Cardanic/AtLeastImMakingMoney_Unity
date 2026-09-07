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
        public CompanyMapObject mapObject;
        public Vector3 lookCenter;
        public float radius;
        public Bounds bounds;
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

    [Header("Tour")]
    [Tooltip("Seconds to wait after StartTour before the camera begins moving.")]
    public float tourStartDelay = 0f;

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
    [Tooltip("Seconds to wait before starting the focus move (zoom-in).")]
    public float focusStartDelay = 0f;

    [Header("Detection Outputs")]
    [Tooltip("Fraction of an approach, measured back from its end, over which TargetLock ramps 0 -> 1.")]
    [Range(0.01f, 0.9f)]
    public float approachLockFraction = 0.3f;
    [Tooltip("Fraction of a departure, measured from its start, over which TargetLock ramps 1 -> 0.")]
    [Range(0.01f, 0.9f)]
    public float departureUnlockFraction = 0.3f;
    [Tooltip("Exponential smoothing time for CameraSpeed, in seconds. 0 = unsmoothed.")]
    public float cameraSpeedSmoothing = 0.1f;

    /// <summary>
    /// 0..1. Ramps 0 -> 1 over the last <see cref="approachLockFraction"/> of an approach,
    /// holds 1 during orbit, ramps 1 -> 0 over the first <see cref="departureUnlockFraction"/>
    /// of a departure. 0 whenever not touring a building.
    /// </summary>
    public float TargetLock { get; private set; }

    /// <summary>Magnitude of the camera position delta per second, smoothed over <see cref="cameraSpeedSmoothing"/>.</summary>
    public float CameraSpeed { get; private set; }

    /// <summary>0..1 progress through the current orbit sweep. 0 when not orbiting.</summary>
    public float OrbitPhase { get; private set; }

    /// <summary>Building currently being toured (locked, or mid lock/unlock). Null when not touring.</summary>
    public CompanyMapObject CurrentTarget { get; private set; }

    /// <summary>Cached world-space renderer bounds for <see cref="CurrentTarget"/>.</summary>
    public Bounds CurrentTargetBounds { get; private set; }

    [Header("Detection HUD (glitch/tear feel)")]
    [Tooltip("Single accent colour, used by the HUD's target panel and inset frame.")]
    public Color accentColor = new Color(1f, 0.16470589f, 0.10196079f, 1f); // #FF2A1A
    [Tooltip("Seconds between tear re-rolls.")]
    public float tearInterval = 0.12f;
    [Tooltip("Peak UV offset of a torn band on the inset camera feed.")]
    public float tearAmount = 0.08f;
    [Tooltip("Rows the inset camera feed is split into for the tear.")]
    public float tearRows = 32f;
    [Tooltip("Roughly how many rows/strips tear per interval.")]
    public float tearBandsPerInterval = 3f;
    [Tooltip("Camera speed that maps to a full tear. 0 = auto-measure at mid-approach.")]
    public float tearSpeedRef = 0f;

    /// <summary>Current tear intensity: TargetLock * (0.3 + 0.7 * saturate(CameraSpeed / tearSpeedRef)).</summary>
    public float TearIntensity { get; private set; }
    public float TearRows => tearRows;
    public float TearInterval => tearInterval;
    public float TearAmount => tearAmount;
    public float TearBandsPerInterval => tearBandsPerInterval;
    public Color AccentColor => accentColor;

    public MotionState State { get; private set; } = MotionState.Idle;

    readonly List<TourStop> _tourStops = new();
    readonly List<Renderer> _rendererBuffer = new();
    float _tearSpeedRuntime;
    bool _tearSpeedCaptured;
    Coroutine _motion;
    float _orbitEndYaw;
    Camera _camera;

    Vector3 _lastTrackedPos;
    bool _hasLastTrackedPos;

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
        _tearSpeedCaptured = false;
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
        Bounds bounds = new Bounds(building.position, Vector3.one);

        if (TryGetRendererBounds(building, out Bounds rendererBounds))
        {
            bounds = rendererBounds;
            lookCenter = rendererBounds.center;
            radius = FitOrbitRadius(rendererBounds.extents.y);
        }

        var mapObj = building.GetComponent<CompanyMapObject>();
        stop = new TourStop
        {
            building = building,
            mapObject = mapObj,
            lookCenter = lookCenter,
            radius = radius,
            bounds = bounds,
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

            // Floating name labels (TextMeshPro + its sub-meshes) sit well above the roof and
            // would balloon the bounds; the frame and orbit fit should track the building only.
            if (r.GetComponentInParent<TMPro.TMP_Text>() != null)
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
        ResetDetectionOutputs();
    }

    void ResetDetectionOutputs()
    {
        TargetLock = 0f;
        OrbitPhase = 0f;
        CurrentTarget = null;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        Vector3 pos = transform.position;

        if (_hasLastTrackedPos && dt > 0f)
        {
            float instantSpeed = (pos - _lastTrackedPos).magnitude / dt;
            if (cameraSpeedSmoothing > 0f)
            {
                float k = 1f - Mathf.Exp(-dt / cameraSpeedSmoothing);
                CameraSpeed += (instantSpeed - CameraSpeed) * k;
            }
            else
            {
                CameraSpeed = instantSpeed;
            }
        }

        _lastTrackedPos = pos;
        _hasLastTrackedPos = true;

        float speedTerm = 0.3f + 0.7f * Mathf.Clamp01(CameraSpeed / TearSpeedReference);
        TearIntensity = Mathf.Clamp01(TargetLock) * speedTerm;
    }

    float TearSpeedReference =>
        tearSpeedRef > 0f ? tearSpeedRef
        : _tearSpeedCaptured ? _tearSpeedRuntime
        : Mathf.Max(approachSpeed, 0.0001f);

    IEnumerator FocusRoutine(Vector3 endPos, Quaternion endRot, float duration)
    {
        if (focusStartDelay > 0f)
            yield return new WaitForSeconds(focusStartDelay);

        yield return AnimatePose(
            transform.position, transform.rotation,
            endPos, endRot,
            duration, approachCurve);
        _motion = null;
        State = MotionState.Idle;
        ResetDetectionOutputs();
    }

    IEnumerator ZoomOutRoutine()
    {
        State = MotionState.ZoomingOut;
        ResetDetectionOutputs();
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

        if (tourStartDelay > 0f)
            yield return new WaitForSeconds(tourStartDelay);

        int count = _tourStops.Count;
        bool single = count == 1;
        int index = 0;

        CompanyMapObject prevTarget = null;
        Bounds prevBounds = default;

        while (true)
        {
            TourStop stop = _tourStops[index];
            if (stop.building == null)
            {
                if (single)
                {
                    ResetDetectionOutputs();
                    yield break;
                }

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
            yield return TourTransition(prevTarget, prevBounds, stop, entryPos, entryRot, approachDur);

            CurrentTarget = stop.mapObject;
            CurrentTargetBounds = stop.bounds;
            TargetLock = 1f;

            if (single)
            {
                while (true)
                {
                    yield return OrbitSweep(center, radius, yaw, stop.clockwise);
                    yaw = _orbitEndYaw;
                }
            }

            yield return OrbitSweep(center, radius, yaw, stop.clockwise);
            OrbitPhase = 0f;

            prevTarget = stop.mapObject;
            prevBounds = stop.bounds;
            index = (index + 1) % count;
        }
    }

    /// <summary>
    /// Move between tour stops while driving <see cref="TargetLock"/> / <see cref="CurrentTarget"/>:
    /// unlock from <paramref name="prev"/> over the first <see cref="departureUnlockFraction"/>,
    /// a dead zone with no target, then lock onto <paramref name="next"/> over the last
    /// <see cref="approachLockFraction"/>.
    /// </summary>
    IEnumerator TourTransition(
        CompanyMapObject prev, Bounds prevBounds,
        TourStop next, Vector3 endPos, Quaternion endRot, float duration)
    {
        OrbitPhase = 0f;

        float unlockEnd = Mathf.Clamp01(departureUnlockFraction);
        float lockStart = 1f - Mathf.Clamp01(approachLockFraction);

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        if (duration <= 0f)
        {
            transform.SetPositionAndRotation(endPos, endRot);
            CurrentTarget = next.mapObject;
            CurrentTargetBounds = next.bounds;
            TargetLock = 1f;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / duration);
            float e = EvaluateEase(t, approachCurve);
            transform.SetPositionAndRotation(
                Vector3.LerpUnclamped(startPos, endPos, e),
                Quaternion.SlerpUnclamped(startRot, endRot, e));

            // Auto-calibrate the tear speed reference from the first mid-approach.
            if (!_tearSpeedCaptured && tearSpeedRef <= 0f && t >= 0.5f)
            {
                _tearSpeedRuntime = Mathf.Max(CameraSpeed, 0.0001f);
                _tearSpeedCaptured = true;
            }

            if (prev != null && t < unlockEnd)
            {
                CurrentTarget = prev;
                CurrentTargetBounds = prevBounds;
                TargetLock = unlockEnd > 0f ? 1f - t / unlockEnd : 0f;
            }
            else if (t < lockStart)
            {
                CurrentTarget = null;
                TargetLock = 0f;
            }
            else
            {
                CurrentTarget = next.mapObject;
                CurrentTargetBounds = next.bounds;
                TargetLock = lockStart < 1f ? (t - lockStart) / (1f - lockStart) : 1f;
            }

            yield return null;
        }

        CurrentTarget = next.mapObject;
        CurrentTargetBounds = next.bounds;
        TargetLock = 1f;
    }

    IEnumerator OrbitSweep(Vector3 center, float radius, float startYaw, bool clockwise)
    {
        float signedSweepRad = (clockwise ? -1f : 1f) * orbitSweepDegrees * Mathf.Deg2Rad;
        float endYaw = startYaw + signedSweepRad;
        float duration = Mathf.Max(0.0001f, orbitDuration);

        TargetLock = 1f;

        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / duration);
            OrbitPhase = t;
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
