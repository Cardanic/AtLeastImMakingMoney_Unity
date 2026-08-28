using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Loads <c>content/msci-world-etf/data.json</c> and filters companies by three independent criteria.
/// A company is kept only if it matches <b>every</b> enabled criterion (AND).
/// </summary>
/// <remarks>
/// Requires the Newtonsoft Json package: Window → Package Manager →
/// <c>com.unity.nuget.newtonsoft-json</c>.
/// Assign the JSON as a TextAsset (e.g. drop data.json into Resources or StreamingAssets).
/// </remarks>
public sealed class MsciWorldCompanyFilter : MonoBehaviour, ICompanyFilterSource
{
    [Header("Source")]
    [Tooltip("Paste or assign content/msci-world-etf/data.json as a TextAsset.")]
    [SerializeField]
    TextAsset dataJson;

    [Header("1 — LobbyFacts")]
    [Tooltip("When enabled: keep companies with lobbying score <= threshold (null / n/d always passes; dial 0 = n/d only).")]
    [SerializeField]
    bool filterByLobbying = true;

    [SerializeField, Range(0f, 100f)]
    float lobbyingEconomicExposureScore;

    [Header("2 — SIPRI / military")]
    [Tooltip("When enabled: keep companies with military score <= threshold (null / n/d always passes; dial 0 = n/d only).")]
    [SerializeField]
    bool filterByMilitary = true;

    [SerializeField, Range(0f, 100f)]
    float militaryEconomicExposureScore;

    [Header("3 — Who Profits")]
    [Tooltip("On: keep only Who Profits firms. Off: exclude Who Profits firms.")]
    [SerializeField]
    bool filterByWhoProfits = true;

    [Header("Runtime")]
    [SerializeField]
    bool filterOnStart = true;
    

    List<Organization> _all = new();
    List<Organization> _filtered = new();
    bool _loaded;

    public IReadOnlyList<Organization> AllCompanies => _all;
    public IReadOnlyList<Organization> FilteredCompanies => _filtered;
    public bool HasFiltered { get; private set; }

    public event Action<IReadOnlyList<Organization>> Filtered;

    void Awake()
    {
        if (dataJson == null)
        {
            Debug.LogError($"{nameof(MsciWorldCompanyFilter)}: assign dataJson (MSCI World data.json).");
            return;
        }

        Load(dataJson.text);
        // Apply in Awake so CompanyIdUdpSender's OnEnable catch-up already has ids
        // before phones are welcomed on the first Update ticks.
        if (filterOnStart && _loaded)
            ApplyFilter();
    }

    void Start()
    {
        // Safety: if something loaded data after Awake without filtering yet.
        if (filterOnStart && _loaded && !HasFiltered)
            ApplyFilter();
    }

    /// <summary>Parse the root <c>{ "data": [...] }</c> document.</summary>
    public void Load(string json)
    {
        var root = JsonConvert.DeserializeObject<OrganizationDataset>(json);
        _all = root?.data ?? new List<Organization>();
        _filtered = new List<Organization>();
        _loaded = true;
        HasFiltered = false;
    }

    /// <summary>
    /// Re-run the three filters against the loaded dataset.
    /// Call this from UI sliders / toggles when inputs change.
    /// </summary>
    public void ApplyFilter()
    {
        _filtered = CompanyFilter.Apply(_all, CurrentCriteria());
        HasFiltered = true;
        Filtered?.Invoke(_filtered);
        Debug.Log(
            $"{nameof(MsciWorldCompanyFilter)}: {_filtered.Count} / {_all.Count} companies " +
            $"(lobby≤{lobbyingEconomicExposureScore:0}, mil≤{militaryEconomicExposureScore:0}, " +
            $"whoProfits={filterByWhoProfits})."
        );
    }

    /// <summary>Inspector / UI setters — update one input then re-filter.</summary>
    public void SetLobbyingEconomicExposureScore(float value)
    {
        lobbyingEconomicExposureScore = Mathf.Clamp(Mathf.Round(value), 0f, 100f);
        ApplyFilter();
    }

    public void SetMilitaryEconomicExposureScore(float value)
    {
        militaryEconomicExposureScore = Mathf.Clamp(Mathf.Round(value), 0f, 100f);
        ApplyFilter();
    }

    public void SetFilterByWhoProfits(bool enabled)
    {
        filterByWhoProfits = enabled;
        ApplyFilter();
    }

    public void SetFilterByLobbying(bool enabled)
    {
        filterByLobbying = enabled;
        ApplyFilter();
    }

    public void SetFilterByMilitary(bool enabled)
    {
        filterByMilitary = enabled;
        ApplyFilter();
    }

    CompanyFilterCriteria CurrentCriteria()
    {
        return new CompanyFilterCriteria
        {
            FilterByLobbying = filterByLobbying,
            LobbyingEconomicExposureScore = lobbyingEconomicExposureScore,
            FilterByMilitary = filterByMilitary,
            MilitaryEconomicExposureScore = militaryEconomicExposureScore,
            FilterByWhoProfits = filterByWhoProfits
        };
    }
}
