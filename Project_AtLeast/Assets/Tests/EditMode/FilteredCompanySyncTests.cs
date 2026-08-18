using System;
using System.Collections.Generic;
using NUnit.Framework;

public class FilteredCompanySyncTests
{
    [Test]
    public void Apply_SpawnsMissingCompanies()
    {
        var spawned = new Dictionary<int, string>();
        var spawnedIds = new List<int>();

        FilteredCompanySync.Apply(
            new[] { CompanyTestFixtures.Org(1), CompanyTestFixtures.Org(2) },
            spawned,
            org =>
            {
                spawnedIds.Add(org.id);
                return $"instance-{org.id}";
            },
            (_, __) => Assert.Fail("Should not despawn on first apply."));

        Assert.AreEqual(new[] { 1, 2 }, spawnedIds);
        Assert.AreEqual("instance-1", spawned[1]);
        Assert.AreEqual("instance-2", spawned[2]);
    }

    [Test]
    public void Apply_ReusesExistingInstances()
    {
        var spawned = new Dictionary<int, string>();
        int spawnCount = 0;
        var original = "keep-me";
        spawned[1] = original;

        FilteredCompanySync.Apply(
            new[] { CompanyTestFixtures.Org(1) },
            spawned,
            _ =>
            {
                spawnCount++;
                return "new";
            },
            (_, __) => Assert.Fail("Should not despawn a company that is still filtered in."));

        Assert.AreEqual(0, spawnCount);
        Assert.AreSame(original, spawned[1]);
    }

    [Test]
    public void Apply_DespawnsCompaniesThatLeftTheFilter()
    {
        var spawned = new Dictionary<int, string>
        {
            [1] = "keep",
            [2] = "drop"
        };
        var despawned = new List<int>();

        FilteredCompanySync.Apply(
            new[] { CompanyTestFixtures.Org(1) },
            spawned,
            _ =>
            {
                Assert.Fail("Should not spawn when the remaining company already exists.");
                return "new";
            },
            (id, instance) =>
            {
                despawned.Add(id);
                Assert.AreEqual("drop", instance);
            });

        Assert.AreEqual(new[] { 2 }, despawned);
        Assert.IsFalse(spawned.ContainsKey(2));
        Assert.AreEqual("keep", spawned[1]);
    }

    [Test]
    public void Apply_AddsAndRemovesInOnePass()
    {
        var spawned = new Dictionary<int, string> { [1] = "old-1", [2] = "old-2" };

        FilteredCompanySync.Apply(
            new[] { CompanyTestFixtures.Org(2), CompanyTestFixtures.Org(3) },
            spawned,
            org => $"new-{org.id}",
            (_, __) => { });

        CollectionAssert.AreEquivalent(new[] { 2, 3 }, spawned.Keys);
        Assert.AreEqual("old-2", spawned[2]);
        Assert.AreEqual("new-3", spawned[3]);
    }

    [Test]
    public void Apply_WhenSpawnReturnsNull_DoesNotRecordTheCompany()
    {
        var spawned = new Dictionary<int, string>();

        FilteredCompanySync.Apply(
            new[] { CompanyTestFixtures.Org(1) },
            spawned,
            _ => null,
            (_, __) => { });

        Assert.IsEmpty(spawned);

        FilteredCompanySync.Apply(
            new[] { CompanyTestFixtures.Org(1) },
            spawned,
            _ => "created-later",
            (_, __) => { });

        Assert.AreEqual("created-later", spawned[1]);
    }

    [Test]
    public void Apply_EmptyFilter_DespawnsEverything()
    {
        var spawned = new Dictionary<int, string> { [1] = "a", [2] = "b" };
        var despawned = new List<int>();

        FilteredCompanySync.Apply(
            Array.Empty<Organization>(),
            spawned,
            _ => "new",
            (id, _) => despawned.Add(id));

        CollectionAssert.AreEquivalent(new[] { 1, 2 }, despawned);
        Assert.IsEmpty(spawned);
    }
}
