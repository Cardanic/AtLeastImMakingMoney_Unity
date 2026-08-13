public class CompanyMetrics
{
    public CompanyData raw;

    public float militaryRevenue2023;
    public float militaryRevenue2024;
    public float totalRevenue2023;
    public float totalRevenue2024;
    public float revenueChangeAbs;
    public float revenueChangePct;
    public float armsRevenuePct;
    public float lobbyingCostEU;

    public static CompanyMetrics FromRaw(CompanyData data)
    {
        var (changeAbs, changePct) = EuroParser.ParseChangeString(data.total_revenue_change);

        return new CompanyMetrics
        {
            raw = data,
            militaryRevenue2023 = EuroParser.ParseEuroAmount(data.military_revenue_2023),
            militaryRevenue2024 = EuroParser.ParseEuroAmount(data.military_revenue_2024),
            totalRevenue2023 = EuroParser.ParseEuroAmount(data.total_revenue_2023),
            totalRevenue2024 = EuroParser.ParseEuroAmount(data.total_revenue_2024),
            revenueChangeAbs = changeAbs,
            revenueChangePct = changePct,
            armsRevenuePct = EuroParser.ParsePercent(data.arms_revenue_pct_of_total, normalize: true),
            lobbyingCostEU = EuroParser.ParseEuroAmount(data.lobbying_cost_EU)
        };
    }
}