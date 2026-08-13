using System.Collections.Generic;
using UnityEngine;

public class CompanyDataLoader : MonoBehaviour
{
    public static CompanyDataLoader Instance;

    [Tooltip("Filename inside a Resources folder, WITHOUT the .json extension")]
    public string jsonFileName = "Data/companies";

    public List<CompanyMetrics> Companies { get; private set; } = new List<CompanyMetrics>();

    void Awake()
    {
        // Simple singleton so other scripts can reach this via CompanyDataLoader.Instance
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadData();
    }

    void LoadData()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(jsonFileName);
        if (jsonFile == null)
        {
            Debug.LogError($"Could not find {jsonFileName}.json in a Resources folder");
            return;
        }

        CompanyDataRoot root = JsonUtility.FromJson<CompanyDataRoot>(jsonFile.text);

        Companies.Clear();
        foreach (var entry in root.data)
            Companies.Add(CompanyMetrics.FromRaw(entry));

        Debug.Log($"CompanyDataLoader: loaded {Companies.Count} companies");
    }
}