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
            HeartbeatOnlinePhones(now);
    }

    public void SubmitCompanyIds(int[] ids, float now)
    {
        _pendingIds = CompanyIdProtocol.CopyIds(ids);
        _sendAt = now + Math.Max(0f, _settings.DebounceSeconds);
        _log?.Invoke(
            $"filter queued ({_pendingIds.Length} companies) — will send in {_settings.DebounceSeconds:0.##}s " +
            $"to {_roster.OnlineCount} phone(s)"
        );
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

            // Prefer the port the phone declared; also keep the packet source port as a
            // fallback — some Android stacks announce from an ephemeral port.
            int declaredPort = message.listenPort > 0 ? message.listenPort : datagram.Remote.Port;
            var endpoint = new IPEndPoint(datagram.Remote.Address, declaredPort);
            var kind = _roster.NoteAnnounce(message.deviceId, endpoint, now, out var phone);
            if (kind == PhoneRoster.AnnounceKind.Found)
            {
                _log?.Invoke($"Phone {phone.Slot} found (id {phone.DeviceId}) at {phone.Endpoint}");
                if (declaredPort != datagram.Remote.Port)
                {
                    _log?.Invoke(
                        $"Phone {phone.Slot} announce source port {datagram.Remote.Port} " +
                        $"differs from listenPort {declaredPort} — using listenPort"
                    );
                }
                FlushPendingFilter(now);
                WelcomeAndSync(phone, now);
            }
            else if (kind == PhoneRoster.AnnounceKind.Reconnected)
            {
                _log?.Invoke($"Phone {phone.Slot} reconnected (id {phone.DeviceId}) at {phone.Endpoint}");
                FlushPendingFilter(now);
                WelcomeAndSync(phone, now);
            }
            // Heartbeat announces only refresh LastSeen via NoteAnnounce.
        }
    }

    void FlushPendingFilter(float now)
    {
        if (_pendingIds == null)
            return;

        var ids = _pendingIds;
        _pendingIds = null;
        SendFilter(ids, incrementSeq: true, now);
    }

    void OnPhoneLost(PhoneRoster.Phone phone)
    {
        _log?.Invoke($"Phone {phone.Slot} lost (id {phone.DeviceId}) at {phone.Endpoint}");
    }

    void WelcomeAndSync(PhoneRoster.Phone phone, float now)
    {
        SendToPhone(
            CompanyIdProtocol.ToBytes(CompanyIdProtocol.Welcome(phone.DeviceId, phone.Slot)),
            phone.Endpoint,
            now
        );
        _log?.Invoke($"welcome sent to Phone {phone.Slot} at {phone.Endpoint} (+ LAN broadcast)");

        if (_lastSentIds == null || _seq <= 0)
        {
            _log?.Invoke($"Phone {phone.Slot} welcomed but no company filter ready yet — waiting for MsciWorldCompanyFilter");
            return;
        }

        PushAssignedFilter(phone, "welcome-sync", forceLog: true, now);
    }

    void PushAssignedFilter(PhoneRoster.Phone phone, string reason, bool forceLog, float now = 0f)
    {
        if (_lastSentIds == null || _seq <= 0 || phone == null)
            return;

        var assigned = CompanyIdProtocol.IdsForPhoneSlot(_lastSentIds, phone.Slot);
        var bytes = CompanyIdProtocol.ToBytes(
            CompanyIdProtocol.Filter(_seq, assigned, phone.DeviceId)
        );
        SendToPhone(bytes, phone.Endpoint, now);

        if (forceLog || (_settings.LogSends && assigned.Length == 0))
        {
            _log?.Invoke(
                $"DATA SENT ({reason}) seq={_seq} Phone {phone.Slot} @ {phone.Endpoint} <- id[{FormatIds(assigned)}] " +
                $"bytes={bytes.Length} filteredTotal={_lastSentIds.Length}"
            );
        }
    }

    void HeartbeatOnlinePhones(float now)
    {
        _nextHeartbeatAt = now + _settings.HeartbeatSeconds;
        if (_lastSentIds == null || _seq <= 0)
            return;

        _roster.ForEachOnline(phone =>
        {
            SendToPhone(
                CompanyIdProtocol.ToBytes(CompanyIdProtocol.Welcome(phone.DeviceId, phone.Slot)),
                phone.Endpoint,
                now
            );
            PushAssignedFilter(phone, "heartbeat", forceLog: _settings.LogSends, now);
        });
    }

    void SendFilter(int[] ids, bool incrementSeq, float now)
    {
        if (incrementSeq)
        {
            if (CompanyIdProtocol.IdsEqual(ids, _lastSentIds) && _seq > 0)
            {
                if (_settings.LogSends)
                    _log?.Invoke($"filter unchanged ({ids?.Length ?? 0} ids) — skip seq bump");
                return;
            }
            _seq++;
        }

        _lastSentIds = ids;
        _nextHeartbeatAt = now + _settings.HeartbeatSeconds;

        int sent = 0;
        _roster.ForEachOnline(phone =>
        {
            var assigned = CompanyIdProtocol.IdsForPhoneSlot(ids, phone.Slot);
            var bytes = CompanyIdProtocol.ToBytes(
                CompanyIdProtocol.Filter(_seq, assigned, phone.DeviceId)
            );
            SendToPhone(bytes, phone.Endpoint, now);
            sent++;

            if (incrementSeq || _settings.LogSends)
            {
                _log?.Invoke(
                    $"DATA SENT ({(incrementSeq ? "filter-update" : "heartbeat")}) seq={_seq} " +
                    $"Phone {phone.Slot} @ {phone.Endpoint} <- id[{FormatIds(assigned)}] bytes={bytes.Length}"
                );
            }
        });

        if (sent == 0 && incrementSeq)
        {
            _log?.Invoke(
                $"filter ready seq={_seq} ids=[{FormatIds(ids)}] but no phones connected yet — " +
                "will sync on next phone announce"
            );
        }
    }

    static string FormatIds(int[] ids)
    {
        if (ids == null || ids.Length == 0)
            return "";
        return string.Join(",", ids);
    }

    /// <summary>
    /// Unicast to the phone and also LAN-broadcast. Many Wi-Fi APs deliver phone→PC
    /// announces but drop PC→phone unicasts; broadcast often still gets through.
    /// </summary>
    void SendToPhone(byte[] bytes, IPEndPoint endpoint, float now)
    {
        if (bytes == null || bytes.Length == 0)
            return;

        if (endpoint != null)
            _transport.Send(bytes, endpoint);

        var targets = _broadcasts.GetBroadcasts(_settings.Port, now);
        for (int i = 0; i < targets.Count; i++)
        {
            // Skip duplicate if unicast target is already that broadcast address.
            if (endpoint != null && targets[i].Equals(endpoint))
                continue;
            _transport.Send(bytes, targets[i]);
        }
    }

    void Broadcast(CompanyIdProtocol.Message message, float now)
    {
        var bytes = CompanyIdProtocol.ToBytes(message);
        var targets = _broadcasts.GetBroadcasts(_settings.Port, now);
        for (int i = 0; i < targets.Count; i++)
            _transport.Send(bytes, targets[i]);
    }
}
