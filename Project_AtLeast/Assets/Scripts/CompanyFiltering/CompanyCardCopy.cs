/// <summary>
/// Holdings-row copy for a company card: name on the left, figures on the right.
/// </summary>
public static class CompanyCardCopy
{
    public const string Unavailable = "n/d";

    const int LabelWidth = 10; // room for "military:"

    public static string SubIndustry(Organization org)
    {
        if (org == null || string.IsNullOrWhiteSpace(org.sub_industry))
            return Unavailable;
        return org.sub_industry;
    }

    public static string Metrics(Organization org)
    {
        return Line("total:", DisplayOrNd(org?.total_revenue_2025)) + "\n"
            + Line("military:", DisplayOrNd(org?.military_revenue_2024)) + "\n"
            + Line("lobby:", LobbyingEu(org?.lobbying_cost_EU));
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

    static string DisplayOrNd(string raw)
    {
        return string.IsNullOrWhiteSpace(raw) ? Unavailable : raw;
    }
}
