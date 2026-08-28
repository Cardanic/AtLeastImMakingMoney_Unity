using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CompanyPathLine : MonoBehaviour
{
    [Header("References")]
    public CompanyMapSpawner spawner;

    [Header("Line Look")]
    [Tooltip("Lift the line slightly above ground/building base so it doesn't clip.")]
    public float verticalOffset = 0.5f;
    [Tooltip("If true, connects the last building back to the first (closed loop).")]
    public bool closeLoop = false;

    LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 0;
    }

    void OnEnable()
    {
        if (spawner != null)
        {
            spawner.Updated += Rebuild;
            Rebuild(); // in case buildings already exist when this enables
        }
    }

    void OnDisable()
    {
        if (spawner != null)
            spawner.Updated -= Rebuild;
    }

    void Rebuild()
    {
        List<Vector3> positions = spawner.GetSpawnedPositionsOrdered();

        if (positions.Count < 2)
        {
            // 0 or 1 building: nothing meaningful to connect, hide the line.
            line.positionCount = 0;
            return;
        }

        int count = closeLoop ? positions.Count + 1 : positions.Count;
        line.positionCount = count;

        for (int i = 0; i < positions.Count; i++)
            line.SetPosition(i, positions[i] + Vector3.up * verticalOffset);

        if (closeLoop)
            line.SetPosition(positions.Count, positions[0] + Vector3.up * verticalOffset);
    }
}