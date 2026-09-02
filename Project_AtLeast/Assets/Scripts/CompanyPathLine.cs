using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CompanyPathLine : MonoBehaviour
{
    [Header("References")]
    public CompanyMapSpawner spawner;

    [Header("Line Look")]
    [Tooltip("World-space width of each match-group line.")]
    public float lineWidth = 0.04f;
    [Tooltip("Lift the line slightly above ground/building base so it doesn't clip.")]
    public float verticalOffset = 0.5f;
    [Tooltip("If true, closes each match-group path back to its first building.")]
    public bool closeLoop = false;

    LineRenderer _template;
    readonly Dictionary<MatchKind, LineRenderer> _linesByKind = new();

    enum MatchKind
    {
        LobbyFacts,
        Sipri,
        WhoProfits
    }

    void Awake()
    {
        _template = GetComponent<LineRenderer>();
        _template.enabled = false;
        _template.positionCount = 0;
    }

    void OnEnable()
    {
        if (spawner != null)
        {
            spawner.Updated += Rebuild;
            Rebuild();
        }
    }

    void OnDisable()
    {
        if (spawner != null)
            spawner.Updated -= Rebuild;
    }

    void Rebuild()
    {
        if (spawner == null)
            return;

        RebuildGroup(MatchKind.LobbyFacts, org => org.matched_lobbyfacts);
        RebuildGroup(MatchKind.Sipri, org => org.matched_sipri);
        RebuildGroup(MatchKind.WhoProfits, org => org.matched_who_profits);
    }

    void RebuildGroup(MatchKind kind, Func<Organization, bool> hasMatch)
    {
        var keys = new List<int>();
        foreach (var kvp in spawner.SpawnedByCompanyId)
        {
            CompanyMapObject mapObj = kvp.Value;
            if (mapObj == null || mapObj.BoundData == null || !hasMatch(mapObj.BoundData))
                continue;

            keys.Add(kvp.Key);
        }

        keys.Sort();

        var positions = new List<Vector3>(keys.Count);
        foreach (int key in keys)
        {
            CompanyMapObject mapObj = spawner.SpawnedByCompanyId[key];
            if (mapObj == null)
                continue;

            positions.Add(mapObj.transform.position + Vector3.up * verticalOffset);
        }

        LineRenderer line = GetOrCreateLine(kind);
        if (positions.Count < 2)
        {
            line.positionCount = 0;
            line.enabled = false;
            return;
        }

        line.enabled = true;
        int count = closeLoop ? positions.Count + 1 : positions.Count;
        line.positionCount = count;

        for (int i = 0; i < positions.Count; i++)
            line.SetPosition(i, positions[i]);

        if (closeLoop)
            line.SetPosition(positions.Count, positions[0]);
    }

    LineRenderer GetOrCreateLine(MatchKind kind)
    {
        if (_linesByKind.TryGetValue(kind, out LineRenderer existing))
            return existing;

        var child = new GameObject($"Line_{kind}");
        child.transform.SetParent(transform, false);

        LineRenderer line = child.AddComponent<LineRenderer>();
        CopyLineSettings(_template, line);
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.useWorldSpace = true;
        line.positionCount = 0;

        _linesByKind[kind] = line;
        return line;
    }

    static void CopyLineSettings(LineRenderer from, LineRenderer to)
    {
        to.materials = from.materials;
        to.colorGradient = from.colorGradient;
        to.shadowCastingMode = from.shadowCastingMode;
        to.receiveShadows = from.receiveShadows;
        to.lightProbeUsage = from.lightProbeUsage;
        to.reflectionProbeUsage = from.reflectionProbeUsage;
        to.alignment = from.alignment;
        to.textureMode = from.textureMode;
        to.numCornerVertices = from.numCornerVertices;
        to.numCapVertices = from.numCapVertices;
        to.generateLightingData = from.generateLightingData;
    }
}
