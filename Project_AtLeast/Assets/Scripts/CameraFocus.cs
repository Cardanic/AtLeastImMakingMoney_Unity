using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFocus : MonoBehaviour
{
    public static CameraFocus Instance;

    [Header("Focus Settings")]
    public float moveSpeed = 5f;
    public float heightOffset = 10f;   // how high above the target the camera sits
    public float backOffset = 8f;      // how far back from the target the camera sits
    public float arriveThreshold = 0.05f;

    [Header("Tour Settings")]
    [Tooltip("How long the camera lingers on each building before moving to the next.")]
    public float pauseAtEachStop = 1.5f;
    [Tooltip("If true, keep cycling through the tour forever (never returns home). If false, return to the home view after the last stop.")]
    public bool loopTour = false;

    [Header("Home / Overview")]
    [Tooltip("Optional. If set, the camera returns here (position + rotation) after a tour finishes. If left empty, the camera's starting transform in the scene is used instead.")]
    public Transform homeTransform;
    [Tooltip("How long to pause at the last building before heading back home.")]
    public float pauseBeforeReturningHome = 1f;

    Vector3 targetPosition;
    Quaternion targetRotation;
    bool useRotation = false;
    bool isMoving = false;
    Coroutine tourCoroutine;

    Vector3 homePosition;
    Quaternion homeRotation;

    void Awake()
    {
        Instance = this;

        // Remember where the camera started (or use an explicit homeTransform if assigned).
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

    /// <summary>Jump straight to one position (used by card clicks). Cancels any running tour.</summary>
    public void FocusOn(Vector3 worldPosition)
    {
        StopTour();
        useRotation = false;
        targetPosition = worldPosition + new Vector3(0, heightOffset, -backOffset);
        isMoving = true;
    }

    /// <summary>
    /// Start (or replace) an automatic tour: visits each position in order,
    /// pausing briefly at each, then returns to the home/overview position
    /// (unless loopTour is true).
    /// </summary>
    public void StartTour(IReadOnlyList<Vector3> positions)
    {
        StopTour();

        if (positions == null || positions.Count == 0)
            return;

        tourCoroutine = StartCoroutine(TourRoutine(positions));
    }

    public void StopTour()
    {
        if (tourCoroutine != null)
        {
            StopCoroutine(tourCoroutine);
            tourCoroutine = null;
        }
    }

    /// <summary>Send the camera back to the overview position immediately.</summary>
    public void ReturnHome()
    {
        StopTour();
        useRotation = true;
        targetPosition = homePosition;
        targetRotation = homeRotation;
        isMoving = true;
    }

    IEnumerator TourRoutine(IReadOnlyList<Vector3> positions)
    {
        do
        {
            foreach (var pos in positions)
            {
                useRotation = false;
                targetPosition = pos + new Vector3(0, heightOffset, -backOffset);
                isMoving = true;

                while (Vector3.Distance(transform.position, targetPosition) > arriveThreshold)
                    yield return null;

                transform.position = targetPosition;
                isMoving = false;

                yield return new WaitForSeconds(pauseAtEachStop);
            }
        }
        while (loopTour);

        // Tour is done (non-looping) — head back to the overview.
        yield return new WaitForSeconds(pauseBeforeReturningHome);

        useRotation = true;
        targetPosition = homePosition;
        targetRotation = homeRotation;
        isMoving = true;

        while (Vector3.Distance(transform.position, targetPosition) > arriveThreshold)
            yield return null;

        transform.position = targetPosition;
        transform.rotation = homeRotation;
        isMoving = false;

        tourCoroutine = null;
    }

    void Update()
    {
        if (!isMoving) return;

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);

        if (useRotation)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * moveSpeed);

        if (Vector3.Distance(transform.position, targetPosition) < arriveThreshold)
        {
            transform.position = targetPosition;
            if (useRotation)
                transform.rotation = targetRotation;
            isMoving = false;
        }
    }
}