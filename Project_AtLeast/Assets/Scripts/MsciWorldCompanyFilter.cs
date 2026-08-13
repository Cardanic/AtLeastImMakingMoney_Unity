using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Loads <c>content/msci-world-etf/data.json</c> and filters companies by three independent criteria.
/// A company is kept if it matches <b>any</b> enabled criterion (OR).
/// </summary>
/// <remarks>
/// Requires the Newtonsoft Json package: Window → Package Manager →
/// <c>com.unity.nuget.newtonsoft-json</c>.
/// Assign the JSON as a TextAsset (e.g. drop data.json into Resources or StreamingAssets).
/// </remarks>
public sealed class MsciWorldCompanyFilter : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("Paste or assign content/msci-world-etf/data.json as a TextAsset.")]
    [SerializeField]
    TextAsset dataJson;

    [Header("1 — LobbyFacts")]
    [Tooltip("When enabled: keep companies with matched_lobbyfacts == true and score >= threshold.")]
    [SerializeField]
    bool filterByLobbying = true;

    [SerializeField, Range(0f, 1f)]
    float lobbyingEconomicExposureScore;

    [Header("2 — SIPRI / military")]
    [Tooltip("When enabled: keep companies with matched_sipri == true and score >= threshold.")]
    [SerializeField]
    bool filterByMilitary = true;

    [SerializeField, Range(0f, 1f)]
    float militaryEconomicExposureScore;

    [Header("3 — Who Profits")]
    [Tooltip("When enabled: keep companies with matched_who_profits == true.")]
    [SerializeField]
    bool filterByWhoProfits = true;

    [Header("Runtime")]
    [SerializeField]
    bool filterOnStart = true;

    List<Organization> _all = new();
    List<Organization> _filtered = new();

    public IReadOnlyList<Organization> AllCompanies => _all;
    public IReadOnlyList<Organization> FilteredCompanies => _filtered;

    public event Action<IReadOnlyList<Organization>> Filtered;

    void Start()
    {
        if (dataJson == null)
        {
            Debug.LogError($"{nameof(MsciWorldCompanyFilter)}: assign dataJson (MSCI World data.json).");
            return;
        }

        Load(dataJson.text);
        if (filterOnStart)
            ApplyFilter();
    }

    /// <summary>Parse the root <c>{ "data": [...] }</c> document.</summary>
    public void Load(string json)
    {
        var root = JsonConvert.DeserializeObject<OrganizationDataset>(json);
        _all = root?.data ?? new List<Organization>();
        _filtered = new List<Organization>(_all);
    }

    /// <summary>
    /// Re-run the three filters against the loaded dataset.
    /// Call this from UI sliders / toggles when inputs change.
    /// </summary>
    public void ApplyFilter()
    {
        _filtered = _all.Where(PassesAnyEnabledFilter).ToList();
        Filtered?.Invoke(_filtered);
        Debug.Log(
            $"{nameof(MsciWorldCompanyFilter)}: {_filtered.Count} / {_all.Count} companies " +
            $"(lobby≥{lobbyingEconomicExposureScore:0.###}, mil≥{militaryEconomicExposureScore:0.###}, " +
            $"whoProfits={filterByWhoProfits})."
        );
    }

    /// <summary>Inspector / UI setters — update one input then re-filter.</summary>
    public void SetLobbyingEconomicExposureScore(float value)
    {
        lobbyingEconomicExposureScore = Mathf.Clamp01(value);
        ApplyFilter();
    }

    public void SetMilitaryEconomicExposureScore(float value)
    {
        militaryEconomicExposureScore = Mathf.Clamp01(value);
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

    bool PassesAnyEnabledFilter(Organization org)
    {
        if (filterByLobbying && MatchesLobbying(org))
            return true;
        if (filterByMilitary && MatchesMilitary(org))
            return true;
        if (filterByWhoProfits && MatchesWhoProfits(org))
            return true;

        // Nothing enabled → show everything; otherwise exclude.
        return !filterByLobbying && !filterByMilitary && !filterByWhoProfits;
    }

    bool MatchesLobbying(Organization org)
    {
        return org.matched_lobbyfacts
            && org.lobbying_economic_exposure_score.HasValue
            && org.lobbying_economic_exposure_score.Value >= lobbyingEconomicExposureScore;
    }

    bool MatchesMilitary(Organization org)
    {
        return org.matched_sipri
            && org.military_economic_exposure_score.HasValue
            && org.military_economic_exposure_score.Value >= militaryEconomicExposureScore;
    }

    static bool MatchesWhoProfits(Organization org) => org.matched_who_profits;

    [Serializable]
    public sealed class OrganizationDataset
    {
        public List<Organization> data = new();
    }

    [Serializable]
    public sealed class Organization
    {
        public int id;
        public string indice;
        public string company_name;
        public string name_sipri;
        public bool matched_sipri;
        public int? sipri_rank;
        public string name_lobbyfacts;
        public bool matched_lobbyfacts;
        public string name_who_profits;
        public bool matched_who_profits;
        public string israeli_occupation_involvement;
        public List<string> israeli_occupation_involvement_category;
        public string israeli_occupation_region;
        public string industry;
        public string sub_industry;
        public string stock_ticker;
        public string country;
        public string total_revenue_2024;
        public double? total_revenue_2024_euro;
        public string total_revenue_2025;
        public double? total_revenue_2025_euro;
        public string total_revenue_2023;
        public double? total_revenue_2023_euro;
        public string total_revenue_change;
        public double? arms_revenue_pct_of_total;
        public string military_revenue_2024;
        public double? military_revenue_2024_euro;
        public string military_revenue_2023;
        public double? military_revenue_2023_euro;
        public double? lobbying_cost_EU;
        public int? lobbying_costs_year;
        public List<string> traded_in;
        public float? lobbying_economic_exposure_score;
        public float? military_economic_exposure_score;
    }
}
