using System;
using System.Collections.Generic;

/// <summary>
/// Subscribes to <see cref="MsciWorldCompanyFilter"/> and catches up if the filter
/// already ran (listeners enabled after <c>Start</c>).
/// </summary>
public sealed class FilteredCompanyListener
{
    readonly MsciWorldCompanyFilter _source;
    readonly Action<IReadOnlyList<MsciWorldCompanyFilter.Organization>> _handler;

    public FilteredCompanyListener(
        MsciWorldCompanyFilter source,
        Action<IReadOnlyList<MsciWorldCompanyFilter.Organization>> handler)
    {
        _source = source;
        _handler = handler;
    }

    public void Subscribe()
    {
        if (_source == null)
            return;

        _source.Filtered += _handler;
        if (_source.HasFiltered)
            _handler(_source.FilteredCompanies);
    }

    public void Unsubscribe()
    {
        if (_source == null)
            return;

        _source.Filtered -= _handler;
    }
}

/// <summary>
/// Keeps an id-keyed spawn dictionary in sync with a filtered company list.
/// Existing instances are reused; only companies that left or entered the set are despawned or spawned.
/// </summary>
public static class FilteredCompanySync
{
    public static void Apply<T>(
        IReadOnlyList<MsciWorldCompanyFilter.Organization> companies,
        Dictionary<int, T> spawned,
        Func<MsciWorldCompanyFilter.Organization, T> spawn,
        Action<int, T> despawn)
        where T : class
    {
        var wanted = new HashSet<int>();
        for (int i = 0; i < companies.Count; i++)
            wanted.Add(companies[i].id);

        var stale = new List<int>();
        foreach (var id in spawned.Keys)
        {
            if (!wanted.Contains(id))
                stale.Add(id);
        }

        for (int i = 0; i < stale.Count; i++)
        {
            int id = stale[i];
            despawn(id, spawned[id]);
            spawned.Remove(id);
        }

        for (int i = 0; i < companies.Count; i++)
        {
            var org = companies[i];
            if (spawned.ContainsKey(org.id))
                continue;

            var instance = spawn(org);
            if (instance != null)
                spawned[org.id] = instance;
        }
    }
}
