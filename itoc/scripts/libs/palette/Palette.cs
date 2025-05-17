using System;
using System.Collections.Generic;
using System.Threading;

namespace Palette;

public class Palette<T> where T : IEquatable<T>
{
    private readonly List<T> _entries = new();
    private readonly T _defaultValue;
    private readonly int _initialBits;
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
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
        _lock.EnterUpgradeableReadLock();
        try
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

            _lock.EnterWriteLock();
            try
            {
                // Double-check in case another thread added the value while we were waiting for the write lock
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
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    public T GetValue(int id)
    {
        _lock.EnterReadLock();
        try
        {
            return id >= 0 && id < _entries.Count ? _entries[id] : _defaultValue;
        }
        finally
        {
            _lock.ExitReadLock();
        }
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

    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _entries.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public bool IsSingleEntry { get; private set; }

    // Ensure proper disposal of the lock
    ~Palette()
    {
        _lock.Dispose();
    }
}