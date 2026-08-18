using System.Collections.Generic;
using NUnit.Framework;

public class CompanyFilterTests
{
    [Test]
    public void Apply_WhenNoCriteriaEnabled_KeepsEveryCompany()
    {
        var companies = new[]
        {
            CompanyTestFixtures.Org(1),
            CompanyTestFixtures.Org(2, whoProfits: true)
        };

        var result = CompanyFilter.Apply(companies, default);

        Assert.AreEqual(2, result.Count);
    }

    [Test]
    public void Apply_WhenSourceIsNull_ReturnsEmptyList()
    {
        var result = CompanyFilter.Apply(null, default);

        Assert.IsEmpty(result);
    }

    [Test]
    public void Passes_WhoProfitsOnly_KeepsMatchingCompanies()
    {
        var criteria = WhoProfitsOnly();

        Assert.IsTrue(CompanyFilter.Passes(CompanyTestFixtures.Org(1, whoProfits: true), criteria));
        Assert.IsFalse(CompanyFilter.Passes(CompanyTestFixtures.Org(2), criteria));
    }

    [Test]
    public void Passes_Lobbying_RequiresMatchFlagAndScoreAtOrAboveThreshold()
    {
        var criteria = LobbyingOnly(0.5f);

        Assert.IsTrue(CompanyFilter.Passes(
            CompanyTestFixtures.Org(1, lobby: true, lobbyScore: 0.5f), criteria));
        Assert.IsTrue(CompanyFilter.Passes(
            CompanyTestFixtures.Org(2, lobby: true, lobbyScore: 0.9f), criteria));
        Assert.IsFalse(CompanyFilter.Passes(
            CompanyTestFixtures.Org(3, lobby: true, lobbyScore: 0.49f), criteria));
        Assert.IsFalse(CompanyFilter.Passes(
            CompanyTestFixtures.Org(4, lobby: false, lobbyScore: 1f), criteria));
        Assert.IsFalse(CompanyFilter.Passes(
            CompanyTestFixtures.Org(5, lobby: true), criteria));
    }

    [Test]
    public void Passes_Military_RequiresMatchFlagAndScoreAtOrAboveThreshold()
    {
        var criteria = MilitaryOnly(0.25f);

        Assert.IsTrue(CompanyFilter.Passes(
            CompanyTestFixtures.Org(1, military: true, militaryScore: 0.25f), criteria));
        Assert.IsFalse(CompanyFilter.Passes(
            CompanyTestFixtures.Org(2, military: true, militaryScore: 0.24f), criteria));
        Assert.IsFalse(CompanyFilter.Passes(
            CompanyTestFixtures.Org(3, military: false, militaryScore: 1f), criteria));
    }

    [Test]
    public void Passes_UsesOrAcrossEnabledCriteria()
    {
        var criteria = new CompanyFilterCriteria
        {
            FilterByLobbying = true,
            LobbyingEconomicExposureScore = 0.8f,
            FilterByMilitary = true,
            MilitaryEconomicExposureScore = 0.8f,
            FilterByWhoProfits = true
        };

        Assert.IsTrue(CompanyFilter.Passes(
            CompanyTestFixtures.Org(1, whoProfits: true), criteria));
        Assert.IsTrue(CompanyFilter.Passes(
            CompanyTestFixtures.Org(2, lobby: true, lobbyScore: 0.8f), criteria));
        Assert.IsTrue(CompanyFilter.Passes(
            CompanyTestFixtures.Org(3, military: true, militaryScore: 0.8f), criteria));
        Assert.IsFalse(CompanyFilter.Passes(CompanyTestFixtures.Org(4), criteria));
    }

    [Test]
    public void Apply_PreservesSourceOrder()
    {
        var companies = new[]
        {
            CompanyTestFixtures.Org(3, whoProfits: true),
            CompanyTestFixtures.Org(1),
            CompanyTestFixtures.Org(2, whoProfits: true)
        };

        var result = CompanyFilter.Apply(companies, WhoProfitsOnly());

        Assert.AreEqual(new[] { 3, 2 }, Ids(result));
    }

    [Test]
    public void Passes_NullOrganization_ReturnsFalse()
    {
        Assert.IsFalse(CompanyFilter.Passes(null, WhoProfitsOnly()));
    }

    static CompanyFilterCriteria WhoProfitsOnly()
    {
        return new CompanyFilterCriteria { FilterByWhoProfits = true };
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
