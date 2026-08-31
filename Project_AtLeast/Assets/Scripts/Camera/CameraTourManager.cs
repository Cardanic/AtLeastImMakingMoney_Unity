using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Watches the company filters. While they're actively being changed, the
/// camera snaps back to the home/overview position. Once changes settle for
/// <see cref="debounceSeconds"/>, the camera tours every building currently
/// spawned — or just stays home if no buildings are spawned at all.
/// </summary>
public class CameraTourManager : MonoBehaviour
{
    [Header("References")]
    public MsciWorldCompanyFilter dataSource;
    public CompanyMapSpawner spawner;

    [Header("Debounce")]
    [Tooltip("Seconds of no filter changes before the tour starts.")]
    public float debounceSeconds = 1f;

    Coroutine debounceCoroutine;

    void OnEnable()
    {
        if (dataSource != null)
            dataSource.Filtered += OnFiltersChanged;
    }

    void OnDisable()
    {
        if (dataSource != null)
            dataSource.Filtered -= OnFiltersChanged;

        if (debounceCoroutine != null)
            StopCoroutine(debounceCoroutine);
    }

    void OnFiltersChanged(IReadOnlyList<Organization> _)
    {
        // Filters are "active" right now — interrupt any tour and go home
        // immediately, then restart the settle countdown.
        if (CameraFocus.Instance != null)
            CameraFocus.Instance.ReturnHome();

        if (debounceCoroutine != null)
            StopCoroutine(debounceCoroutine);

        debounceCoroutine = StartCoroutine(DebounceThenTour());
    }

    IEnumerator DebounceThenTour()
    {
        yield return new WaitForSeconds(debounceSeconds);
        debounceCoroutine = null;

        if (spawner == null || CameraFocus.Instance == null)
            yield break;

        var positions = spawner.GetSpawnedPositionsOrdered();

        if (positions.Count == 0)
        {
            // No buildings in the scene -> stay at the overview.
            CameraFocus.Instance.ReturnHome();
            yield break;
        }

        CameraFocus.Instance.StartTour(positions);
    }
}