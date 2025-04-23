using System.Collections.Generic;
using Godot;

public class ChunkData
{
    public readonly int X;
    public readonly int Y;
    public readonly int Z;

    /// <summary>
    ///     Mask for opaque blocks.
    /// </summary>
    public ulong[] OpaqueMask = new ulong[ChunkMesher.CS_P2];

    public ulong[] TransparentMasks;
    private readonly Palette<Block> _palette = new(null);
    private readonly List<ulong> _data = new();
    private readonly int _entriesPerLong;

    private ChunkData()
    {
    }

    public ChunkData(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
        _entriesPerLong = 64 / _palette.BitsPerEntry;

        // Initialize with all air blocks
        var totalEntries = ChunkMesher.CS_P3;
        var longCount = (totalEntries + _entriesPerLong - 1) / _entriesPerLong;
        _data = new List<ulong>(new ulong[longCount]);
    }

    public ChunkData(Vector3I pos) : this(pos.X, pos.Y, pos.Z)
    {
    }

    public Vector3I GetPosition()
    {
        return new Vector3I(X, Y, Z);
    }

    public Block GetBlock(int axis, int a, int b, int c)
    {
        var index = ChunkMesher.GetAxisIndex(axis, a, b, c);
        return GetBlock(index);
    }

    public Block GetBlock(int x, int y, int z)
    {
        var index = ChunkMesher.GetIndex(x, y, z);
        return GetBlock(index);
    }

    public Block GetBlock(Vector3I pos)
    {
        return GetBlock(pos.X, pos.Y, pos.Z);
    }

    public Block GetBlock(int index)
    {
        var longIndex = index / _entriesPerLong;
        var bitOffset = index % _entriesPerLong * _palette.BitsPerEntry;

        if (longIndex >= _data.Count) return null;

        var value = (_data[longIndex] >> bitOffset) & _palette.Mask;
        return _palette.GetValue((int)value);
    }


    public void SetBlock(int x, int y, int z, string blockId)
    {
        var block = BlockManager.Instance.GetBlock(blockId);
        SetBlock(x, y, z, block);
    }

    public void SetBlock(Vector3I pos, string blockId)
    {
        SetBlock(pos.X, pos.Y, pos.Z, blockId);
    }

    public void SetBlock(int x, int y, int z, Block block)
    {
        var index = ChunkMesher.GetIndex(x, y, z);
        var longIndex = index / _entriesPerLong;
        var bitOffset = index % _entriesPerLong * _palette.BitsPerEntry;

        var paletteId = _palette.GetId(block);

        // Check if we need to resize the data array
        if (longIndex >= _data.Count)
        {
            var needed = longIndex - _data.Count + 1;
            _data.AddRange(new ulong[needed]);
        }

        // Clear existing bits
        _data[longIndex] &= ~(_palette.Mask << bitOffset);
        // Set new bits
        _data[longIndex] |= ((ulong)paletteId & _palette.Mask) << bitOffset;

        if (block == null || !block.IsOpaque)
            ChunkMesher.AddNonOpaqueVoxel(OpaqueMask, x, y, z);
        else
            ChunkMesher.AddOpaqueVoxel(OpaqueMask, x, y, z);

        if (block != null && !block.IsOpaque)
        {
            if (TransparentMasks == null)
                TransparentMasks = new ulong[ChunkMesher.CS_P2];

            ChunkMesher.AddOpaqueVoxel(TransparentMasks, x, y, z);
        }
    }

    public void SetBlock(Vector3I pos, Block block)
    {
        SetBlock(pos.X, pos.Y, pos.Z, block);
    }

    public int GetBytes()
    {
        return (_data.Count * sizeof(ulong)
                + OpaqueMask.Length * sizeof(ulong)) / 8;
    }
}