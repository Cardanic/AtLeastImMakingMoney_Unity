using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

/// <summary>
/// Long-running UDP channel. All socket I/O stays on one background thread so Unity's
/// main thread never blocks, and the socket is rebound if the LAN adapter drops.
/// </summary>
public sealed class UdpLanTransport : IDisposable
{
    public readonly struct Datagram
    {
        public readonly byte[] Bytes;
        public readonly IPEndPoint Remote;

        public Datagram(byte[] bytes, IPEndPoint remote)
        {
            Bytes = bytes;
            Remote = remote;
        }
    }

    const int MaxQueuedDatagrams = 64;
    const int PollMicroseconds = 10_000;
    const int ReconnectMinMs = 1000;
    const int ReconnectMaxMs = 15000;
    const int ThreadJoinMs = 100;

    readonly int _port;
    readonly Action<string> _warn;
    readonly ConcurrentQueue<Datagram> _inbound = new();
    readonly ConcurrentQueue<Datagram> _outbound = new();

    UdpClient _udp;
    Thread _thread;
    volatile bool _run;
    volatile bool _disposed;
    int _generation;
    int _inboundCount;
    int _outboundCount;
    int _reconnectDelayMs = ReconnectMinMs;
    long _lastLoopTicks;
    DateTime _nextStartUtc = DateTime.MinValue;

    public UdpLanTransport(int port, Action<string> warn = null)
    {
        _port = port;
        _warn = warn;
    }

    public bool IsRunning => _run && _thread != null && _thread.IsAlive;

    public void EnsureRunning()
    {
        if (_disposed)
            return;
        if (IsRunning && !IsLoopStalled())
            return;
        if (DateTime.UtcNow < _nextStartUtc)
            return;
        _nextStartUtc = DateTime.UtcNow.AddMilliseconds(_reconnectDelayMs);
        Restart("watchdog");
    }

    public void Start()
    {
        if (_disposed)
            return;

        Interlocked.Increment(ref _generation);
        StopThreadAndSocket();
        _run = true;
        int generation = _generation;
        if (!TryBind())
        {
            _reconnectDelayMs = Math.Min(_reconnectDelayMs * 2, ReconnectMaxMs);
            return;
        }

        _reconnectDelayMs = ReconnectMinMs;
        _thread = new Thread(() => IoLoop(generation))
        {
            IsBackground = true,
            Name = "UdpLanTransport"
        };
        _thread.Start();
    }

    public void Send(byte[] bytes, IPEndPoint endpoint)
    {
        if (_disposed || bytes == null || bytes.Length == 0 || endpoint == null)
            return;

        Enqueue(_outbound, ref _outboundCount, new Datagram(bytes, CopyEndpoint(endpoint)));
    }

    public bool TryReceive(out Datagram datagram)
    {
        if (_inbound.TryDequeue(out datagram))
        {
            Interlocked.Decrement(ref _inboundCount);
            return true;
        }

        datagram = default;
        return false;
    }

    public void Dispose()
    {
        _disposed = true;
        _run = false;
        StopThreadAndSocket();
    }

    void Restart(string reason)
    {
        _warn?.Invoke($"UDP transport restart ({reason}) on port {_port}");
        Start();
    }

    bool IsLoopStalled()
    {
        if (_lastLoopTicks == 0)
            return false;
        long ageMs = (DateTime.UtcNow.Ticks - _lastLoopTicks) / TimeSpan.TicksPerMillisecond;
        return ageMs > 20000;
    }

    void IoLoop(int generation)
    {
        while (_run && !_disposed && _generation == generation)
        {
            _lastLoopTicks = DateTime.UtcNow.Ticks;
            try
            {
                FlushOutbound();
                PollInbound();
            }
            catch (ObjectDisposedException)
            {
                if (!StillThisSession(generation))
                    break;
                RebindAfterFault(generation);
            }
            catch (SocketException)
            {
                if (!StillThisSession(generation))
                    break;
                RebindAfterFault(generation);
            }
            catch (Exception ex)
            {
                _warn?.Invoke($"UDP I/O exception: {ex.GetType().Name}");
                if (!StillThisSession(generation))
                    break;
                RebindAfterFault(generation);
            }
        }
    }

    bool StillThisSession(int generation)
    {
        return _run && !_disposed && _generation == generation;
    }

    void FlushOutbound()
    {
        var udp = _udp;
        if (udp == null)
            return;

        while (_outbound.TryDequeue(out var datagram))
        {
            Interlocked.Decrement(ref _outboundCount);
            try
            {
                udp.Send(datagram.Bytes, datagram.Bytes.Length, datagram.Remote);
            }
            catch (SocketException)
            {
                break;
            }
        }
    }

    void PollInbound()
    {
        var udp = _udp;
        if (udp?.Client == null)
        {
            Thread.Sleep(10);
            return;
        }

        if (!udp.Client.Poll(PollMicroseconds, SelectMode.SelectRead))
            return;
        if (udp.Available <= 0)
            return;

        var remote = new IPEndPoint(IPAddress.Any, 0);
        byte[] received = udp.Receive(ref remote);
        if (received == null || received.Length == 0 || received.Length > CompanyIdProtocol.MaxDatagramBytes)
            return;

        Enqueue(_inbound, ref _inboundCount, new Datagram(received, CopyEndpoint(remote)));
    }

    void RebindAfterFault(int generation)
    {
        CloseSocket();
        int delay = _reconnectDelayMs;
        _reconnectDelayMs = Math.Min(_reconnectDelayMs * 2, ReconnectMaxMs);

        int waited = 0;
        while (waited < delay && StillThisSession(generation))
        {
            _lastLoopTicks = DateTime.UtcNow.Ticks;
            Thread.Sleep(50);
            waited += 50;
        }

        if (!StillThisSession(generation))
            return;
        if (TryBind())
            _reconnectDelayMs = ReconnectMinMs;
    }

    bool TryBind()
    {
        CloseSocket();
        try
        {
            var udp = new UdpClient();
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.EnableBroadcast = true;
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, _port));
            _udp = udp;
            _lastLoopTicks = DateTime.UtcNow.Ticks;
            return true;
        }
        catch (SocketException ex)
        {
            _warn?.Invoke($"UDP bind failed on {_port} ({ex.SocketErrorCode})");
            _udp = null;
            return false;
        }
    }

    void StopThreadAndSocket()
    {
        _run = false;
        CloseSocket();
        var thread = _thread;
        _thread = null;
        if (thread != null && thread.IsAlive && thread != Thread.CurrentThread)
            thread.Join(ThreadJoinMs);
    }

    void CloseSocket()
    {
        var udp = _udp;
        _udp = null;
        if (udp == null)
            return;
        try { udp.Close(); }
        catch (Exception) { /* already closed */ }
    }

    static void Enqueue(ConcurrentQueue<Datagram> queue, ref int count, Datagram datagram)
    {
        queue.Enqueue(datagram);
        if (Interlocked.Increment(ref count) <= MaxQueuedDatagrams)
            return;

        if (queue.TryDequeue(out _))
            Interlocked.Decrement(ref count);
    }

    static IPEndPoint CopyEndpoint(IPEndPoint endpoint)
    {
        return new IPEndPoint(endpoint.Address, endpoint.Port);
    }
}
