using System;

/// <summary>
/// Phone application service: announce this device, accept a slot, receive company ids.
/// Copy this file to the phone project with the protocol and transport types.
/// </summary>
public sealed class PhoneExhibitClient : IDisposable
{
    public struct Settings
    {
        public int Port;
        public float AnnounceIntervalSeconds;
        public bool LogReceives;
    }

    const float MinAnnounceGap = 0.2f;

    readonly Settings _settings;
    readonly string _deviceId;
    readonly UdpLanTransport _transport;
    readonly LanBroadcastDirectory _broadcasts;
    readonly Action<string> _log;

    float _nextAnnounceAt;
    float _lastAnnounceAt = -999f;
    int _lastSeq = int.MinValue;
    int _phoneSlot;
    bool _connected;
    bool _started;

    public event Action<int[]> CompanyIdsReceived;

    public PhoneExhibitClient(Settings settings, string deviceId, Action<string> log)
    {
        _settings = settings;
        _deviceId = deviceId;
        _log = log;
        _transport = new UdpLanTransport(settings.Port, log);
        _broadcasts = new LanBroadcastDirectory();
    }

    public string DeviceId => _deviceId;
    public int PhoneSlot => _phoneSlot;
    public int[] LastIds { get; private set; } = Array.Empty<int>();

    public void Start(float now)
    {
        _transport.Start();
        _started = true;
        _nextAnnounceAt = now;
        _log?.Invoke($"announcing as id {_deviceId} on UDP {_settings.Port}. Waiting for exhibit PC...");
    }

    public void Tick(float now)
    {
        if (!_started)
            return;

        _transport.EnsureRunning();
        DrainInbox(now);

        if (now >= _nextAnnounceAt)
        {
            _nextAnnounceAt = now + _settings.AnnounceIntervalSeconds;
            Announce(now);
        }
    }

    public void Dispose()
    {
        _started = false;
        _transport.Dispose();
    }

    void DrainInbox(float now)
    {
        while (_transport.TryReceive(out var datagram))
        {
            if (!CompanyIdProtocol.TryParse(datagram.Bytes, datagram.Bytes.Length, out var message))
                continue;

            if (message.type == CompanyIdProtocol.TypeDiscover)
            {
                Announce(now);
                continue;
            }

            if (message.type == CompanyIdProtocol.TypeWelcome)
            {
                if (!string.IsNullOrEmpty(message.deviceId) && message.deviceId != _deviceId)
                    continue;

                bool wasConnected = _connected;
                _phoneSlot = message.phoneSlot;
                _connected = true;
                if (!wasConnected)
                    _log?.Invoke($"connected as Phone {_phoneSlot} (id {_deviceId})");
                continue;
            }

            if (message.type != CompanyIdProtocol.TypeFilter)
                continue;
            if (message.seq == _lastSeq)
                continue;

            _lastSeq = message.seq;
            LastIds = CompanyIdProtocol.CopyIds(message.ids);
            CompanyIdsReceived?.Invoke(LastIds);

            if (_settings.LogReceives)
            {
                string who = _phoneSlot > 0 ? $"Phone {_phoneSlot}" : "unassigned phone";
                _log?.Invoke($"{who} received seq={message.seq} ids=[{string.Join(",", LastIds)}]");
            }
        }
    }

    void Announce(float now)
    {
        if (now - _lastAnnounceAt < MinAnnounceGap)
            return;

        _lastAnnounceAt = now;
        var bytes = CompanyIdProtocol.ToBytes(CompanyIdProtocol.Announce(_deviceId, _settings.Port));
        var targets = _broadcasts.GetBroadcasts(_settings.Port, now);
        for (int i = 0; i < targets.Count; i++)
            _transport.Send(bytes, targets[i]);
    }
}
