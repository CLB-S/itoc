using Godot;
using Palette;

public class Chunk
{
    public readonly Vector3I Position;

    /// <summary>
    ///     Mask for opaque blocks.
    /// </summary>
    public ulong[] OpaqueMask = new ulong[ChunkMesher.CS_P2];

    public ulong[] TransparentMasks;
    private readonly PaletteStorage<Block> _paletteStorage;

    // Lock object for thread synchronization
    private readonly object _lockObject = new object();

    private Chunk()
    {
    }

    public Chunk(int x, int y, int z)
    {
        Position = new Vector3I(x, y, z);

        var palette = new Palette<Block>(BlockManager.Instance.GetBlock("air"));
        _paletteStorage = new PaletteStorage<Block>(palette);
    }

    public Chunk(Vector3I pos) : this(pos.X, pos.Y, pos.Z)
    {
    }

    public Vector3I GetPosition()
    {
        return Position;
    }

    public Vector2I GetChunkColumnPosition()
    {
        return new Vector2I(Position.X, Position.Z);
    }

    public ChunkColumn GetChunkColumn()
    {
        return Core.Instance.CurrentWorld.GetChunkColumn(GetChunkColumnPosition());
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
        lock (_lockObject)
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
        lock (_lockObject)
        {
            var index = ChunkMesher.GetBlockIndex(x, y, z);
            _paletteStorage.Set(index, block);

            SetMesherMask(x + 1, y + 1, z + 1, block);
        }
    }

    public void SetMesherMask(int x, int y, int z, Block block)
    {
        lock (_lockObject)
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
    }

    public void SetBlock(Vector3I pos, Block block)
    {
        SetBlock(pos.X, pos.Y, pos.Z, block);
    }

    public int GetBytes()
    {
        lock (_lockObject)
            return _paletteStorage.GetStorageSize() * sizeof(ulong) +
                   OpaqueMask.Length * sizeof(ulong);
    }
}