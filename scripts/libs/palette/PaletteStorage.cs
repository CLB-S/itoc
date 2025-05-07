using System;
using System.Collections.Generic;

namespace Palette;

// TODO: Testing & OnBitsDecreased

public class PaletteStorage<T> where T : IEquatable<T>
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

    public T Get(int index)
    {
        if (index < 0) return _palette.GetValue(-1);

        // If we're in single entry mode, always return the first palette entry
        if (_isSingleEntryMode) return _palette.GetValue(0);

        int longIndex = index / _entriesPerLong;
        if (longIndex >= _data.Count) return _palette.GetValue(-1);

        int bitOffset = (index % _entriesPerLong) * _palette.BitsPerEntry;
        ulong mask = _palette.Mask << bitOffset;
        ulong value = (_data[longIndex] & mask) >> bitOffset;

        return _palette.GetValue((int)value);
    }

    public void Set(int index, T value)
    {
        int paletteId = _palette.GetId(value);
        Set(index, (ulong)paletteId);
    }

    public void Set(int index, ulong paletteId)
    {
        // If we're in single entry mode, we don't need to store anything
        // Just ensure the palette ID is 0 (first entry)
        if (_isSingleEntryMode)
        {
            if (paletteId != 0)
            {
                // This would exit single entry mode, handled by UpdateSingleEntryMode callback
                _palette.GetId(_palette.GetValue((int)paletteId));
            }
            return;
        }

        EnsureCapacity(index);

        int longIndex = index / _entriesPerLong;
        int bitOffset = (index % _entriesPerLong) * _palette.BitsPerEntry;

        // Clear the current value
        ulong clearMask = ~(_palette.Mask << bitOffset);
        _data[longIndex] &= clearMask;

        // Set the new value
        _data[longIndex] |= (paletteId & _palette.Mask) << bitOffset;
    }

    private void EnsureCapacity(int index)
    {
        // No need to ensure capacity in single entry mode
        if (_isSingleEntryMode) return;

        int requiredLongs = (index / _entriesPerLong) + 1;
        while (_data.Count < requiredLongs)
            _data.Add(0UL);
    }

    private void MigrateData(int newBits)
    {
        // If we're in single entry mode, no need to migrate
        if (_isSingleEntryMode) return;

        var oldData = _data;
        _data = new List<ulong>();
        UpdateEntriesPerLong();

        int entriesToMigrate = oldData.Count * _entriesPerLong;
        for (int i = 0; i < entriesToMigrate; i++)
        {
            // Reconstruct the value using old packing
            int oldLongIndex = i / (64 / (newBits - 1));
            int oldBitOffset = (i % (64 / (newBits - 1))) * (newBits - 1);
            ulong oldValue = (oldData[oldLongIndex] >> oldBitOffset) & ((1UL << (newBits - 1)) - 1);

            // Store with new packing
            Set(i, oldValue);
        }
    }

    private void UpdateSingleEntryMode(bool isSingleEntry)
    {
        if (_isSingleEntryMode == isSingleEntry) return;

        _isSingleEntryMode = isSingleEntry;

        if (_isSingleEntryMode)
        {
            // Transitioning to single entry mode, we can clear the data
            _data.Clear();
        }
        else
        {
            // Transitioning from single entry mode to normal mode
            // All entries were implicitly 0, no need to initialize anything
        }
    }

    private void UpdateEntriesPerLong() =>
        _entriesPerLong = 64 / _palette.BitsPerEntry;

    public int GetStorageSize() => _isSingleEntryMode ? 0 : _data.Count;
}