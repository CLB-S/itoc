using System;
using System.Collections.Generic;

namespace Palette;

public class PaletteStorage<T> where T : IEquatable<T>
{
    private readonly Palette<T> _palette;
    private List<ulong> _data = new();
    private int _entriesPerLong;

    public PaletteStorage(Palette<T> palette)
    {
        _palette = palette;
        _palette.OnBitsIncreased += MigrateData;
        UpdateEntriesPerLong();
    }

    public T Get(int index)
    {
        if (index < 0) return _palette.GetValue(-1);

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
        int requiredLongs = (index / _entriesPerLong) + 1;
        while (_data.Count < requiredLongs)
            _data.Add(0UL);
    }

    private void MigrateData(int newBits)
    {
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

    private void UpdateEntriesPerLong() =>
        _entriesPerLong = 64 / _palette.BitsPerEntry;

    public int GetStorageSize() => _data.Count;
}