using System.Collections.Generic;
using NUnit.Framework;

public class CompanyFilterTests
{
    [Test]
    public void Apply_WhenLobbyAndMilitaryOff_WhoProfitsFalse_ExcludesWhoProfitsFirms()
    {
        var companies = new[]
        {
            CompanyTestFixtures.Org(1),
            CompanyTestFixtures.Org(2, whoProfits: true)
        };

        var result = CompanyFilter.Apply(companies, default);

        Assert.AreEqual(new[] { 1 }, Ids(result));
    }

    [Test]
    public void Apply_WhenSourceIsNull_ReturnsEmptyList()
    {
        var result = CompanyFilter.Apply(null, default);

        Assert.IsEmpty(result);
    }

    [Test]
    public void Passes_WhoProfitsTrue_KeepsOnlyWhoProfitsFirms()
    {
        var criteria = WhoProfitsOnly(include: true);

        Assert.IsTrue(CompanyFilter.Passes(CompanyTestFixtures.Org(1, whoProfits: true), criteria));
        Assert.IsFalse(CompanyFilter.Passes(CompanyTestFixtures.Org(2), criteria));
    }

    [Test]
    public void Passes_WhoProfitsFalse_ExcludesWhoProfitsFirms()
    {
        var criteria = WhoProfitsOnly(include: false);

        Assert.IsTrue(CompanyFilter.Passes(CompanyTestFixtures.Org(1), criteria));
        Assert.IsFalse(CompanyFilter.Passes(CompanyTestFixtures.Org(2, whoProfits: true), criteria));
    }

    [Test]
    public void Passes_Lobbying_NullPasses_RealScoresNeedDialAboveZero()
    {
        var atZero = LobbyingOnly(0f);
        var atFifty = LobbyingOnly(50f);

        Assert.IsTrue(CompanyFilter.Passes(CompanyTestFixtures.Org(1), atZero));
        Assert.IsTrue(CompanyFilter.Passes(
            CompanyTestFixtures.Org(2, lobbyScore: 0f), atZero));
        Assert.IsFalse(CompanyFilter.Passes(
            CompanyTestFixtures.Org(6, lobbyScore: 1f), atZero));
        Assert.IsTrue(CompanyFilter.Passes(
            CompanyTestFixtures.Org(3, lobbyScore: 0f), atFifty));
        Assert.IsTrue(CompanyFilter.Passes(
            CompanyTestFixtures.Org(4, lobbyScore: 50f), atFifty));
        Assert.IsFalse(CompanyFilter.Passes(
            CompanyTestFixtures.Org(5, lobbyScore: 51f), atFifty));
    }

    [Test]
    public void Passes_Military_NullPasses_RealScoresNeedDialAboveZero()
    {
        var atZero = MilitaryOnly(0f);
        var atTwentyFive = MilitaryOnly(25f);

        Assert.IsTrue(CompanyFilter.Passes(CompanyTestFixtures.Org(1), atZero));
        Assert.IsTrue(CompanyFilter.Passes(
            CompanyTestFixtures.Org(2, militaryScore: 0f), atZero));
        Assert.IsFalse(CompanyFilter.Passes(
            CompanyTestFixtures.Org(5, militaryScore: 1f), atZero));
        Assert.IsTrue(CompanyFilter.Passes(
            CompanyTestFixtures.Org(3, militaryScore: 25f), atTwentyFive));
        Assert.IsFalse(CompanyFilter.Passes(
            CompanyTestFixtures.Org(4, militaryScore: 26f), atTwentyFive));
    }

    [Test]
    public void Passes_UsesAndAcrossEnabledCriteria()
    {
        var criteria = new CompanyFilterCriteria
        {
            FilterByLobbying = true,
            LobbyingEconomicExposureScore = 80f,
            FilterByMilitary = true,
            MilitaryEconomicExposureScore = 80f,
            FilterByWhoProfits = true
        };

        Assert.IsTrue(CompanyFilter.Passes(
            CompanyTestFixtures.Org(
                1,
                lobbyScore: 80f,
                militaryScore: 80f,
                whoProfits: true),
            criteria));
        Assert.IsTrue(CompanyFilter.Passes(
            CompanyTestFixtures.Org(2, whoProfits: true), criteria));
        Assert.IsFalse(CompanyFilter.Passes(
            CompanyTestFixtures.Org(3), criteria));
        Assert.IsFalse(CompanyFilter.Passes(
            CompanyTestFixtures.Org(4, lobbyScore: 80f), criteria));
        Assert.IsFalse(CompanyFilter.Passes(
            CompanyTestFixtures.Org(5, militaryScore: 80f), criteria));
        Assert.IsFalse(CompanyFilter.Passes(
            CompanyTestFixtures.Org(
                6,
                lobbyScore: 81f,
                militaryScore: 80f,
                whoProfits: true),
            criteria));
    }

    [Test]
    public void Apply_SortsByTotalRevenue2025AmountDescending()
    {
        var companies = new[]
        {
            new Organization
            {
                id = 1,
                matched_who_profits = true,
                total_revenue_2025_amount = 10_000_000
            },
            new Organization
            {
                id = 2,
                matched_who_profits = true,
                total_revenue_2025_amount = 90_000_000
            },
            new Organization
            {
                id = 3,
                matched_who_profits = true,
                total_revenue_2025_amount = null
            },
            new Organization
            {
                id = 4,
                matched_who_profits = true,
                total_revenue_2025_amount = 50_000_000
            }
        };

        var result = CompanyFilter.Apply(companies, WhoProfitsOnly(include: true));

        Assert.AreEqual(new[] { 2, 4, 1, 3 }, Ids(result));
    }

    [Test]
    public void Passes_NullOrganization_ReturnsFalse()
    {
        Assert.IsFalse(CompanyFilter.Passes(null, WhoProfitsOnly(include: true)));
    }

    static CompanyFilterCriteria WhoProfitsOnly(bool include)
    {
        return new CompanyFilterCriteria { FilterByWhoProfits = include };
    }

    static CompanyFilterCriteria LobbyingOnly(float threshold)
    {
        return new CompanyFilterCriteria
        {
            FilterByLobbying = true,
            LobbyingEconomicExposureScore = threshold
        };
    }

    static CompanyFilterCriteria MilitaryOnly(float threshold)
    {
        return new CompanyFilterCriteria
        {
            FilterByMilitary = true,
            MilitaryEconomicExposureScore = threshold
        };
    }

    static int[] Ids(List<Organization> companies)
    {
        var ids = new int[companies.Count];
        for (int i = 0; i < companies.Count; i++)
            ids[i] = companies[i].id;
        return ids;
    }
}
