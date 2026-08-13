using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompanyMapSpawner : MonoBehaviour
{
    [Header("Prefab Variants (pick 1 randomly per company)")]
    public List<GameObject> buildingPrefabVariants = new List<GameObject>(); // size 3

    [Header("Spawn Area")]
    public BoxCollider spawnArea; // drag MapSpawnArea here

    [Header("Optional")]
    public Transform mapParent; // organizes spawned objects under one GameObject in Hierarchy

    [Header("Data Source")]
    public MsciWorldCompanyFilter dataSource;

    public Dictionary<int, CompanyMapObject> SpawnedByCompanyId { get; private set; } = new Dictionary<int, CompanyMapObject>();

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
        yield return null;

        if (dataSource != null)
            HandleFiltered(dataSource.FilteredCompanies);
    }

    void HandleFiltered(IReadOnlyList<MsciWorldCompanyFilter.Organization> companies)
    {
        // Clear previous buildings
        CompanyRegistry.ClearMapObjects();
        foreach (var kvp in SpawnedByCompanyId)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }
        SpawnedByCompanyId.Clear();

        foreach (var org in companies)
            SpawnOne(org);
    }

    void SpawnOne(MsciWorldCompanyFilter.Organization org)
    {
        if (buildingPrefabVariants.Count == 0 || spawnArea == null)
        {
            Debug.LogWarning("CompanyMapSpawner: missing prefab variants or spawn area");
            return;
        }

        GameObject prefab = buildingPrefabVariants[Random.Range(0, buildingPrefabVariants.Count)];
        Vector3 randomPos = GetRandomPointInBounds(spawnArea.bounds);

        GameObject instance = Instantiate(prefab, randomPos, Quaternion.identity, mapParent);
        instance.name = $"Building_{org.id}_{org.company_name}";

        CompanyMapObject mapObj = instance.GetComponent<CompanyMapObject>();
        if (mapObj != null)
        {
            mapObj.Bind(org);
            SpawnedByCompanyId[org.id] = mapObj;
            CompanyRegistry.MapObjects[org.id] = mapObj;
        }
        else
        {
            Debug.LogWarning($"Prefab {prefab.name} has no CompanyMapObject component attached");
        }
    }

    Vector3 GetRandomPointInBounds(Bounds bounds)
    {
        Vector3 point = Vector3.zero;
        int attempts = 0;
        float minDistance = 5f; // tune to your building size

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
            if (Vector3.Distance(kvp.Value.transform.position, point) < minDistance)
                return true;
        }
        return false;
    }
}