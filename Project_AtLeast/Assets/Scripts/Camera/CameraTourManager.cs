using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Watches the company filters. While they're actively being changed, the
/// camera pulls back to the home/overview pose. Once changes settle for
/// <see cref="tourStartDelay"/> and zoom-out has finished, the camera tours
/// every spawned building in <see cref="MsciWorldCompanyFilter.FilteredCompanies"/>
/// order — or stays home if none are spawned.
/// </summary>
public class CameraTourManager : MonoBehaviour
{
    [Header("References")]
    public MsciWorldCompanyFilter dataSource;
    public CompanyMapSpawner spawner;

    [Header("Tour start")]
    [Tooltip("Seconds with no Filtered events before a tour may start (after zoom-out completes).")]
    public float tourStartDelay = 3f;

    readonly List<Transform> _tourStops = new();
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
        if (CameraFocus.Instance != null)
            CameraFocus.Instance.ReturnHome();

        if (debounceCoroutine != null)
            StopCoroutine(debounceCoroutine);

        debounceCoroutine = StartCoroutine(DebounceThenTour());
    }

    IEnumerator DebounceThenTour()
    {
        yield return new WaitForSeconds(tourStartDelay);

        if (CameraFocus.Instance == null)
        {
            debounceCoroutine = null;
            yield break;
        }

        while (CameraFocus.Instance != null && CameraFocus.Instance.State != CameraFocus.MotionState.Idle)
            yield return null;

        debounceCoroutine = null;

        if (CameraFocus.Instance == null)
            yield break;

        RebuildTourStops();

        if (_tourStops.Count == 0)
        {
            CameraFocus.Instance.ReturnHome();
            yield break;
        }

        CameraFocus.Instance.StartTour(_tourStops);
    }

    void RebuildTourStops()
    {
        _tourStops.Clear();

        if (dataSource == null || spawner == null)
            return;

        IReadOnlyList<Organization> portfolio = dataSource.FilteredCompanies;
        for (int i = 0; i < portfolio.Count; i++)
        {
            Organization org = portfolio[i];
            if (org == null)
                continue;

            if (spawner.TryGetSpawnedTransform(org.id, out Transform building))
                _tourStops.Add(building);
        }
    }
}
