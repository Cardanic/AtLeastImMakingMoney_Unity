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
