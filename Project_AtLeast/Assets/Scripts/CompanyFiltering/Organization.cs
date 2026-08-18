using System;
using System.Collections.Generic;

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
