using NUnit.Framework;

static class CompanyTestFixtures
{
    public static Organization Org(
        int id,
        bool lobby = false,
        float? lobbyScore = null,
        bool military = false,
        float? militaryScore = null,
        bool whoProfits = false)
    {
        return new Organization
        {
            id = id,
            company_name = $"Company {id}",
            matched_lobbyfacts = lobby,
            lobbying_economic_exposure_score = lobbyScore,
            matched_sipri = military,
            military_economic_exposure_score = militaryScore,
            matched_who_profits = whoProfits
        };
    }
}
