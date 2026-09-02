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
            military_revenue_2024 = "30.550.000.000$",
            lobbying_cost_EU = 800_000
        };

        Assert.AreEqual(
            "total:    89.463.000.000$\nmilitary: 30.550.000.000$\nlobby:    800K€",
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
            "total:    n/d\nmilitary: n/d\nlobby:    n/d",
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
