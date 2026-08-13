using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompanyListSpawner : MonoBehaviour
{
    public GameObject companyCardPrefab;
    public Transform contentParent;

     public MsciWorldCompanyFilter dataSource; // drag your FilterManager GameObject her

    void OnEnable()
    {
        if (dataSource != null)
            dataSource.Filtered += HandleFiltered;
    }

    void OnDisable()
    {
        if (dataSource != null)
            dataSource.Filtered -= HandleFiltered;
    }

    IEnumerator Start()
    {
        // Wait a frame so dataSource.Start() has already run (Load + first ApplyFilter)
        yield return null;

        if (dataSource != null)
            HandleFiltered(dataSource.FilteredCompanies);
    }

    void HandleFiltered(IReadOnlyList<MsciWorldCompanyFilter.Organization> companies)
    {
        // Clear previous cards
        CompanyRegistry.ClearUICards();
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        // Spawn fresh cards for the current filtered set
        foreach (var org in companies)
        {
            GameObject card = Instantiate(companyCardPrefab, contentParent);
            CompanyCardUI ui = card.GetComponent<CompanyCardUI>();
            ui.Bind(org);
            CompanyRegistry.UICards[org.id] = ui;
        }
    }
}