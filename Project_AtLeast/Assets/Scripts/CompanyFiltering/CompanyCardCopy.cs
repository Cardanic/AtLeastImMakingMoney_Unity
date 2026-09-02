/// <summary>
/// Holdings-row copy for a company card: name on the left, figures on the right.
/// </summary>
public static class CompanyCardCopy
{
    public const string Unavailable = "n/d";

    const int LabelWidth = 14; // room for "Military 2024:"

    public static string SubIndustry(Organization org)
    {
        if (org == null || string.IsNullOrWhiteSpace(org.sub_industry))
            return Unavailable;
        return org.sub_industry;
    }

    public static string Metrics(Organization org)
    {
        return Line("Total 2025:", TotalRevenue2025(org)) + "\n"
            + Line("Military 2024:", MilitaryRevenue2024(org)) + "\n"
            + Line("Lobby:", LobbyingEu(org?.lobbying_cost_EU));
    }

    public static string TotalRevenue2025(Organization org)
    {
        if (org?.total_revenue_2025_amount == null || org.total_revenue_2025_amount <= 0)
            return Unavailable;
        return CompactEuroFormat.Format(
            org.total_revenue_2025_amount.Value,
            CurrencyFromRaw(org.total_revenue_2025));
    }

    public static string MilitaryRevenue2024(Organization org)
    {
        double? amount = org?.MilitaryRevenue2024Numeric;
        if (amount == null || amount <= 0)
            return Unavailable;
        return CompactEuroFormat.Format(amount.Value, "$");
    }

    public static string LobbyingEu(double? amount)
    {
        if (amount == null || amount.Value <= 0)
            return Unavailable;
        return CompactEuroFormat.Format(amount.Value);
    }

    static string Line(string label, string value)
    {
        if (label.Length >= LabelWidth)
            return label + " " + value;
        return label.PadRight(LabelWidth) + value;
    }

    static string CurrencyFromRaw(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "€";
        if (raw.Contains("$"))
            return "$";
        if (raw.Contains("£"))
            return "£";
        return "€";
    }
}
