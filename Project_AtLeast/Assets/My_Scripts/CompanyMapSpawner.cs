using System;
using System.Collections.Generic;
using UnityEngine;

public class CompanyMapSpawner : MonoBehaviour
{
    [Header("Prefab Variants (pick 1 randomly per company)")]
    public List<GameObject> buildingPrefabVariants = new List<GameObject>();

    [Header("Spawn Area")]
    public BoxCollider spawnArea;

    [Header("Spacing")]
    [Tooltip("Minimum horizontal distance between building centers.")]
    public float minSpacing = 22f;

    [Tooltip("Keep buildings this far inside the spawn area edges.")]
    public float edgePadding = 8f;

    [Tooltip("Random offset within a grid cell so placement does not look perfectly rigid.")]
    [Range(0f, 0.45f)]
    public float cellJitter = 0.3f;

    [Tooltip("Random placement attempts before falling back to the least-crowded spot.")]
    public int maxPlacementAttempts = 80;

    [Header("Optional")]
    public Transform mapParent;

    [Header("Data Source")]
    public MsciWorldCompanyFilter dataSource;

    public Dictionary<int, CompanyMapObject> SpawnedByCompanyId { get; } = new Dictionary<int, CompanyMapObject>();

    /// <summary>Fires after SpawnedByCompanyId has finished changing (spawns/despawns applied).</summary>
    public event Action Updated;

    FilteredCompanyListener _listener;
    readonly List<Vector3> _gridSlots = new();
    readonly List<Vector3> _availableSlots = new();
    float _cachedSpacing = -1f;
    float _cachedPadding = -1f;
    Bounds _cachedBounds;

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
    }

    CompanyMapObject SpawnOne(Organization org)
    {
        if (buildingPrefabVariants.Count == 0 || spawnArea == null)
        {
            Debug.LogWarning("CompanyMapSpawner: missing prefab variants or spawn area");
            return null;
        }

        GameObject prefab = buildingPrefabVariants[UnityEngine.Random.Range(0, buildingPrefabVariants.Count)];
        Vector3 pos = FindNonOverlappingPosition(spawnArea.bounds);

        GameObject instance = Instantiate(prefab, pos, Quaternion.identity, mapParent);
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

    Vector3 FindNonOverlappingPosition(Bounds bounds)
    {
        EnsureGrid(bounds);

        // Prefer unused grid slots — even spacing, no overlap by construction.
        RebuildAvailableSlots();
        if (_availableSlots.Count > 0)
        {
            int pick = UnityEngine.Random.Range(0, _availableSlots.Count);
            Vector3 slot = _availableSlots[pick];
            Vector3 jittered = ApplyJitter(slot, bounds);
            // Jitter can pull toward a neighbor; keep the raw slot if that would breach spacing.
            if (MinHorizontalClearance(jittered) >= minSpacing)
                return jittered;
            return ClampToArea(slot, bounds);
        }

        // Area is denser than the grid: try random points, then least-crowded fallback.
        Vector3 best = SampleRandomPoint(bounds);
        float bestClearance = MinHorizontalClearance(best);
        int attempts = Mathf.Max(1, maxPlacementAttempts);

        for (int i = 0; i < attempts; i++)
        {
            Vector3 candidate = SampleRandomPoint(bounds);
            float clearance = MinHorizontalClearance(candidate);
            if (clearance >= minSpacing)
                return candidate;

            if (clearance > bestClearance)
            {
                bestClearance = clearance;
                best = candidate;
            }
        }

        return best;
    }

    void EnsureGrid(Bounds bounds)
    {
        if (_gridSlots.Count > 0
            && Mathf.Approximately(_cachedSpacing, minSpacing)
            && Mathf.Approximately(_cachedPadding, edgePadding)
            && _cachedBounds.center == bounds.center
            && _cachedBounds.size == bounds.size)
            return;

        _gridSlots.Clear();
        _cachedSpacing = minSpacing;
        _cachedPadding = edgePadding;
        _cachedBounds = bounds;

        float spacing = Mathf.Max(1f, minSpacing);
        float pad = Mathf.Max(0f, edgePadding);
        float minX = bounds.min.x + pad;
        float maxX = bounds.max.x - pad;
        float minZ = bounds.min.z + pad;
        float maxZ = bounds.max.z - pad;
        float y = bounds.center.y;

        if (maxX <= minX || maxZ <= minZ)
        {
            _gridSlots.Add(new Vector3(bounds.center.x, y, bounds.center.z));
            return;
        }

        for (float x = minX; x <= maxX + 0.001f; x += spacing)
        {
            for (float z = minZ; z <= maxZ + 0.001f; z += spacing)
                _gridSlots.Add(new Vector3(x, y, z));
        }
    }

    void RebuildAvailableSlots()
    {
        _availableSlots.Clear();
        float minSqr = minSpacing * minSpacing;

        foreach (Vector3 slot in _gridSlots)
        {
            bool free = true;
            foreach (var kvp in SpawnedByCompanyId)
            {
                if (kvp.Value == null)
                    continue;

                Vector3 existing = kvp.Value.transform.position;
                float dx = existing.x - slot.x;
                float dz = existing.z - slot.z;
                if (dx * dx + dz * dz < minSqr)
                {
                    free = false;
                    break;
                }
            }

            if (free)
                _availableSlots.Add(slot);
        }
    }

    Vector3 ApplyJitter(Vector3 slot, Bounds bounds)
    {
        float maxJitter = minSpacing * Mathf.Clamp01(cellJitter);
        if (maxJitter <= 0f)
            return ClampToArea(slot, bounds);

        Vector3 jittered = new Vector3(
            slot.x + UnityEngine.Random.Range(-maxJitter, maxJitter),
            slot.y,
            slot.z + UnityEngine.Random.Range(-maxJitter, maxJitter));

        return ClampToArea(jittered, bounds);
    }

    Vector3 SampleRandomPoint(Bounds bounds)
    {
        float pad = Mathf.Max(0f, edgePadding);
        float minX = bounds.min.x + pad;
        float maxX = bounds.max.x - pad;
        float minZ = bounds.min.z + pad;
        float maxZ = bounds.max.z - pad;

        if (maxX <= minX || maxZ <= minZ)
            return new Vector3(bounds.center.x, bounds.center.y, bounds.center.z);

        return new Vector3(
            UnityEngine.Random.Range(minX, maxX),
            bounds.center.y,
            UnityEngine.Random.Range(minZ, maxZ));
    }

    Vector3 ClampToArea(Vector3 point, Bounds bounds)
    {
        float pad = Mathf.Max(0f, edgePadding);
        point.x = Mathf.Clamp(point.x, bounds.min.x + pad, bounds.max.x - pad);
        point.z = Mathf.Clamp(point.z, bounds.min.z + pad, bounds.max.z - pad);
        point.y = bounds.center.y;
        return point;
    }

    float MinHorizontalClearance(Vector3 point)
    {
        float best = float.PositiveInfinity;
        foreach (var kvp in SpawnedByCompanyId)
        {
            if (kvp.Value == null)
                continue;

            Vector3 existing = kvp.Value.transform.position;
            float dx = existing.x - point.x;
            float dz = existing.z - point.z;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);
            if (dist < best)
                best = dist;
        }

        return float.IsPositiveInfinity(best) ? float.PositiveInfinity : best;
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
