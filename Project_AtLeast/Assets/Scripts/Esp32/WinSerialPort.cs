#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

/// <summary>
/// Minimal Windows COM-port reader for Unity (.NET Standard 2.1 has no System.IO.Ports).
/// Opens a port, configures 8N1, and exposes blocking Read into a byte buffer.
/// </summary>
public sealed class WinSerialPort : IDisposable
{
    const uint GenericRead = 0x80000000;
    const uint GenericWrite = 0x40000000;
    const uint OpenExisting = 3;

    SafeFileHandle _handle;
    bool _disposed;

    public string PortName { get; private set; }
    public int BaudRate { get; private set; }
    public bool IsOpen => _handle != null && !_handle.IsInvalid && !_handle.IsClosed;

    public WinSerialPort(string portName, int baudRate)
    {
        PortName = portName ?? throw new ArgumentNullException(nameof(portName));
        BaudRate = baudRate;
    }

    public void Open()
    {
        if (IsOpen)
            return;

        string path = PortName.StartsWith(@"\\.\", StringComparison.Ordinal)
            ? PortName
            : @"\\.\" + PortName;

        IntPtr raw = CreateFile(
            path,
            GenericRead | GenericWrite,
            0,
            IntPtr.Zero,
            OpenExisting,
            0,
            IntPtr.Zero);

        if (raw == IntPtr.Zero || raw == new IntPtr(-1))
            throw new IOException($"Failed to open serial port {PortName} (error {Marshal.GetLastWin32Error()}).");

        _handle = new SafeFileHandle(raw, ownsHandle: true);

        var dcb = new Dcb { DCBlength = (uint)Marshal.SizeOf(typeof(Dcb)) };
        if (!GetCommState(_handle, ref dcb))
            throw new IOException($"GetCommState failed on {PortName} (error {Marshal.GetLastWin32Error()}).");

        dcb.BaudRate = (uint)BaudRate;
        dcb.ByteSize = 8;
        dcb.Parity = 0;   // NOPARITY
        dcb.StopBits = 0; // ONESTOPBIT
        dcb.Flags = 1;    // fBinary

        if (!SetCommState(_handle, ref dcb))
            throw new IOException($"SetCommState failed on {PortName} (error {Marshal.GetLastWin32Error()}).");

        var timeouts = new CommTimeouts
        {
            ReadIntervalTimeout = 50,
            ReadTotalTimeoutConstant = 50,
            ReadTotalTimeoutMultiplier = 0,
            WriteTotalTimeoutConstant = 50,
            WriteTotalTimeoutMultiplier = 0
        };
        if (!SetCommTimeouts(_handle, ref timeouts))
            throw new IOException($"SetCommTimeouts failed on {PortName} (error {Marshal.GetLastWin32Error()}).");

        PurgeComm(_handle, 0x000F);
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        if (!IsOpen)
            throw new ObjectDisposedException(nameof(WinSerialPort));
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));
        if (offset < 0 || count < 0 || offset + count > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(count));
        if (count == 0)
            return 0;

        byte[] slice = offset == 0 && count == buffer.Length ? buffer : new byte[count];
        if (!ReadFile(_handle, slice, (uint)count, out uint read, IntPtr.Zero))
        {
            int err = Marshal.GetLastWin32Error();
            if (read == 0)
                return 0;
            throw new IOException($"ReadFile failed on {PortName} (error {err}).");
        }

        if (!ReferenceEquals(slice, buffer) && read > 0)
            Buffer.BlockCopy(slice, 0, buffer, offset, (int)read);
        return (int)read;
    }

    public void Close()
    {
        if (_handle != null && !_handle.IsClosed)
            _handle.Close();
        _handle = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Close();
    }

    /// <summary>Enumerate COM port names via QueryDosDevice.</summary>
    public static string[] GetPortNames()
    {
        var names = new List<string>();
        char[] buffer = new char[65536];
        uint written = QueryDosDevice(null, buffer, (uint)buffer.Length);
        if (written == 0)
            return Array.Empty<string>();

        int start = 0;
        for (int i = 0; i < written; i++)
        {
            if (buffer[i] != '\0')
                continue;
            int len = i - start;
            if (len > 0)
            {
                string name = new string(buffer, start, len);
                if (name.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
                    names.Add(name);
            }
            start = i + 1;
            if (start < written && buffer[start] == '\0')
                break;
        }

        names.Sort(CompareComPorts);
        return names.ToArray();
    }

    static int CompareComPorts(string a, string b)
    {
        if (TryComNumber(a, out int na) && TryComNumber(b, out int nb))
            return na.CompareTo(nb);
        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    }

    static bool TryComNumber(string name, out int number)
    {
        number = 0;
        if (string.IsNullOrEmpty(name) || name.Length < 4)
            return false;
        if (!name.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
            return false;
        return int.TryParse(name.Substring(3), out number);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool ReadFile(
        SafeFileHandle hFile,
        byte[] lpBuffer,
        uint nNumberOfBytesToRead,
        out uint lpNumberOfBytesRead,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GetCommState(SafeFileHandle hFile, ref Dcb lpDCB);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool SetCommState(SafeFileHandle hFile, ref Dcb lpDCB);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool SetCommTimeouts(SafeFileHandle hFile, ref CommTimeouts lpCommTimeouts);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool PurgeComm(SafeFileHandle hFile, uint dwFlags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern uint QueryDosDevice(string lpDeviceName, [Out] char[] lpTargetPath, uint ucchMax);

    [StructLayout(LayoutKind.Sequential)]
    struct Dcb
    {
        public uint DCBlength;
        public uint BaudRate;
        public uint Flags;
        public ushort wReserved;
        public ushort XonLim;
        public ushort XoffLim;
        public byte ByteSize;
        public byte Parity;
        public byte StopBits;
        public byte XonChar;
        public byte XoffChar;
        public byte ErrorChar;
        public byte EofChar;
        public byte EvtChar;
        public ushort wReserved1;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct CommTimeouts
    {
        public uint ReadIntervalTimeout;
        public uint ReadTotalTimeoutMultiplier;
        public uint ReadTotalTimeoutConstant;
        public uint WriteTotalTimeoutMultiplier;
        public uint WriteTotalTimeoutConstant;
    }
}
#endif
