using System.Collections.Generic;
using UnityEngine;

public class CompanyMapSpawner : MonoBehaviour
{
    [Header("Prefab Variants (pick 1 randomly per company)")]
    public List<GameObject> buildingPrefabVariants = new List<GameObject>();

    [Header("Spawn Area")]
    public BoxCollider spawnArea;

    [Header("Optional")]
    public Transform mapParent;

    [Header("Data Source")]
    public MsciWorldCompanyFilter dataSource;

    public Dictionary<int, CompanyMapObject> SpawnedByCompanyId { get; } = new Dictionary<int, CompanyMapObject>();

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

    void HandleFiltered(IReadOnlyList<MsciWorldCompanyFilter.Organization> companies)
    {
        FilteredCompanySync.Apply(companies, SpawnedByCompanyId, SpawnOne, Despawn);
    }

    CompanyMapObject SpawnOne(MsciWorldCompanyFilter.Organization org)
    {
        if (buildingPrefabVariants.Count == 0 || spawnArea == null)
        {
            Debug.LogWarning("CompanyMapSpawner: missing prefab variants or spawn area");
            return null;
        }

        GameObject prefab = buildingPrefabVariants[Random.Range(0, buildingPrefabVariants.Count)];
        Vector3 randomPos = GetRandomPointInBounds(spawnArea.bounds);

        GameObject instance = Instantiate(prefab, randomPos, Quaternion.identity, mapParent);
        instance.name = $"Building_{org.id}_{org.company_name}";

        CompanyMapObject mapObj = instance.GetComponent<CompanyMapObject>();
        if (mapObj == null)
        {
            Debug.LogWarning($"Prefab {prefab.name} has no CompanyMapObject component attached");
            Destroy(instance);
            return null;
        }

        mapObj.Bind(org);
        CompanyRegistry.RegisterMapObject(org.id, mapObj);
        return mapObj;
    }

    void Despawn(int id, CompanyMapObject mapObj)
    {
        CompanyRegistry.UnregisterMapObject(id);
        if (mapObj != null)
            Destroy(mapObj.gameObject);
    }

    Vector3 GetRandomPointInBounds(Bounds bounds)
    {
        Vector3 point = Vector3.zero;
        int attempts = 0;
        float minDistance = 5f;

        do
        {
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float z = Random.Range(bounds.min.z, bounds.max.z);
            point = new Vector3(x, bounds.center.y, z);
            attempts++;
        }
        while (attempts < 30 && IsTooCloseToExisting(point, minDistance));

        return point;
    }

    bool IsTooCloseToExisting(Vector3 point, float minDistance)
    {
        foreach (var kvp in SpawnedByCompanyId)
        {
            if (kvp.Value != null && Vector3.Distance(kvp.Value.transform.position, point) < minDistance)
                return true;
        }
        return false;
    }
}
