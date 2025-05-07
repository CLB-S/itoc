using Godot;
using Palette;

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
    private readonly PaletteStorage<Block> _paletteStorage;

    private ChunkData()
    {
    }

    public ChunkData(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;

        var palette = new Palette<Block>(BlockManager.Instance.GetBlock("air"));
        _paletteStorage = new PaletteStorage<Block>(palette);
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
        var index = ChunkMesher.GetBlockAxisIndex(axis, a, b, c);
        return GetBlock(index);
    }

    public Block GetBlock(int x, int y, int z)
    {
        var index = ChunkMesher.GetBlockIndex(x, y, z);
        return GetBlock(index);
    }

    public Block GetBlock(Vector3I pos)
    {
        return GetBlock(pos.X, pos.Y, pos.Z);
    }

    public Block GetBlock(int index)
    {
        return _paletteStorage.Get(index);
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
        var index = ChunkMesher.GetBlockIndex(x, y, z);
        _paletteStorage.Set(index, block);

        SetMesherMask(x + 1, y + 1, z + 1, block);
    }

    public void SetMesherMask(int x, int y, int z, Block block)
    {
        if (block == null || !block.IsOpaque)
            ChunkMesher.AddNonOpaqueVoxel(OpaqueMask, x, y, z);
        else
            ChunkMesher.AddOpaqueVoxel(OpaqueMask, x, y, z);

        if (block != null && !block.IsOpaque)
        {
            TransparentMasks ??= new ulong[ChunkMesher.CS_P2];

            ChunkMesher.AddOpaqueVoxel(TransparentMasks, x, y, z);
        }
    }

    public void SetBlock(Vector3I pos, Block block)
    {
        SetBlock(pos.X, pos.Y, pos.Z, block);
    }

    public int GetBytes()
    {
        return _paletteStorage.GetStorageSize() * sizeof(ulong) +
                +OpaqueMask.Length * sizeof(ulong);
    }
}