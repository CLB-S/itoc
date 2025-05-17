using System;
using System.Collections.Generic;
using System.Threading;

namespace ITOC.Libs.Palette;

// TODO: Testing & OnBitsDecreased

public class PaletteStorage<T> where T : IEquatable<T>
{
    private readonly Palette<T> _palette;
    private List<ulong> _data = new();
    private int _entriesPerLong;
    private bool _isSingleEntryMode;
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

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

        _lock.EnterReadLock();
        try
        {
            var longIndex = index / _entriesPerLong;
            if (longIndex >= _data.Count) return _palette.GetValue(-1);

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
        // If we're in single entry mode, we don't need to store anything
        // Just ensure the palette ID is 0 (first entry)
        if (_isSingleEntryMode)
        {
            if (paletteId != 0)
                // This would exit single entry mode, handled by UpdateSingleEntryMode callback
                _palette.GetId(_palette.GetValue((int)paletteId));
            return;
        }

        _lock.EnterWriteLock();
        try
        {
            EnsureCapacity(index);

            var longIndex = index / _entriesPerLong;
            var bitOffset = index % _entriesPerLong * _palette.BitsPerEntry;

            // Clear the current value
            var clearMask = ~(_palette.Mask << bitOffset);
            _data[longIndex] &= clearMask;

            // Set the new value
            _data[longIndex] |= (paletteId & _palette.Mask) << bitOffset;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void EnsureCapacity(int index)
    {
        // No need to ensure capacity in single entry mode
        if (_isSingleEntryMode) return;

        // This method is called from Set which already has a write lock
        var requiredLongs = index / _entriesPerLong + 1;
        while (_data.Count < requiredLongs)
            _data.Add(0UL);
    }

    private void MigrateData(int newBits)
    {
        // If we're in single entry mode, no need to migrate
        if (_isSingleEntryMode) return;

        _lock.EnterWriteLock();
        try
        {
            var oldData = _data;
            _data = new List<ulong>();
            UpdateEntriesPerLong();

            var entriesToMigrate = oldData.Count * _entriesPerLong;
            for (var i = 0; i < entriesToMigrate; i++)
            {
                // Reconstruct the value using old packing
                var oldLongIndex = i / (64 / (newBits - 1));
                var oldBitOffset = i % (64 / (newBits - 1)) * (newBits - 1);
                var oldValue = (oldData[oldLongIndex] >> oldBitOffset) & ((1UL << (newBits - 1)) - 1);

                // Store with new packing - calling Set() with the write lock already acquired would deadlock
                // So we duplicate the logic from Set() here
                var longIndex = i / _entriesPerLong;
                var bitOffset = i % _entriesPerLong * _palette.BitsPerEntry;

                // Ensure capacity
                while (_data.Count <= longIndex)
                    _data.Add(0UL);

                // Set the new value
                _data[longIndex] |= (oldValue & _palette.Mask) << bitOffset;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void UpdateSingleEntryMode(bool isSingleEntry)
    {
        if (_isSingleEntryMode == isSingleEntry) return;

        _lock.EnterWriteLock();
        try
        {
            _isSingleEntryMode = isSingleEntry;

            if (_isSingleEntryMode)
            {
                // Transitioning to single entry mode, we can clear the data
                _data.Clear();
            }
            // Transitioning from single entry mode to normal mode
            // All entries were implicitly 0, no need to initialize anything
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void UpdateEntriesPerLong()
    {
        _entriesPerLong = 64 / _palette.BitsPerEntry;
    }

    public int GetStorageSize()
    {
        if (_isSingleEntryMode) return 0;

        _lock.EnterReadLock();
        try
        {
            return _data.Count;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // Ensure proper disposal of the lock
    ~PaletteStorage()
    {
        _lock.Dispose();
    }
}