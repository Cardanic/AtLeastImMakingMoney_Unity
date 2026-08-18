using System;
using System.Collections.Generic;

public interface ICompanyFilterSource
{
    IReadOnlyList<Organization> FilteredCompanies { get; }
    bool HasFiltered { get; }
    event Action<IReadOnlyList<Organization>> Filtered;
}
