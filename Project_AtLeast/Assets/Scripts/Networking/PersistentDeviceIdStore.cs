using UnityEngine;

/// <summary>
/// Stable per-install phone id. Outer-adapter: Unity PlayerPrefs.
/// </summary>
public static class PersistentDeviceIdStore
{
    const string PrefsKey = "CompanyIdUdpReceiver.DeviceId";

    public static string GetOrCreate()
    {
        string id = PlayerPrefs.GetString(PrefsKey, "");
        if (!string.IsNullOrEmpty(id))
            return id;

        id = System.Guid.NewGuid().ToString("N");
        PlayerPrefs.SetString(PrefsKey, id);
        PlayerPrefs.Save();
        return id;
    }
}
