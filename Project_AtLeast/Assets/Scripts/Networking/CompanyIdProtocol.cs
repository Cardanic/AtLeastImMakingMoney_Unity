using System;
using System.Text;
using UnityEngine;

/// <summary>
/// JSON datagram contract for the exhibit LAN. No I/O here — keep this file identical
/// on the phone project.
/// Uses JsonUtility (not Newtonsoft) so IL2CPP device builds can parse welcome/filter.
/// </summary>
public static class CompanyIdProtocol
{
    public const int Version = 1;
    public const int DefaultPort = 47777;
    public const int MaxDatagramBytes = 8192;

    public const string TypeDiscover = "discover";
    public const string TypeAnnounce = "announce";
    public const string TypeWelcome = "welcome";
    public const string TypeFilter = "filter";

    [Serializable]
    public sealed class Message
    {
        public int v = Version;
        public string type = "";
        public int seq;
        public int[] ids = Array.Empty<int>();
        public string deviceId = "";
        public int phoneSlot;
        public int listenPort;
    }

    public static byte[] ToBytes(Message message)
    {
        if (message == null)
            return Array.Empty<byte>();
        if (message.ids == null)
            message.ids = Array.Empty<int>();
        if (message.deviceId == null)
            message.deviceId = "";
        if (message.type == null)
            message.type = "";

        string json = JsonUtility.ToJson(message);
        return Encoding.UTF8.GetBytes(json);
    }

    public static bool TryParse(byte[] bytes, int length, out Message message)
    {
        message = null;
        if (bytes == null || length <= 0 || length > MaxDatagramBytes)
            return false;

        try
        {
            var json = Encoding.UTF8.GetString(bytes, 0, length);
            if (string.IsNullOrWhiteSpace(json))
                return false;

            var parsed = JsonUtility.FromJson<Message>(json);
            if (parsed == null || parsed.v != Version || string.IsNullOrEmpty(parsed.type))
                return false;
            if (!IsKnownType(parsed.type))
                return false;

            parsed.ids ??= Array.Empty<int>();
            parsed.deviceId ??= "";
            message = parsed;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool IsKnownType(string type)
    {
        return type == TypeDiscover
            || type == TypeAnnounce
            || type == TypeWelcome
            || type == TypeFilter;
    }

    public static Message Discover()
    {
        return new Message { v = Version, type = TypeDiscover };
    }

    public static Message Announce(string deviceId, int listenPort)
    {
        return new Message
        {
            v = Version,
            type = TypeAnnounce,
            deviceId = deviceId,
            listenPort = listenPort
        };
    }

    public static Message Welcome(string deviceId, int phoneSlot)
    {
        return new Message
        {
            v = Version,
            type = TypeWelcome,
            deviceId = deviceId,
            phoneSlot = phoneSlot
        };
    }

    public static Message Filter(int seq, int[] ids, string deviceId = "")
    {
        return new Message
        {
            v = Version,
            type = TypeFilter,
            seq = seq,
            ids = ids ?? Array.Empty<int>(),
            deviceId = deviceId ?? ""
        };
    }

    public static bool IdsEqual(int[] a, int[] b)
    {
        if (ReferenceEquals(a, b))
            return true;
        if (a == null || b == null || a.Length != b.Length)
            return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }
        return true;
    }

    public static int[] CopyIds(int[] source)
    {
        if (source == null || source.Length == 0)
            return Array.Empty<int>();
        var copy = new int[source.Length];
        Array.Copy(source, copy, source.Length);
        return copy;
    }

    /// <summary>
    /// Phone 1 → filteredIds[0], Phone 2 → filteredIds[1], …
    /// Returns an empty array when the slot has no company (more phones than ids).
    /// </summary>
    public static int[] IdsForPhoneSlot(int[] filteredIds, int phoneSlot)
    {
        int index = phoneSlot - 1;
        if (filteredIds == null || index < 0 || index >= filteredIds.Length)
            return Array.Empty<int>();
        return new[] { filteredIds[index] };
    }
}
