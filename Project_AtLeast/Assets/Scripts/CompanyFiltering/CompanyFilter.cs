using System.Collections.Generic;

public struct CompanyFilterCriteria
{
    public bool FilterByLobbying;
    public float LobbyingEconomicExposureScore;
    public bool FilterByMilitary;
    public float MilitaryEconomicExposureScore;
    public bool FilterByWhoProfits;
}

public static class CompanyFilter
{
    public static List<Organization> Apply(IReadOnlyList<Organization> all, in CompanyFilterCriteria criteria)
    {
        var filtered = new List<Organization>();
        if (all == null)
            return filtered;

        for (int i = 0; i < all.Count; i++)
        {
            if (Passes(all[i], criteria))
                filtered.Add(all[i]);
        }

        SortByTotalRevenue2025Desc(filtered);
        return filtered;
    }

    public static bool Passes(Organization org, in CompanyFilterCriteria criteria)
    {
        if (org == null)
            return false;

        if (criteria.FilterByLobbying
            && !MatchesLobbying(org, criteria.LobbyingEconomicExposureScore))
            return false;
        if (criteria.FilterByMilitary
            && !MatchesMilitary(org, criteria.MilitaryEconomicExposureScore))
            return false;

        // Always applied: true = only Who Profits firms; false = exclude them.
        if (org.matched_who_profits != criteria.FilterByWhoProfits)
            return false;

        return true;
    }

    /// <summary>Highest <c>total_revenue_2025_amount</c> first; missing amounts last.</summary>
    public static void SortByTotalRevenue2025Desc(List<Organization> companies)
    {
        if (companies == null || companies.Count < 2)
            return;

        companies.Sort(CompareTotalRevenue2025Desc);
    }

    static int CompareTotalRevenue2025Desc(Organization a, Organization b)
    {
        double aAmount = a?.total_revenue_2025_amount ?? double.MinValue;
        double bAmount = b?.total_revenue_2025_amount ?? double.MinValue;
        int byAmount = bAmount.CompareTo(aAmount);
        if (byAmount != 0)
            return byAmount;

        int aId = a?.id ?? 0;
        int bId = b?.id ?? 0;
        return aId.CompareTo(bId);
    }

    static bool MatchesLobbying(Organization org, float threshold)
    {
        // Null / 0 is n/d and always passes this axis. Dial 0 keeps only n/d.
        if (IsUndisclosed(org.lobbying_economic_exposure_score))
            return true;
        return threshold > 0f
            && org.lobbying_economic_exposure_score.Value <= threshold;
    }

    static bool MatchesMilitary(Organization org, float threshold)
    {
        if (IsUndisclosed(org.military_economic_exposure_score))
            return true;
        return threshold > 0f
            && org.military_economic_exposure_score.Value <= threshold;
    }

    static bool IsUndisclosed(float? score)
    {
        return !score.HasValue || score.Value <= 0f;
    }
}
