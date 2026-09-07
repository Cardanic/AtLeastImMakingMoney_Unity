using System;

/// <summary>
/// Decides which filtered company each phone slot shows. Without this every phone
/// would show a fixed slice (Phone 1 → filtered[0], …), so the same companies win
/// forever and anything past the phone count is never seen.
///
/// Two knobs, both optional:
///  - Shuffle: reorder the filtered list once per filter change, so it is not always
///    the first N ids that land on phones.
///  - Rotate: when there are more companies than phones, advance the window every
///    RotateSeconds so every phone moves on to a fresh company and the whole list
///    cycles through. No-ops when the list already fits in the phone count.
///
/// Pure C# — no Unity types — so it matches the rest of the Networking services.
/// </summary>
public sealed class PhoneCompanyAssignment
{
    readonly bool _shuffle;
    readonly float _rotateSeconds;
    readonly int _rotateStep;
    readonly Random _rng;

    int[] _order = Array.Empty<int>();
    int _offset;
    float _nextRotateAt = -1f;

    /// <param name="shuffle">Randomise the order handed to phones.</param>
    /// <param name="rotateSeconds">Seconds between window advances; 0 disables rotation.</param>
    /// <param name="rotateStep">Companies to advance per rotation; 0 = advance by the phone count.</param>
    /// <param name="seed">Fixed shuffle seed for a reproducible order; 0 = fresh random each filter change.</param>
    public PhoneCompanyAssignment(bool shuffle, float rotateSeconds, int rotateStep, int seed = 0)
    {
        _shuffle = shuffle;
        _rotateSeconds = rotateSeconds;
        _rotateStep = rotateStep < 0 ? 0 : rotateStep;
        _rng = seed != 0 ? new Random(seed) : new Random();
    }

    /// <summary>Feed a new filtered id list. Resets the rotation window.</summary>
    public void SetIds(int[] ids, float now)
    {
        _order = CompanyIdProtocol.CopyIds(ids);
        if (_shuffle)
            Shuffle(_order);
        _offset = 0;
        _nextRotateAt = _rotateSeconds > 0f && _order.Length > 0 ? now + _rotateSeconds : -1f;
    }

    /// <summary>
    /// Advance the window if the interval elapsed. Returns true when the assignment
    /// changed and callers should re-push to phones.
    /// </summary>
    public bool Tick(float now, int phoneCount)
    {
        if (_nextRotateAt < 0f || now < _nextRotateAt)
            return false;

        _nextRotateAt = now + _rotateSeconds;

        // Nothing is hidden — every company is already on a phone.
        if (phoneCount < 1 || _order.Length <= phoneCount)
            return false;

        int step = _rotateStep > 0 ? _rotateStep : phoneCount;
        _offset = (int)(((long)_offset + step) % _order.Length);
        return true;
    }

    /// <summary>
    /// The id for a 1-based phone slot, or an empty array when there are fewer
    /// companies than phones and this slot has none (matches the old contract).
    /// </summary>
    public int[] IdForSlot(int phoneSlot)
    {
        int slotIndex = phoneSlot - 1;
        if (_order.Length == 0 || slotIndex < 0 || slotIndex >= _order.Length)
            return Array.Empty<int>();

        int index = (int)(((long)_offset + slotIndex) % _order.Length);
        return new[] { _order[index] };
    }

    void Shuffle(int[] items)
    {
        for (int i = items.Length - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
