using Godot;
using System;
using System.Collections.Generic;

public class Palette
{
    private List<string> _entries = new List<string>();
    private int _bitsPerEntry;
    private ulong _mask;

    public Palette()
    {
        _bitsPerEntry = 4; // Default to 4 bits
        _mask = (1UL << _bitsPerEntry) - 1UL;
        _entries.Add("itoc:air"); // Air by default
    }

    public int GetId(string blockId)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i] == blockId) return i;
        }

        // Not found, add to palette
        int newId = _entries.Count;
        _entries.Add(blockId);

        // Check if we need more bits
        int requiredBits = (int)Math.Ceiling(Math.Log2(_entries.Count));
        if (requiredBits > _bitsPerEntry)
        {
            _bitsPerEntry = requiredBits;
            _mask = (1UL << _bitsPerEntry) - 1UL;
        }

        return newId;
    }

    public string GetBlock(int id)
    {
        if (id < 0 || id >= _entries.Count) return "itoc:air"; // Return air if invalid
        return _entries[id];
    }

    public int BitsPerEntry => _bitsPerEntry;
    public int Count => _entries.Count;
    public ulong Mask => _mask;
}

public class ChunkData
{
    public readonly int X;
    public readonly int Y;
    public readonly int Z;

    private Palette _palette = new Palette();
    private List<ulong> _data = new List<ulong>();
    private int _entriesPerLong;

    private ChunkData() { }
    public ChunkData(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
        _entriesPerLong = 64 / _palette.BitsPerEntry;

        // Initialize with all air blocks
        int totalEntries = ChunkMesher.CS_P3;
        int longCount = (totalEntries + _entriesPerLong - 1) / _entriesPerLong;
        _data = new List<ulong>(new ulong[longCount]);
    }

    public ChunkData(Vector3I pos) : this(pos.X, pos.Y, pos.Z) { }

    public Vector3I GetPosition()
    {
        return new Vector3I(X, Y, Z);
    }

    public string GetBlock(int axis, int a, int b, int c)
    {
        int index = ChunkMesher.GetAxisIndex(axis, a, b, c);
        int longIndex = index / _entriesPerLong;
        int bitOffset = (index % _entriesPerLong) * _palette.BitsPerEntry;

        if (longIndex >= _data.Count) return "itoc:air";

        ulong value = (_data[longIndex] >> bitOffset) & _palette.Mask;
        return _palette.GetBlock((int)value);
    }

    public string GetBlock(int x, int y, int z)
    {
        int index = ChunkMesher.GetIndex(x, y, z);
        int longIndex = index / _entriesPerLong;
        int bitOffset = (index % _entriesPerLong) * _palette.BitsPerEntry;

        if (longIndex >= _data.Count) return "itoc:air";

        ulong value = (_data[longIndex] >> bitOffset) & _palette.Mask;
        return _palette.GetBlock((int)value);
    }

    public void SetBlock(int x, int y, int z, string blockId)
    {
        int index = ChunkMesher.GetIndex(x, y, z);
        int longIndex = index / _entriesPerLong;
        int bitOffset = (index % _entriesPerLong) * _palette.BitsPerEntry;

        string normalizedId = Block.NormalizeBlockId(blockId);
        int paletteId = _palette.GetId(normalizedId);

        // Check if we need to resize the data array
        if (longIndex >= _data.Count)
        {
            int needed = longIndex - _data.Count + 1;
            _data.AddRange(new ulong[needed]);
        }

        // Clear existing bits
        _data[longIndex] &= ~(_palette.Mask << bitOffset);
        // Set new bits
        _data[longIndex] |= ((ulong)paletteId & _palette.Mask) << bitOffset;
    }
}
