using System;
using System.Collections.Generic;
using System.Linq;

namespace ITOC.Libs.Palette;

public sealed class PaletteStorage<T> : IDisposable where T : IEquatable<T>
{
    private readonly Palette<T> _palette;
    private List<ulong> _data = new();
    private int _entriesPerLong;
    private bool _isSingleEntryMode;

    public PaletteStorage(Palette<T> palette)
    {
        _palette = palette;
        _palette.OnBitsIncreased += MigrateData;
        _palette.OnSingleEntryStateChanged += UpdateSingleEntryMode;
        UpdateEntriesPerLong();
        _isSingleEntryMode = _palette.IsSingleEntry;
    }

    public PaletteStorage(Palette<T> palette, T[] values) : this(palette)
    {
        if (values == null || values.Length == 0)
            return;

        InitializeFromArray(values);
    }

    private void InitializeFromArray(T[] values)
    {
        // Analyze the array to determine if it's single-value
        bool isSingleValue = true;
        T firstValue = values[0];

        for (int i = 1; i < values.Length && isSingleValue; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(firstValue, values[i]))
            {
                isSingleValue = false;
                break;
            }
        }

        // Fast path for single-value arrays
        if (isSingleValue)
        {
            var paletteId = _palette.GetId(firstValue);
            if (paletteId != 0)
            {
                _palette.ForceNormalMode();

                // Fill with the same value
                EnsureCapacity(values.Length - 1);

                if (paletteId == 0) return; // Nothing to do for default value

                // Set all bits to the same value efficiently
                ulong filledLong = 0UL;
                for (int i = 0; i < _entriesPerLong; i++)
                    filledLong |= ((ulong)paletteId & _palette.Mask) << (i * _palette.BitsPerEntry);

                for (int i = 0; i < _data.Count; i++)
                    _data[i] = filledLong;
            }
            return;
        }

        // Get all unique values and assign palette IDs in one pass
        var uniqueValues = new Dictionary<T, int>(EqualityComparer<T>.Default);
        var hasNonDefaultValues = false;

        // First pass - identify unique values
        foreach (var value in values)
        {
            if (value == null) continue;
            if (!uniqueValues.ContainsKey(value))
            {
                var id = _palette.GetId(value);
                uniqueValues[value] = id;
                if (id != 0) hasNonDefaultValues = true;
            }
        }

        // If we only have default values, nothing more to do
        if (!hasNonDefaultValues && _isSingleEntryMode)
            return;

        if (_isSingleEntryMode && hasNonDefaultValues)
            _palette.ForceNormalMode();

        // Ensure we have enough capacity for all values
        EnsureCapacity(values.Length - 1);

        // Second pass - write values directly to storage
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] == null) continue;
            var paletteId = (ulong)uniqueValues[values[i]];
            if (paletteId == 0) continue; // Skip default values

            var longIndex = i / _entriesPerLong;
            var bitOffset = (i % _entriesPerLong) * _palette.BitsPerEntry;

            _data[longIndex] |= (paletteId & _palette.Mask) << bitOffset;
        }
    }

    public T Get(int index)
    {
        if (index < 0) return _palette.DefaultValue;

        if (_isSingleEntryMode) return _palette.GetValue(0);

        var longIndex = index / _entriesPerLong;
        if (longIndex >= _data.Count) return _palette.DefaultValue;

        var bitOffset = index % _entriesPerLong * _palette.BitsPerEntry;
        var mask = _palette.Mask << bitOffset;
        var value = (_data[longIndex] & mask) >> bitOffset;

        return _palette.GetValue((int)value);
    }

    public void Set(int index, T value)
    {
        var paletteId = _palette.GetId(value);
        Set(index, (ulong)paletteId);
    }

    public void Set(int index, ulong paletteId)
    {
        if (_isSingleEntryMode)
        {
            if (paletteId != 0)
                _palette.ForceNormalMode();
        }

        EnsureCapacity(index);

        var longIndex = index / _entriesPerLong;
        var bitOffset = index % _entriesPerLong * _palette.BitsPerEntry;

        _data[longIndex] &= ~(_palette.Mask << bitOffset);
        _data[longIndex] |= (paletteId & _palette.Mask) << bitOffset;
    }

    public void SetRange(IEnumerable<(int Index, T Value)> entries)
    {
        if (entries == null) return;

        foreach (var (index, value) in entries)
        {
            if (index < 0) continue;
            Set(index, value);
        }
    }

    private void EnsureCapacity(int index)
    {
        var requiredLongs = index / _entriesPerLong + 1;
        while (_data.Count < requiredLongs)
            _data.Add(0UL);
    }

    private void MigrateData(int oldBits, int newBits)
    {
        if (_isSingleEntryMode) return;

        var oldData = _data;
        _data = new List<ulong>(CalculateNewCapacity(oldData.Count, oldBits, newBits));

        var oldEntriesPerLong = 64 / oldBits;
        var newEntriesPerLong = 64 / newBits;
        var totalEntries = oldData.Count * oldEntriesPerLong;

        for (var i = 0; i < totalEntries; i++)
        {
            var oldLongIndex = i / oldEntriesPerLong;
            var oldBitOffset = (i % oldEntriesPerLong) * oldBits;
            var oldValue = (oldData[oldLongIndex] >> oldBitOffset) & ((1UL << oldBits) - 1);

            var newLongIndex = i / newEntriesPerLong;
            var newBitOffset = (i % newEntriesPerLong) * newBits;

            if (newLongIndex >= _data.Count)
                _data.Add(0UL);

            _data[newLongIndex] |= (oldValue & _palette.Mask) << newBitOffset;
        }

        UpdateEntriesPerLong();
    }

    private int CalculateNewCapacity(int oldLongCount, int oldBits, int newBits)
    {
        var totalEntries = oldLongCount * (64 / oldBits);
        return (totalEntries + (64 / newBits) - 1) / (64 / newBits);
    }

    private void UpdateSingleEntryMode(bool isSingleEntry)
    {
        if (_isSingleEntryMode == isSingleEntry) return;

        _isSingleEntryMode = isSingleEntry;
        if (isSingleEntry) _data.Clear();
    }

    private void UpdateEntriesPerLong() =>
        _entriesPerLong = 64 / _palette.BitsPerEntry;

    public int StorageSize => _isSingleEntryMode ? 0 : _data.Count;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    ~PaletteStorage() => Dispose();
}
