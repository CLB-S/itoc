using System;
using Godot;
using Palette;

namespace ITOC;

public class OnBlockUpdatedEventArgs : EventArgs
{
    public Vector3I UpdatePosition { get; private set; }
    public Block UpdateSourceBlock { get; private set; }
    public Block UpdateTargetBlock { get; private set; }

    public OnBlockUpdatedEventArgs(Vector3I updatePosition, Block updateSourceBlock, Block updateTargetBlock)
    {
        UpdatePosition = updatePosition;
        UpdateSourceBlock = updateSourceBlock;
        UpdateTargetBlock = updateTargetBlock;
    }
}

public enum ChunkState
{
    Created,
    Ready
}

public class Chunk
{
    public readonly Vector3I Position;
    public ChunkState State { get; set; }
    public Vector3 WorldPosition => Position * ChunkMesher.CS;
    public Vector3 CenterPosition => Position * ChunkMesher.CS + Vector3I.One * (ChunkMesher.CS / 2);

    public event EventHandler<OnBlockUpdatedEventArgs> OnBlockUpdated;
    public event EventHandler OnMeshUpdated;

    /// <summary>
    ///     Mask for opaque blocks.
    /// </summary>
    private readonly ulong[] _opaqueMask = new ulong[ChunkMesher.CS_P2];

    private ulong[] _transparentMasks;
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

        State = ChunkState.Created;
    }

    public Chunk(Vector3I pos) : this(pos.X, pos.Y, pos.Z)
    {
    }

    #region Get
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

    public Mesh GetMesh()
    {
        if (State != ChunkState.Ready)
            throw new InvalidOperationException("Chunk is not ready.");

        var meshData = new ChunkMesher.MeshData(_opaqueMask, _transparentMasks);
        ChunkMesher.MeshChunk(this, meshData);
        return ChunkMesher.GenerateMesh(meshData);
    }

    public int GetBytes()
    {
        lock (_lockObject)
            return _paletteStorage.GetStorageSize() * sizeof(ulong) +
                   _opaqueMask.Length * sizeof(ulong);
    }

    public double GetDistanceTo(Vector3 pos)
    {
        return CenterPosition.DistanceTo(pos);
    }

    #endregion

    #region Set
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
            var oldBlock = _paletteStorage.Get(index);
            if (oldBlock == block)
                return;

            _paletteStorage.Set(index, block);
            SetMesherMask(x + 1, y + 1, z + 1, block);

            if (State == ChunkState.Ready)
                OnBlockUpdated?.Invoke(this,
                    new OnBlockUpdatedEventArgs(new Vector3I(x, y, z), oldBlock, block));
        }
    }

    public void SetMesherMask(int x, int y, int z, Block block)
    {
        lock (_lockObject)
        {
            if (block == null || !block.IsOpaque)
                ChunkMesher.AddNonOpaqueVoxel(_opaqueMask, x, y, z);
            else
                ChunkMesher.AddOpaqueVoxel(_opaqueMask, x, y, z);

            if (block != null && !block.IsOpaque)
            {
                _transparentMasks ??= new ulong[ChunkMesher.CS_P2];
                ChunkMesher.AddOpaqueVoxel(_transparentMasks, x, y, z);
            }

            if (State == ChunkState.Ready)
                OnMeshUpdated?.Invoke(this, EventArgs.Empty);
        }
    }

    public void SetBlock(Vector3I pos, Block block)
    {
        SetBlock(pos.X, pos.Y, pos.Z, block);
    }

    #endregion
}