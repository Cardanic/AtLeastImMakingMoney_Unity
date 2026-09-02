using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CompanyListSpawner : MonoBehaviour
{
    public GameObject companyCardPrefab;
    public Transform contentParent;
    public MsciWorldCompanyFilter dataSource;

    readonly Dictionary<int, CompanyCardUI> _spawned = new();
    FilteredCompanyListener _listener;
    PortfolioAutoScroll _autoScroll;

    void OnEnable()
    {
        if (contentParent != null)
            _autoScroll = contentParent.GetComponentInParent<PortfolioAutoScroll>();

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

        _autoScroll?.ResetToTop();
    }

    CompanyCardUI SpawnOne(Organization org)
    {
        if (companyCardPrefab == null || contentParent == null)
        {
            Debug.LogWarning("CompanyListSpawner: missing company card prefab or content parent");
            return null;
        }

        GameObject card = Instantiate(companyCardPrefab, contentParent);
        StretchToContentWidth(card);
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

    static void StretchToContentWidth(GameObject card)
    {
        var rect = card.transform as RectTransform;
        if (rect == null)
            return;

        rect.localScale = Vector3.one;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, rect.sizeDelta.y);

        var layout = card.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.flexibleWidth = 1f;
            if (layout.preferredHeight <= 0f)
                layout.preferredHeight = 120f;
        }
    }

    void Despawn(int id, CompanyCardUI ui)
    {
        CompanyRegistry.UnregisterCard(id);
        if (ui != null)
            Destroy(ui.gameObject);
    }
}
