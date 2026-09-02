using System.Collections.Generic;

/// <summary>
/// Maps a disclosed amount onto a 0–100 dial against the dataset maximum.
/// Missing or non-positive amounts stay null (dial 0 / n/d), never score 0.
/// </summary>
public static class ExposureScore
{
    public static float? FromAmount(double? amount, double datasetMax)
    {
        if (!amount.HasValue || amount.Value <= 0.0 || datasetMax <= 0.0)
            return null;
        return (float)(amount.Value / datasetMax * 100.0);
    }

    public static void AssignFromAmounts(
        IReadOnlyList<Organization> companies,
        double maxLobbyingCostEU,
        double maxMilitaryRevenue)
    {
        if (companies == null)
            return;

        for (int i = 0; i < companies.Count; i++)
        {
            Organization org = companies[i];
            org.lobbying_economic_exposure_score =
                FromAmount(org.lobbying_cost_EU, maxLobbyingCostEU);
            org.military_economic_exposure_score =
                FromAmount(org.MilitaryRevenue2024Numeric, maxMilitaryRevenue);
        }
    }
}
