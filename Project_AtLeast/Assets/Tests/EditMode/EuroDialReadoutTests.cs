using System.Collections.Generic;
using NUnit.Framework;

public class EuroDialReadoutTests
{
    [Test]
    public void Format_UsesCompactSuffixes()
    {
        Assert.AreEqual("4M€", CompactEuroFormat.Format(4_000_000));
        Assert.AreEqual("8M€", CompactEuroFormat.Format(8_000_000));
        Assert.AreEqual("59.5B€", CompactEuroFormat.Format(59_478_000_000));
        Assert.AreEqual("1.5K€", CompactEuroFormat.Format(1_500));
        Assert.AreEqual("500€", CompactEuroFormat.Format(500));
    }

    [Test]
    public void CenterLabel_AtZero_ShowsNdNotZeroEuro()
    {
        Assert.AreEqual("n/d", EuroDialReadout.CenterLabel(0f, 0f, 100f, 8_000_000));
    }

    [Test]
    public void CenterLabel_MapsLinearlyAgainstDatasetMax()
    {
        Assert.AreEqual("4M€", EuroDialReadout.CenterLabel(50f, 0f, 100f, 8_000_000));
        Assert.AreEqual("8M€", EuroDialReadout.CenterLabel(100f, 0f, 100f, 8_000_000));
    }

    [Test]
    public void MaxOf_IgnoresNullsAndPicksHighest()
    {
        var companies = new List<Organization>
        {
            new Organization { lobbying_cost_EU = null },
            new Organization { lobbying_cost_EU = 100_000 },
            new Organization { lobbying_cost_EU = 8_000_000 },
            new Organization { lobbying_cost_EU = 2_000_000 }
        };

        Assert.AreEqual(8_000_000, EuroDialReadout.MaxOf(companies, o => o.lobbying_cost_EU));
        Assert.AreEqual(0.0, EuroDialReadout.MaxOf(companies, o => o.MilitaryRevenue2024Numeric));
    }

    [Test]
    public void MaxOf_UsesMilitaryAmountWhenEuroFieldIsMissing()
    {
        var companies = new List<Organization>
        {
            new Organization { military_revenue_2024_amount = 30_550_000_000 },
            new Organization { military_revenue_2024_amount = 64_650_000_000 }
        };

        Assert.AreEqual(64_650_000_000, EuroDialReadout.MaxOf(companies, o => o.MilitaryRevenue2024Numeric));
        Assert.AreEqual("32.3B$", EuroDialReadout.CenterLabel(50f, 0f, 100f, 64_650_000_000, "n/d", "$"));
    }

    [Test]
    public void ToHundred_ScalesUnitIntervalScoresOntoTheDial()
    {
        Assert.AreEqual(10f, ExposureScore.ToHundred(0.1f));
        Assert.AreEqual(100f, ExposureScore.ToHundred(1f));
        Assert.AreEqual(52f, ExposureScore.ToHundred(52f));
        Assert.IsNull(ExposureScore.ToHundred(null));
    }
}
