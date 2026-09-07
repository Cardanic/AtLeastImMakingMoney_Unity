using NUnit.Framework;

public class CompanyCardCopyTests
{
    [Test]
    public void SubIndustry_UsesSubIndustryOnly()
    {
        var org = new Organization
        {
            industry = "Industrials",
            sub_industry = "Aerospace & Defense"
        };

        Assert.AreEqual("Aerospace & Defense", CompanyCardCopy.SubIndustry(org));
    }

    [Test]
    public void SubIndustry_Missing_ShowsNd()
    {
        Assert.AreEqual("n/d", CompanyCardCopy.SubIndustry(null));
        Assert.AreEqual("n/d", CompanyCardCopy.SubIndustry(new Organization()));
    }

    [Test]
    public void Metrics_StacksRevenueMilitaryAndLobbyingEuro()
    {
        var org = new Organization
        {
            total_revenue_2025 = "89.463.000.000$",
            total_revenue_2025_amount = 89_463_000_000,
            military_revenue_2024 = "30.550.000.000$",
            military_revenue_2024_amount = 30_550_000_000,
            lobbying_cost_EU = 800_000
        };

        Assert.AreEqual(
            "Total 2025:   89.5B$\nMilitary 2024: 30.6B$\nLobby:        800K€",
            CompanyCardCopy.Metrics(org));
    }

    [Test]
    public void Metrics_MissingAmounts_ShowNd()
    {
        var org = new Organization
        {
            total_revenue_2025 = null,
            military_revenue_2024 = null,
            lobbying_cost_EU = null
        };

        Assert.AreEqual(
            "Total 2025:   n/d\nMilitary 2024: n/d\nLobby:        n/d",
            CompanyCardCopy.Metrics(org));
    }

    [Test]
    public void TotalRevenue2025_UsesReportingCurrency_NotEuroFallback()
    {
        // Toyota: yen, trillions → needs the T tier and the ¥ symbol (was "50685B€").
        var toyota = new Organization
        {
            reporting_currency = "JPY",
            total_revenue_2025 = "50.684.952.000.000¥",
            total_revenue_2025_amount = 50_684_952_000_000
        };
        Assert.AreEqual("50.7T¥", CompanyCardCopy.TotalRevenue2025(toyota));

        // Kongsberg: NOK has no one-glyph symbol (was "58.6B€", ~10x the euro value).
        var kongsberg = new Organization
        {
            reporting_currency = "NOK",
            total_revenue_2025 = "58.599.000.000NOK",
            total_revenue_2025_amount = 58_599_000_000
        };
        Assert.AreEqual("58.6B NOK", CompanyCardCopy.TotalRevenue2025(kongsberg));

        // CAE: "C$" must not be read as US dollars.
        var cae = new Organization
        {
            reporting_currency = "CAD",
            total_revenue_2025 = "4.914.000.000C$",
            total_revenue_2025_amount = 4_914_000_000
        };
        Assert.AreEqual("4.9B C$", CompanyCardCopy.TotalRevenue2025(cae));
    }

    [Test]
    public void CurrencySymbol_FallsBackToRawString_WhenCodeMissing()
    {
        Assert.AreEqual("$", CompanyCardCopy.CurrencySymbol(null, "89.463.000.000$"));
        Assert.AreEqual("£", CompanyCardCopy.CurrencySymbol("", "30.662.000.000£"));
        Assert.AreEqual("¥", CompanyCardCopy.CurrencySymbol(null, "3.582.733.000.000¥"));
        Assert.AreEqual(" C$", CompanyCardCopy.CurrencySymbol(null, "4.914.000.000C$"));
        Assert.AreEqual(" NOK", CompanyCardCopy.CurrencySymbol(null, "58.599.000.000NOK"));
        Assert.AreEqual("€", CompanyCardCopy.CurrencySymbol(null, null));
    }

    [Test]
    public void LobbyingEu_ZeroOrNull_IsNd_NotZeroEuro()
    {
        Assert.AreEqual("n/d", CompanyCardCopy.LobbyingEu(null));
        Assert.AreEqual("n/d", CompanyCardCopy.LobbyingEu(0));
        Assert.AreEqual("2M€", CompanyCardCopy.LobbyingEu(2_000_000));
    }
}
