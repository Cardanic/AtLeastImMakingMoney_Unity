using System.Collections.Generic;
using UnityEngine;

public class CompanyListSpawner : MonoBehaviour
{
    public GameObject companyCardPrefab;
    public Transform contentParent;
    public MsciWorldCompanyFilter dataSource;

    readonly Dictionary<int, CompanyCardUI> _spawned = new();
    FilteredCompanyListener _listener;

    void OnEnable()
    {
        _listener = new FilteredCompanyListener(dataSource, HandleFiltered);
        _listener.Subscribe();
    }

    void OnDisable()
    {
        _listener?.Unsubscribe();
        _listener = null;
    }

    void HandleFiltered(IReadOnlyList<Organization> companies)
    {
        FilteredCompanySync.Apply(companies, _spawned, SpawnOne, Despawn);

        for (int i = 0; i < companies.Count; i++)
        {
            if (_spawned.TryGetValue(companies[i].id, out var card) && card != null)
                card.transform.SetSiblingIndex(i);
        }
    }

    CompanyCardUI SpawnOne(Organization org)
    {
        if (companyCardPrefab == null || contentParent == null)
        {
            Debug.LogWarning("CompanyListSpawner: missing company card prefab or content parent");
            return null;
        }

        GameObject card = Instantiate(companyCardPrefab, contentParent);
        CompanyCardUI ui = card.GetComponent<CompanyCardUI>();
        if (ui == null)
        {
            Debug.LogWarning($"Prefab {companyCardPrefab.name} has no CompanyCardUI component attached");
            Destroy(card);
            return null;
        }

        ui.Bind(org);
        CompanyRegistry.RegisterCard(org.id, ui);
        return ui;
    }

    void Despawn(int id, CompanyCardUI ui)
    {
        CompanyRegistry.UnregisterCard(id);
        if (ui != null)
            Destroy(ui.gameObject);
    }
}
