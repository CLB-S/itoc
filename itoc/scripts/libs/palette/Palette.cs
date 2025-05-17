using System;
using System.Collections.Generic;

namespace ITOC.Libs.Palette;

public sealed class Palette<T> : IDisposable where T : IEquatable<T>
{
    private readonly List<T> _entries = new();
    private readonly int _initialBits;

    public event Action<int, int> OnBitsIncreased;
    public event Action<bool> OnSingleEntryStateChanged;

    public Palette(T defaultValue, int initialBits = 4)
    {
        DefaultValue = defaultValue;
        _entries.Add(defaultValue);
        _initialBits = initialBits;
        UpdateBits(initialBits);
        IsSingleEntry = true;
    }

    public T DefaultValue { get; }
    public int BitsPerEntry { get; private set; }
    public ulong Mask { get; private set; }
    public bool IsSingleEntry { get; private set; }

    public int GetId(T value)
    {
        for (var i = 0; i < _entries.Count; i++)
            if (EqualityComparer<T>.Default.Equals(_entries[i], value))
                return i;

        var newId = _entries.Count;
        _entries.Add(value);

        var wasSingleEntry = IsSingleEntry;
        IsSingleEntry = false;
        if (wasSingleEntry)
            OnSingleEntryStateChanged?.Invoke(false);

        var requiredBits = CalculateRequiredBits();
        if (requiredBits > BitsPerEntry)
        {
            var oldBits = BitsPerEntry;
            UpdateBits(requiredBits);
            OnBitsIncreased?.Invoke(oldBits, requiredBits);
        }

        return newId;
    }

    public T GetValue(int id)
    {
        return id >= 0 && id < _entries.Count ? _entries[id] : DefaultValue;
    }

    public void ForceNormalMode()
    {
        if (!IsSingleEntry) return;

        IsSingleEntry = false;
        OnSingleEntryStateChanged?.Invoke(false);
    }

    private int CalculateRequiredBits()
    {
        var count = _entries.Count;
        if (count <= 1) return 1;

        int bits = _initialBits;
        while ((1 << bits) < count) bits++;
        return bits;
    }

    private void UpdateBits(int newBits)
    {
        BitsPerEntry = newBits;
        Mask = (1UL << newBits) - 1UL;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    ~Palette() => Dispose();
}