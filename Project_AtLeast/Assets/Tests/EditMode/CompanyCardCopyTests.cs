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
    public void LobbyingEu_ZeroOrNull_IsNd_NotZeroEuro()
    {
        Assert.AreEqual("n/d", CompanyCardCopy.LobbyingEu(null));
        Assert.AreEqual("n/d", CompanyCardCopy.LobbyingEu(0));
        Assert.AreEqual("2M€", CompanyCardCopy.LobbyingEu(2_000_000));
    }
}
