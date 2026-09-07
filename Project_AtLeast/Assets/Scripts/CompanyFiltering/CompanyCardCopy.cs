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
            CurrencySymbol(org.reporting_currency, org.total_revenue_2025));
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

    /// <summary>
    /// Symbol for the company's own reporting currency (total_revenue_* is never converted).
    /// Prefers the ISO code in <c>reporting_currency</c>; falls back to sniffing the formatted
    /// string for rows that predate that field. Never silently defaults a yen or krone figure to €.
    /// </summary>
    public static string CurrencySymbol(string reportingCurrency, string raw)
    {
        switch ((reportingCurrency ?? string.Empty).Trim().ToUpperInvariant())
        {
            case "USD": return "$";
            case "EUR": return "€";
            case "GBP": return "£";
            case "JPY": return "¥";
            case "CAD": return " C$";
            case "": break;
            default: return " " + reportingCurrency.Trim().ToUpperInvariant(); // NOK, SEK, CHF, ILS …
        }

        if (string.IsNullOrWhiteSpace(raw))
            return "€";
        if (raw.Contains("C$"))
            return " C$";
        if (raw.Contains("$"))
            return "$";
        if (raw.Contains("£"))
            return "£";
        if (raw.Contains("¥"))
            return "¥";
        if (raw.Contains("€"))
            return "€";
        string trimmed = raw.Trim();
        if (trimmed.Length < 3)
            return "€";
        string tail = trimmed.Substring(trimmed.Length - 3);
        return char.IsLetter(tail[0]) ? " " + tail.ToUpperInvariant() : "€";
    }
}
