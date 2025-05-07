using System;
using System.Collections.Generic;

namespace Palette;

public class Palette<T> where T : IEquatable<T>
{
    private readonly List<T> _entries = new();
    private readonly T _defaultValue;
    private readonly int _initialBits;
    public event Action<int> OnBitsIncreased;
    public event Action<bool> OnSingleEntryStateChanged;

    public Palette(T defaultValue, int initialBits = 4)
    {
        _defaultValue = defaultValue;
        _entries.Add(defaultValue);
        _initialBits = initialBits;
        UpdateBits(initialBits);
        IsSingleEntry = true;
    }

    public int GetId(T value)
    {
        for (var i = 0; i < _entries.Count; i++)
            if (_entries[i] == null)
            {
                if (value == null) return i;
            }
            else if (_entries[i].Equals(value))
            {
                return i;
            }

        var newId = _entries.Count;
        _entries.Add(value);

        // Update single entry state
        var wasSingleEntry = IsSingleEntry;
        IsSingleEntry = _entries.Count <= 1;
        if (wasSingleEntry != IsSingleEntry)
            OnSingleEntryStateChanged?.Invoke(IsSingleEntry);

        var requiredBits = CalculateRequiredBits();
        if (requiredBits > BitsPerEntry)
        {
            UpdateBits(requiredBits);
            OnBitsIncreased?.Invoke(requiredBits);
        }

        return newId;
    }

    public T GetValue(int id)
    {
        return id >= 0 && id < _entries.Count ? _entries[id] : _defaultValue;
    }

    private int CalculateRequiredBits()
    {
        if (_entries.Count <= Math.Pow(2, _initialBits))
            return _initialBits;

        return _entries.Count <= 1 ? 1 : (int)Math.Ceiling(Math.Log2(_entries.Count));
    }

    private void UpdateBits(int bits)
    {
        BitsPerEntry = bits;
        Mask = (1UL << bits) - 1UL;
    }

    public int BitsPerEntry { get; private set; }
    public ulong Mask { get; private set; }
    public int Count => _entries.Count;
    public bool IsSingleEntry { get; private set; }
}