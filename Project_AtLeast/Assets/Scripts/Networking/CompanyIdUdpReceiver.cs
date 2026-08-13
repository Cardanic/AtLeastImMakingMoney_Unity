using UnityEngine;

/// <summary>
/// Phone composition root. Copy this file plus protocol, transport, broadcast directory,
/// device-id store, and <see cref="PhoneExhibitClient"/> into the phone Unity app.
/// Start this before the exhibit PC so the sender can find the phone.
/// </summary>
public sealed class CompanyIdUdpReceiver : MonoBehaviour
{
    [SerializeField]
    int port = CompanyIdProtocol.DefaultPort;

    [SerializeField, Min(0.25f)]
    float announceIntervalSeconds = 1f;

    [SerializeField]
    bool logReceives = true;

    PhoneExhibitClient _client;

    public event System.Action<int[]> IdsReceived;

    public int[] LastIds => _client != null ? _client.LastIds : System.Array.Empty<int>();
    public string DeviceId => _client != null ? _client.DeviceId : "";
    public int PhoneSlot => _client != null ? _client.PhoneSlot : 0;

    void OnEnable()
    {
        Application.runInBackground = true;
        _client = new PhoneExhibitClient(
            new PhoneExhibitClient.Settings
            {
                Port = port,
                AnnounceIntervalSeconds = announceIntervalSeconds,
                LogReceives = logReceives
            },
            PersistentDeviceIdStore.GetOrCreate(),
            message => Debug.Log($"{nameof(CompanyIdUdpReceiver)}: {message}")
        );
        _client.CompanyIdsReceived += OnCompanyIdsReceived;
        _client.Start(Time.unscaledTime);
    }

    void OnDisable()
    {
        if (_client != null)
            _client.CompanyIdsReceived -= OnCompanyIdsReceived;
        _client?.Dispose();
        _client = null;
    }

    void Update()
    {
        _client?.Tick(Time.unscaledTime);
    }

    void OnApplicationPause(bool pause)
    {
        if (!pause)
            _client?.Tick(Time.unscaledTime);
    }

    void OnApplicationQuit()
    {
        _client?.Dispose();
        _client = null;
    }

    void OnCompanyIdsReceived(int[] ids)
    {
        IdsReceived?.Invoke(ids);
    }
}
