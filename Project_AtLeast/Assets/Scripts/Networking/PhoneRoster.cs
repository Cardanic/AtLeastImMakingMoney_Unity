using System;
using System.Collections.Generic;
using System.Net;

/// <summary>
/// In-memory roster of discovered phones. Assigns stable Phone 1 / Phone 2 slots for
/// the whole Play session and tracks online / lost / reconnect without doing I/O.
/// </summary>
public sealed class PhoneRoster
{
    public enum AnnounceKind
    {
        Ignored,
        Heartbeat,
        Found,
        Reconnected
    }

    public sealed class Phone
    {
        public string DeviceId { get; }
        public int Slot { get; }
        public IPEndPoint Endpoint { get; internal set; }
        public float LastSeen { get; internal set; }
        public bool IsOnline { get; internal set; }

        internal Phone(string deviceId, int slot)
        {
            DeviceId = deviceId;
            Slot = slot;
        }
    }

    readonly int _maxPhones;
    readonly Dictionary<string, Phone> _phones = new();
    readonly Dictionary<string, int> _slotByDeviceId = new();
    readonly List<Phone> _lostBuffer = new();
    int _nextSlot = 1;

    public PhoneRoster(int maxPhones = 32)
    {
        _maxPhones = maxPhones < 1 ? 1 : maxPhones;
    }

    public int OnlineCount
    {
        get
        {
            int count = 0;
            foreach (var phone in _phones.Values)
            {
                if (phone.IsOnline)
                    count++;
            }
            return count;
        }
    }

    public AnnounceKind NoteAnnounce(string deviceId, IPEndPoint endpoint, float now, out Phone phone)
    {
        phone = null;
        if (string.IsNullOrEmpty(deviceId) || endpoint == null)
            return AnnounceKind.Ignored;
        if (deviceId.Length > 64)
            return AnnounceKind.Ignored;

        if (!_slotByDeviceId.TryGetValue(deviceId, out int slot))
        {
            if (_slotByDeviceId.Count >= _maxPhones)
                return AnnounceKind.Ignored;
            slot = _nextSlot++;
            _slotByDeviceId[deviceId] = slot;
        }

        if (!_phones.TryGetValue(deviceId, out phone))
        {
            phone = new Phone(deviceId, slot)
            {
                Endpoint = Copy(endpoint),
                LastSeen = now,
                IsOnline = true
            };
            _phones[deviceId] = phone;
            return AnnounceKind.Found;
        }

        phone.Endpoint = Copy(endpoint);
        phone.LastSeen = now;
        if (phone.IsOnline)
            return AnnounceKind.Heartbeat;

        phone.IsOnline = true;
        return AnnounceKind.Reconnected;
    }

    public void TickTimeouts(float now, float timeoutSeconds, Action<Phone> onLost)
    {
        _lostBuffer.Clear();
        foreach (var phone in _phones.Values)
        {
            if (!phone.IsOnline)
                continue;
            if (now - phone.LastSeen <= timeoutSeconds)
                continue;
            phone.IsOnline = false;
            _lostBuffer.Add(phone);
        }

        for (int i = 0; i < _lostBuffer.Count; i++)
            onLost?.Invoke(_lostBuffer[i]);
    }

    public void ForEachOnline(Action<Phone> action)
    {
        foreach (var phone in _phones.Values)
        {
            if (phone.IsOnline)
                action(phone);
        }
    }

    static IPEndPoint Copy(IPEndPoint endpoint)
    {
        return new IPEndPoint(endpoint.Address, endpoint.Port);
    }
}
