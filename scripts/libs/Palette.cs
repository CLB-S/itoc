// See also `ChunkData` for usage.

using System;
using System.Collections.Generic;

public class Palette<T> where T : IEquatable<T>
{
    private readonly List<T> _entries = new();
    private readonly T _defaultValue;

    public Palette(T defaultValue)
    {
        BitsPerEntry = 4; // Default to 4 bits
        Mask = (1UL << BitsPerEntry) - 1UL;
        _defaultValue = defaultValue;
        _entries.Add(defaultValue); // Default value by default
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

        // Not found, add to palette
        var newId = _entries.Count;
        _entries.Add(value);

        // Check if we need more bits
        var requiredBits = (int)Math.Ceiling(Math.Log2(_entries.Count));
        if (requiredBits > BitsPerEntry)
        {
            BitsPerEntry = requiredBits;
            Mask = (1UL << BitsPerEntry) - 1UL;
        }

        return newId;
    }

    public T GetValue(int id)
    {
        if (id < 0 || id >= _entries.Count) return _defaultValue;
        return _entries[id];
    }

    public int BitsPerEntry { get; private set; }

    public int Count => _entries.Count;
    public ulong Mask { get; private set; }
}