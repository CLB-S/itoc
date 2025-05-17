using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ITOC.Libs.Palette;

public sealed class PaletteStorage<T> : IDisposable where T : IEquatable<T>
{
    private readonly Palette<T> _palette;
    private List<ulong> _data = new();
    private int _entriesPerLong;
    private bool _isSingleEntryMode;
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

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
        if (index < 0) return _palette.DefaultValue;

        if (_isSingleEntryMode) return _palette.GetValue(0);

        _lock.EnterReadLock();
        try
        {
            var longIndex = index / _entriesPerLong;
            if (longIndex >= _data.Count) return _palette.DefaultValue;

            var bitOffset = index % _entriesPerLong * _palette.BitsPerEntry;
            var mask = _palette.Mask << bitOffset;
            var value = (_data[longIndex] & mask) >> bitOffset;

            return _palette.GetValue((int)value);
        }
        finally
        {
            _lock.ExitReadLock();
        }
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

        _lock.EnterWriteLock();
        try
        {
            EnsureCapacity(index);

            var longIndex = index / _entriesPerLong;
            var bitOffset = index % _entriesPerLong * _palette.BitsPerEntry;

            _data[longIndex] &= ~(_palette.Mask << bitOffset);
            _data[longIndex] |= (paletteId & _palette.Mask) << bitOffset;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void SetRange(IEnumerable<(int Index, T Value)> entries)
    {
        if (entries == null || !entries.Any())
            return;

        // Convert to list to avoid multiple enumeration
        var entriesList = entries.ToList();

        // In single entry mode, check if we need to transition
        if (_isSingleEntryMode)
        {
            // Check if all entries have the same palette ID 0
            var allSameValue = entriesList.All(e => _palette.GetId(e.Value) == 0);

            if (!allSameValue)
                _palette.ForceNormalMode();
            else
                return; // All entries have the default value, nothing to do
        }

        _lock.EnterWriteLock();
        try
        {
            // Find the highest index to ensure capacity once
            var maxIndex = entriesList.Max(e => e.Index);
            EnsureCapacity(maxIndex);

            foreach (var (index, value) in entriesList)
            {
                var paletteId = (ulong)_palette.GetId(value);
                var longIndex = index / _entriesPerLong;
                var bitOffset = index % _entriesPerLong * _palette.BitsPerEntry;

                _data[longIndex] &= ~(_palette.Mask << bitOffset);
                _data[longIndex] |= (paletteId & _palette.Mask) << bitOffset;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
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

        _lock.EnterWriteLock();
        try
        {
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
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private int CalculateNewCapacity(int oldLongCount, int oldBits, int newBits)
    {
        var totalEntries = oldLongCount * (64 / oldBits);
        return (totalEntries + (64 / newBits) - 1) / (64 / newBits);
    }

    private void UpdateSingleEntryMode(bool isSingleEntry)
    {
        if (_isSingleEntryMode == isSingleEntry) return;

        _lock.EnterWriteLock();
        try
        {
            _isSingleEntryMode = isSingleEntry;
            if (isSingleEntry) _data.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void UpdateEntriesPerLong() =>
        _entriesPerLong = 64 / _palette.BitsPerEntry;

    public int StorageSize => _isSingleEntryMode ? 0 : _data.Count;

    public void Dispose()
    {
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }

    ~PaletteStorage() => Dispose();
}
