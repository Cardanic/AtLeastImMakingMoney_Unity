using System;
using System.Net;

/// <summary>
/// Exhibit application service: discover phones, keep the roster alive, push company ids.
/// No Unity types — the MonoBehaviour only ticks this and feeds filter changes in.
/// </summary>
public sealed class ExhibitPhoneHub : IDisposable
{
    public struct Settings
    {
        public int Port;
        public float DebounceSeconds;
        public float HeartbeatSeconds;
        public float DiscoverIntervalSeconds;
        public float PhoneTimeoutSeconds;
        public bool LogSends;
        public int MaxPhones;
    }

    readonly Settings _settings;
    readonly UdpLanTransport _transport;
    readonly LanBroadcastDirectory _broadcasts;
    readonly PhoneRoster _roster;
    readonly Action<string> _log;

    int[] _pendingIds;
    int[] _lastSentIds;
    int _seq;
    float _sendAt = -1f;
    float _nextDiscoverAt;
    float _nextHeartbeatAt;
    bool _started;

    public ExhibitPhoneHub(Settings settings, Action<string> log)
    {
        _settings = settings;
        _log = log;
        _transport = new UdpLanTransport(settings.Port, log);
        _broadcasts = new LanBroadcastDirectory();
        _roster = new PhoneRoster(settings.MaxPhones > 0 ? settings.MaxPhones : 32);
    }

    public int ConnectedPhoneCount => _roster.OnlineCount;

    public void Start(float now)
    {
        _transport.Start();
        _started = true;
        _nextDiscoverAt = now;
        var ips = _broadcasts.GetLocalIPv4(now);
        _log?.Invoke(
            $"looking for phones on UDP {_settings.Port}. " +
            $"This PC IPv4: {(ips.Count == 0 ? "(none)" : string.Join(", ", ips))}"
        );
    }

    public void Tick(float now)
    {
        if (!_started)
            return;

        _transport.EnsureRunning();
        DrainInbox(now);
        _roster.TickTimeouts(now, _settings.PhoneTimeoutSeconds, OnPhoneLost);

        if (_pendingIds != null && now >= _sendAt)
        {
            var ids = _pendingIds;
            _pendingIds = null;
            SendFilter(ids, incrementSeq: true, now);
        }

        if (now >= _nextDiscoverAt)
        {
            _nextDiscoverAt = now + _settings.DiscoverIntervalSeconds;
            Broadcast(CompanyIdProtocol.Discover(), now);
        }

        if (_lastSentIds != null && now >= _nextHeartbeatAt)
            SendFilter(_lastSentIds, incrementSeq: false, now);
    }

    public void SubmitCompanyIds(int[] ids, float now)
    {
        _pendingIds = CompanyIdProtocol.CopyIds(ids);
        _sendAt = now + Math.Max(0f, _settings.DebounceSeconds);
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
            if (message.type != CompanyIdProtocol.TypeAnnounce)
                continue;

            int replyPort = message.listenPort > 0 ? message.listenPort : datagram.Remote.Port;
            var endpoint = new IPEndPoint(datagram.Remote.Address, replyPort);
            var kind = _roster.NoteAnnounce(message.deviceId, endpoint, now, out var phone);
            if (kind == PhoneRoster.AnnounceKind.Found)
            {
                _log?.Invoke($"Phone {phone.Slot} found (id {phone.DeviceId}) at {phone.Endpoint}");
                WelcomeAndSync(phone);
            }
            else if (kind == PhoneRoster.AnnounceKind.Reconnected)
            {
                _log?.Invoke($"Phone {phone.Slot} reconnected (id {phone.DeviceId}) at {phone.Endpoint}");
                WelcomeAndSync(phone);
            }
        }
    }

    void OnPhoneLost(PhoneRoster.Phone phone)
    {
        _log?.Invoke($"Phone {phone.Slot} lost (id {phone.DeviceId}) at {phone.Endpoint}");
    }

    void WelcomeAndSync(PhoneRoster.Phone phone)
    {
        _transport.Send(CompanyIdProtocol.ToBytes(CompanyIdProtocol.Welcome(phone.DeviceId, phone.Slot)), phone.Endpoint);
        if (_lastSentIds != null)
            _transport.Send(CompanyIdProtocol.ToBytes(CompanyIdProtocol.Filter(_seq, _lastSentIds)), phone.Endpoint);
    }

    void SendFilter(int[] ids, bool incrementSeq, float now)
    {
        if (incrementSeq)
        {
            if (CompanyIdProtocol.IdsEqual(ids, _lastSentIds) && _seq > 0)
                return;
            _seq++;
        }

        _lastSentIds = ids;
        _nextHeartbeatAt = now + _settings.HeartbeatSeconds;

        var bytes = CompanyIdProtocol.ToBytes(CompanyIdProtocol.Filter(_seq, ids));
        int sent = 0;
        _roster.ForEachOnline(phone =>
        {
            _transport.Send(bytes, phone.Endpoint);
            sent++;
        });

        if (incrementSeq && _settings.LogSends)
            _log?.Invoke($"seq={_seq} ids=[{string.Join(",", ids)}] to {sent} connected phone(s)");
    }

    void Broadcast(CompanyIdProtocol.Message message, float now)
    {
        var bytes = CompanyIdProtocol.ToBytes(message);
        var targets = _broadcasts.GetBroadcasts(_settings.Port, now);
        for (int i = 0; i < targets.Count; i++)
            _transport.Send(bytes, targets[i]);
    }
}
