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
        Assert.AreEqual(0.0, EuroDialReadout.MaxOf(companies, o => o.military_revenue_2024_euro));
    }
}
