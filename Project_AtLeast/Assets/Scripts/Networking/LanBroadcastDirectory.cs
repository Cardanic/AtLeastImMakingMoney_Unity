using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using NetworkInterface = System.Net.NetworkInformation.NetworkInterface;

/// <summary>
/// Cached local broadcast targets. Enumerating adapters every packet is expensive and
/// has been a source of leaks / stalls in long-running Windows processes.
/// </summary>
public sealed class LanBroadcastDirectory
{
    readonly float _refreshSeconds;
    readonly List<IPEndPoint> _broadcasts = new();
    readonly List<string> _localIPv4 = new();
    float _nextRefreshAt = -1f;
    int _cachedPort = -1;

    public LanBroadcastDirectory(float refreshSeconds = 30f)
    {
        _refreshSeconds = MathfMax(refreshSeconds, 5f);
    }

    public IReadOnlyList<IPEndPoint> GetBroadcasts(int port, float now)
    {
        if (_nextRefreshAt < 0f || now >= _nextRefreshAt || _cachedPort != port)
            Refresh(port, now);
        return _broadcasts;
    }

    public IReadOnlyList<string> GetLocalIPv4(float now)
    {
        if (_nextRefreshAt < 0f || now >= _nextRefreshAt)
            Refresh(_cachedPort > 0 ? _cachedPort : CompanyIdProtocol.DefaultPort, now);
        return _localIPv4;
    }

    void Refresh(int port, float now)
    {
        _cachedPort = port;
        _nextRefreshAt = now + _refreshSeconds;
        _broadcasts.Clear();
        _localIPv4.Clear();
        _broadcasts.Add(new IPEndPoint(IPAddress.Broadcast, port));

        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up)
                    continue;
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                foreach (var address in nic.GetIPProperties().UnicastAddresses)
                {
                    if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    string ip = address.Address.ToString();
                    if (!_localIPv4.Contains(ip))
                        _localIPv4.Add(ip);

                    if (address.IPv4Mask == null)
                        continue;

                    var broadcast = ToBroadcast(address.Address, address.IPv4Mask);
                    if (broadcast == null)
                        continue;

                    var endpoint = new IPEndPoint(broadcast, port);
                    if (!_broadcasts.Exists(t => t.Equals(endpoint)))
                        _broadcasts.Add(endpoint);
                }
            }
        }
        catch (NetworkInformationException)
        {
            if (_broadcasts.Count == 0)
                _broadcasts.Add(new IPEndPoint(IPAddress.Broadcast, port));
        }
    }

    static IPAddress ToBroadcast(IPAddress address, IPAddress mask)
    {
        var ipBytes = address.GetAddressBytes();
        var maskBytes = mask.GetAddressBytes();
        if (ipBytes.Length != 4 || maskBytes.Length != 4)
            return null;

        var broadcast = new byte[4];
        for (int i = 0; i < 4; i++)
            broadcast[i] = (byte)(ipBytes[i] | (byte)~maskBytes[i]);
        return new IPAddress(broadcast);
    }

    static float MathfMax(float a, float b) => a > b ? a : b;
}
