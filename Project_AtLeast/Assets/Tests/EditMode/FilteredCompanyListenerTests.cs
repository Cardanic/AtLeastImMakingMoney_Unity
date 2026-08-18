using System;
using System.Collections.Generic;
using NUnit.Framework;

public class FilteredCompanyListenerTests
{
    [Test]
    public void Subscribe_WhenFilterHasNotRun_DoesNotCatchUp()
    {
        var source = new FakeFilterSource { HasFiltered = false };
        int calls = 0;

        var listener = new FilteredCompanyListener(source, _ => calls++);
        listener.Subscribe();

        Assert.AreEqual(0, calls);
    }

    [Test]
    public void Subscribe_WhenFilterAlreadyRan_AppliesCurrentResult()
    {
        var companies = new[] { CompanyTestFixtures.Org(7) };
        var source = new FakeFilterSource
        {
            HasFiltered = true,
            FilteredCompanies = companies
        };
        IReadOnlyList<Organization> received = null;

        var listener = new FilteredCompanyListener(source, list => received = list);
        listener.Subscribe();

        Assert.AreSame(companies, received);
    }

    [Test]
    public void Subscribe_ForwardsLaterFilterEvents()
    {
        var source = new FakeFilterSource();
        var received = new List<IReadOnlyList<Organization>>();
        var listener = new FilteredCompanyListener(source, received.Add);

        listener.Subscribe();
        var first = new[] { CompanyTestFixtures.Org(1) };
        var second = new[] { CompanyTestFixtures.Org(2) };
        source.Raise(first);
        source.Raise(second);

        Assert.AreEqual(2, received.Count);
        Assert.AreSame(first, received[0]);
        Assert.AreSame(second, received[1]);
    }

    [Test]
    public void Unsubscribe_StopsReceivingFilterEvents()
    {
        var source = new FakeFilterSource();
        int calls = 0;
        var listener = new FilteredCompanyListener(source, _ => calls++);

        listener.Subscribe();
        listener.Unsubscribe();
        source.Raise(new[] { CompanyTestFixtures.Org(1) });

        Assert.AreEqual(0, calls);
    }

    [Test]
    public void Subscribe_WithNullSource_DoesNotThrow()
    {
        var listener = new FilteredCompanyListener(null, _ => Assert.Fail("Should not run."));
        Assert.DoesNotThrow(() => listener.Subscribe());
        Assert.DoesNotThrow(() => listener.Unsubscribe());
    }

    sealed class FakeFilterSource : ICompanyFilterSource
    {
        public IReadOnlyList<Organization> FilteredCompanies { get; set; } = Array.Empty<Organization>();
        public bool HasFiltered { get; set; }
        public event Action<IReadOnlyList<Organization>> Filtered;

        public void Raise(IReadOnlyList<Organization> companies)
        {
            FilteredCompanies = companies;
            HasFiltered = true;
            Filtered?.Invoke(companies);
        }
    }
}
