using Godot;
using System;
using System.Collections.Generic;

public class ChunkData
{
    public readonly int X;
    public readonly int Y;
    public readonly int Z;

    public ulong[] OpaqueMask = new ulong[ChunkMesher.CS_P2];
    private Palette<string> _palette = new Palette<string>("air");
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

        if (longIndex >= _data.Count) return "air";

        ulong value = (_data[longIndex] >> bitOffset) & _palette.Mask;
        return _palette.GetValue((int)value);
    }

    public string GetBlock(int x, int y, int z)
    {
        int index = ChunkMesher.GetIndex(x, y, z);
        int longIndex = index / _entriesPerLong;
        int bitOffset = (index % _entriesPerLong) * _palette.BitsPerEntry;

        if (longIndex >= _data.Count) return "air";

        ulong value = (_data[longIndex] >> bitOffset) & _palette.Mask;
        return _palette.GetValue((int)value);
    }

    public void SetBlock(int x, int y, int z, string blockId)
    {
        int index = ChunkMesher.GetIndex(x, y, z);
        int longIndex = index / _entriesPerLong;
        int bitOffset = (index % _entriesPerLong) * _palette.BitsPerEntry;

        int paletteId = _palette.GetId(blockId);

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

        if (Block.IsTransparent(blockId))
            ChunkMesher.AddNonOpaqueVoxel(OpaqueMask, x, y, z);
        else
            ChunkMesher.AddOpaqueVoxel(OpaqueMask, x, y, z);
    }

    public int GetBytes()
    {
        return (_data.Count * sizeof(ulong)
               + OpaqueMask.Length * sizeof(ulong)) / 8;
    }
}
