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

        if (criteria.FilterByLobbying && MatchesLobbying(org, criteria.LobbyingEconomicExposureScore))
            return true;
        if (criteria.FilterByMilitary && MatchesMilitary(org, criteria.MilitaryEconomicExposureScore))
            return true;
        if (criteria.FilterByWhoProfits && MatchesWhoProfits(org))
            return true;

        return !criteria.FilterByLobbying && !criteria.FilterByMilitary && !criteria.FilterByWhoProfits;
    }

    static bool MatchesLobbying(Organization org, float threshold)
    {
        return org.matched_lobbyfacts
            && org.lobbying_economic_exposure_score.HasValue
            && org.lobbying_economic_exposure_score.Value >= threshold;
    }

    static bool MatchesMilitary(Organization org, float threshold)
    {
        return org.matched_sipri
            && org.military_economic_exposure_score.HasValue
            && org.military_economic_exposure_score.Value >= threshold;
    }

    static bool MatchesWhoProfits(Organization org) => org.matched_who_profits;
}
