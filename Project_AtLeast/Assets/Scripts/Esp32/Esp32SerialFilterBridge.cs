using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Optional USB-serial bridge from the ESP32 encoder firmware into
/// <see cref="MsciWorldCompanyFilter"/>. Sliders/toggle stay wired and work without hardware.
/// Enable <see cref="connectOnStart"/> (or call <see cref="Connect"/>) only when a board is plugged in.
/// </summary>
[DefaultExecutionOrder(-50)]
public sealed class Esp32SerialFilterBridge : MonoBehaviour
{
    static readonly Regex CompactLine = new Regex(
        @"^F,(-?\d+),(-?\d+),(-?\d+)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    static readonly Regex VerboseMilitary = new Regex(
        @"military revenue:(-?\d+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    static readonly Regex VerboseLobby = new Regex(
        @"lobby costs:(-?\d+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    static readonly Regex VerboseOccupation = new Regex(
        @"occupation involvement:(-?\d+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    [Header("Connection")]
    [Tooltip("When false (default), Play Mode never opens a COM port — developers use sliders only.")]
    [SerializeField]
    bool connectOnStart;

    [Tooltip("Empty = first available Windows COM port. Otherwise e.g. COM5.")]
    [SerializeField]
    string portName = "";

    [SerializeField]
    int baudRate = 115200;

    [Header("Filter + UI")]
    [SerializeField]
    MsciWorldCompanyFilter filter;

    [SerializeField]
    Slider lobbySlider;

    [SerializeField]
    Slider militarySlider;

    [SerializeField]
    Toggle whoProfitsToggle;

    readonly ConcurrentQueue<string> _lines = new();
    readonly StringBuilder _lineBuffer = new();
    readonly byte[] _readBuffer = new byte[256];

    Thread _readerThread;
    volatile bool _runReader;
    bool _loggedConnectFailure;
    bool _hasLast;
    int _lastMilitary = int.MinValue;
    int _lastLobby = int.MinValue;
    int _lastOccupation = int.MinValue;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    WinSerialPort _port;
#endif

    public bool IsConnected
    {
        get
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return _port != null && _port.IsOpen;
#else
            return false;
#endif
        }
    }

    void Awake()
    {
        if (filter == null)
            filter = GetComponent<MsciWorldCompanyFilter>();
        TryAutoWireUi();
    }

    void Start()
    {
        if (connectOnStart)
            Connect();
    }

    void OnDisable()
    {
        Disconnect();
    }

    void OnDestroy()
    {
        Disconnect();
    }

    void Update()
    {
        while (_lines.TryDequeue(out string line))
            HandleLine(line);
    }

    /// <summary>Open the configured COM port (Windows Editor / Standalone only).</summary>
    public void Connect()
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        if (IsConnected)
            return;

        string resolved = ResolvePortName();
        if (string.IsNullOrEmpty(resolved))
        {
            LogConnectFailureOnce(
                "No COM port found. Leave connectOnStart off to use sliders without an ESP32, " +
                "or set portName (e.g. COM5).");
            return;
        }

        try
        {
            _port = new WinSerialPort(resolved, baudRate);
            _port.Open();
            _loggedConnectFailure = false;
            _hasLast = false;
            StartReaderThread();
            Debug.Log($"{nameof(Esp32SerialFilterBridge)}: connected to {resolved} @ {baudRate}.");
        }
        catch (Exception ex)
        {
            Disconnect();
            LogConnectFailureOnce($"Failed to open {resolved}: {ex.Message}. Sliders remain usable.");
        }
#else
        LogConnectFailureOnce("ESP32 serial is only supported on Windows Editor / Standalone.");
#endif
    }

    public void Disconnect()
    {
        _runReader = false;
        if (_readerThread != null)
        {
            try { _readerThread.Join(300); }
            catch { /* ignore */ }
            _readerThread = null;
        }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        if (_port != null)
        {
            try { _port.Dispose(); }
            catch { /* ignore */ }
            _port = null;
        }
#endif

        while (_lines.TryDequeue(out _)) { }
        lock (_lineBuffer)
            _lineBuffer.Clear();
    }

    void StartReaderThread()
    {
        _runReader = true;
        _readerThread = new Thread(ReaderLoop)
        {
            IsBackground = true,
            Name = "Esp32SerialReader"
        };
        _readerThread.Start();
    }

    void ReaderLoop()
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        while (_runReader)
        {
            try
            {
                if (_port == null || !_port.IsOpen)
                {
                    Thread.Sleep(50);
                    continue;
                }

                int read = _port.Read(_readBuffer, 0, _readBuffer.Length);
                if (read <= 0)
                {
                    Thread.Sleep(5);
                    continue;
                }

                lock (_lineBuffer)
                {
                    for (int i = 0; i < read; i++)
                    {
                        char c = (char)_readBuffer[i];
                        if (c == '\r')
                            continue;
                        if (c == '\n')
                        {
                            if (_lineBuffer.Length > 0)
                            {
                                _lines.Enqueue(_lineBuffer.ToString());
                                _lineBuffer.Clear();
                            }
                            continue;
                        }
                        _lineBuffer.Append(c);
                        if (_lineBuffer.Length > 512)
                            _lineBuffer.Clear();
                    }
                }
            }
            catch (Exception)
            {
                if (_runReader)
                    Thread.Sleep(100);
            }
        }
#endif
    }

    void HandleLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;
        if (line.IndexOf("RESET", StringComparison.OrdinalIgnoreCase) >= 0)
            return;
        if (line.StartsWith("Encoders", StringComparison.OrdinalIgnoreCase))
            return;

        if (!TryParseLine(line, out int military, out int lobby, out int occupation))
            return;

        military = Mathf.Clamp(military, 0, 100);
        lobby = Mathf.Clamp(lobby, 0, 100);
        occupation = occupation != 0 ? 1 : 0;

        if (_hasLast
            && military == _lastMilitary
            && lobby == _lastLobby
            && occupation == _lastOccupation)
            return;

        _hasLast = true;
        _lastMilitary = military;
        _lastLobby = lobby;
        _lastOccupation = occupation;
        ApplyToFilterAndUi(military, lobby, occupation);
    }

    void ApplyToFilterAndUi(int military, int lobby, int occupation)
    {
        bool whoProfits = occupation != 0;

        // Drive UI when the displayed value differs so IntegerSliderLabel / visuals update
        // (Slider/Toggle onValueChanged also call Set*). Always call Set* when the UI
        // value is already equal, so the filter still matches the encoder packet.
        if (militarySlider != null && !Mathf.Approximately(militarySlider.value, military))
            militarySlider.value = military;
        else if (filter != null)
            filter.SetMilitaryEconomicExposureScore(military);

        if (lobbySlider != null && !Mathf.Approximately(lobbySlider.value, lobby))
            lobbySlider.value = lobby;
        else if (filter != null)
            filter.SetLobbyingEconomicExposureScore(lobby);

        if (whoProfitsToggle != null && whoProfitsToggle.isOn != whoProfits)
            whoProfitsToggle.isOn = whoProfits;
        else if (filter != null)
            filter.SetFilterByWhoProfits(whoProfits);
    }

    static bool TryParseLine(string line, out int military, out int lobby, out int occupation)
    {
        military = 0;
        lobby = 0;
        occupation = 0;

        Match compact = CompactLine.Match(line);
        if (compact.Success)
        {
            military = ParseInt(compact.Groups[1].Value);
            lobby = ParseInt(compact.Groups[2].Value);
            occupation = ParseInt(compact.Groups[3].Value);
            return true;
        }

        Match mMil = VerboseMilitary.Match(line);
        Match mLob = VerboseLobby.Match(line);
        Match mOcc = VerboseOccupation.Match(line);
        if (!mMil.Success || !mLob.Success || !mOcc.Success)
            return false;

        military = ParseInt(mMil.Groups[1].Value);
        lobby = ParseInt(mLob.Groups[1].Value);
        occupation = ParseInt(mOcc.Groups[1].Value);
        return true;
    }

    static int ParseInt(string text)
    {
        return int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    string ResolvePortName()
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        if (!string.IsNullOrWhiteSpace(portName))
            return portName.Trim();

        string[] ports = WinSerialPort.GetPortNames();
        return ports.Length > 0 ? ports[0] : null;
#else
        return null;
#endif
    }

    void TryAutoWireUi()
    {
        if (lobbySlider == null)
        {
            var go = GameObject.Find("ParameterPrefab_1");
            if (go != null)
                lobbySlider = go.GetComponentInChildren<Slider>(true);
        }

        if (militarySlider == null)
        {
            var go = GameObject.Find("ParameterPrefab_2");
            if (go != null)
                militarySlider = go.GetComponentInChildren<Slider>(true);
        }

        if (whoProfitsToggle == null)
        {
            var go = GameObject.Find("Toggle");
            if (go != null)
                whoProfitsToggle = go.GetComponent<Toggle>();
        }
    }

    void LogConnectFailureOnce(string message)
    {
        if (_loggedConnectFailure)
            return;
        _loggedConnectFailure = true;
        Debug.LogWarning($"{nameof(Esp32SerialFilterBridge)}: {message}");
    }
}
