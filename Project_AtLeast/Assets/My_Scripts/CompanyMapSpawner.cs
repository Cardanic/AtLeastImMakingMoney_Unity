using System;
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

    /// <summary>Fires after SpawnedByCompanyId has finished changing (spawns/despawns applied).</summary>
    public event Action Updated;

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
        FilteredCompanySync.Apply(companies, SpawnedByCompanyId, SpawnOne, Despawn);
        Updated?.Invoke();
        int idx = UnityEngine.Random.Range(0, buildingPrefabVariants.Count);
GameObject prefab = buildingPrefabVariants[idx];
Debug.Log($"Picked variant {idx}/{buildingPrefabVariants.Count - 1}: {prefab?.name ?? "NULL"}");
    }

    CompanyMapObject SpawnOne(Organization org)
    {
        if (buildingPrefabVariants.Count == 0 || spawnArea == null)
        {
            Debug.LogWarning("CompanyMapSpawner: missing prefab variants or spawn area");
            return null;
        }

        GameObject prefab = buildingPrefabVariants[UnityEngine.Random.Range(0, buildingPrefabVariants.Count)];
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
            float x = UnityEngine.Random.Range(bounds.min.x, bounds.max.x);
            float z = UnityEngine.Random.Range(bounds.min.z, bounds.max.z);
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

    public List<Vector3> GetSpawnedPositions()
    {
        var positions = new List<Vector3>(SpawnedByCompanyId.Count);
        foreach (var kvp in SpawnedByCompanyId)
        {
            if (kvp.Value != null)
                positions.Add(kvp.Value.transform.position);
        }
        return positions;
    }

    public List<Vector3> GetSpawnedPositionsOrdered()
    {
        var keys = new List<int>(SpawnedByCompanyId.Keys);
        keys.Sort();

        var positions = new List<Vector3>(keys.Count);
        foreach (int key in keys)
        {
            var mapObj = SpawnedByCompanyId[key];
            if (mapObj != null)
                positions.Add(mapObj.transform.position);
        }
        return positions;
    }
}