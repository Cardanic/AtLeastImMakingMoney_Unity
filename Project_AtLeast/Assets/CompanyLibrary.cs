// CompanyLibrary.cs — attach to a persistent GameObject in both projects
using System.Collections.Generic;
using UnityEngine;

public class CompanyLibrary : MonoBehaviour
{
    public static CompanyLibrary Instance;
    
    private Dictionary<int, CompanyEntry> _companies = new();

    void Awake()
    {
        Instance = this;
        LoadJSON();
    }

    void LoadJSON()
    {
        TextAsset json = Resources.Load<TextAsset>("companies");
        var db = JsonUtility.FromJson<CompanyDatabase>(json.text);

        foreach (var company in db.data)
            _companies[company.id] = company;

        Debug.Log($"[CompanyLibrary] Loaded {_companies.Count} companies.");
    }

    public CompanyEntry Get(int id)
    {
        _companies.TryGetValue(id, out var entry);
        return entry;
    }
}