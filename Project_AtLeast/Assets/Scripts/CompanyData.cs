using System.Collections.Generic;

[System.Serializable]
public class CompanyData
{
    public int id;
    public string name;
    public float lobbying_economic_exposure_score;
    public float military_economic_exposure_score;
    public string industry;
    public string sub_industry;
    public string well_known_for;
    public string military_revenue_2023;
    public string military_revenue_2024;
    public string total_revenue_2023;
    public string total_revenue_2024;
    public string total_revenue_change;
    public string arms_revenue_pct_of_total;
    public string lobbying_cost_EU;
}

[System.Serializable]
public class CompanyDataRoot
{
    public string generated;
    public List<CompanyData> data;
}