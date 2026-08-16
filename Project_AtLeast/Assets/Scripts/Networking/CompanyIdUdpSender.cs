using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Exhibit composition root. Wires the company filter to <see cref="ExhibitPhoneHub"/>.
/// </summary>
public sealed class CompanyIdUdpSender : MonoBehaviour
{
    [Header("Data")]
    [SerializeField]
    MsciWorldCompanyFilter dataSource;

    [Header("Network")]
    [SerializeField]
    int port = CompanyIdProtocol.DefaultPort;

    [Tooltip("Wait this long after the last slider/toggle change before sending.")]
    [SerializeField, Min(0f)]
    float debounceSeconds = 0.15f;

    [Tooltip("Re-send the last id list so phones keep the current filter.")]
    [SerializeField, Min(0.25f)]
    float heartbeatSeconds = 2f;

    [Tooltip("How often this PC asks the LAN who is listening.")]
    [SerializeField, Min(0.25f)]
    float discoverIntervalSeconds = 1f;

    [Tooltip("Drop a phone if it stops announcing for this long.")]
    [SerializeField, Min(1f)]
    float phoneTimeoutSeconds = 5f;

    [SerializeField]
    bool logSends;

    ExhibitPhoneHub _hub;
    FilteredCompanyListener _listener;

    void Awake()
    {
        Application.runInBackground = true;
        if (dataSource == null)
            dataSource = GetComponent<MsciWorldCompanyFilter>();
    }

    void OnEnable()
    {
        _hub = new ExhibitPhoneHub(
            new ExhibitPhoneHub.Settings
            {
                Port = port,
                DebounceSeconds = debounceSeconds,
                HeartbeatSeconds = heartbeatSeconds,
                DiscoverIntervalSeconds = discoverIntervalSeconds,
                PhoneTimeoutSeconds = phoneTimeoutSeconds,
                LogSends = logSends,
                MaxPhones = 32
            },
            message => Debug.Log($"{nameof(CompanyIdUdpSender)}: {message}")
        );
        _hub.Start(Time.unscaledTime);

        _listener = new FilteredCompanyListener(dataSource, HandleFiltered);
        _listener.Subscribe();
    }

    void OnDisable()
    {
        _listener?.Unsubscribe();
        _listener = null;
        _hub?.Dispose();
        _hub = null;
    }

    void Update()
    {
        _hub?.Tick(Time.unscaledTime);
    }

    void OnApplicationQuit()
    {
        _hub?.Dispose();
        _hub = null;
    }

    void HandleFiltered(IReadOnlyList<MsciWorldCompanyFilter.Organization> companies)
    {
        if (_hub == null)
            return;

        var ids = new int[companies.Count];
        for (int i = 0; i < companies.Count; i++)
            ids[i] = companies[i].id;
        _hub.SubmitCompanyIds(ids, Time.unscaledTime);
    }
}
