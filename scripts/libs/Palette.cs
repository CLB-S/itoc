// See also `ChunkData` for usage.

using System;
using System.Collections.Generic;

public class Palette<T> where T : IEquatable<T>
{
    private List<T> _entries = new List<T>();
    private int _bitsPerEntry;
    private ulong _mask;
    private T _defaultValue;

    public Palette(T defaultValue)
    {
        _bitsPerEntry = 4; // Default to 4 bits
        _mask = (1UL << _bitsPerEntry) - 1UL;
        _defaultValue = defaultValue;
        _entries.Add(defaultValue); // Default value by default
    }

    public int GetId(T value)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i] == null)
            {
                if (value == null) return i;
            }
            else if (_entries[i].Equals(value)) return i;
        }

        // Not found, add to palette
        int newId = _entries.Count;
        _entries.Add(value);

        // Check if we need more bits
        int requiredBits = (int)Math.Ceiling(Math.Log2(_entries.Count));
        if (requiredBits > _bitsPerEntry)
        {
            _bitsPerEntry = requiredBits;
            _mask = (1UL << _bitsPerEntry) - 1UL;
        }

        return newId;
    }

    public T GetValue(int id)
    {
        if (id < 0 || id >= _entries.Count) return _defaultValue;
        return _entries[id];
    }

    public int BitsPerEntry => _bitsPerEntry;
    public int Count => _entries.Count;
    public ulong Mask => _mask;
}
