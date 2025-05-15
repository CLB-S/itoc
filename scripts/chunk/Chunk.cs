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
    public Vector3I Index { get; protected set; }
    public ChunkState State { get; set; }
    public Vector3 WorldPosition => Index * ChunkMesher.CS;
    public Vector3 CenterPosition => Index * ChunkMesher.CS + Vector3I.One * (ChunkMesher.CS / 2);

    public int Lod { get; protected set; } = 0;

    public event EventHandler<OnBlockUpdatedEventArgs> OnBlockUpdated;
    public event EventHandler OnMeshUpdated;

    /// <summary>
    ///     Mask for opaque blocks.
    /// </summary>
    protected readonly ulong[] _opaqueMask = new ulong[ChunkMesher.CS_P2];

    protected ulong[] _transparentMasks;
    protected readonly PaletteStorage<Block> _paletteStorage; // Storage for all blocks 

    // Lock object for thread synchronization
    protected readonly object _lockObject = new object();

    public Chunk(int x, int y, int z)
    {
        Index = new Vector3I(x, y, z);

        var palette = new Palette<Block>(BlockManager.Instance.GetBlock("air"));
        _paletteStorage = new PaletteStorage<Block>(palette);

        State = ChunkState.Created;
    }

    public Chunk(Vector3I index) : this(index.X, index.Y, index.Z)
    {
    }

    protected void InvokeMeshUpdatedEvent()
    {
        OnMeshUpdated?.Invoke(this, EventArgs.Empty);
    }

    #region Get
    public virtual Vector2I GetChunkColumnPosition()
    {
        return new Vector2I(Index.X, Index.Z);
    }

    public virtual ChunkColumn GetChunkColumn()
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

    public virtual Block GetBlock(int index)
    {
        lock (_lockObject)
            return _paletteStorage.Get(index);
    }

    public virtual ChunkMesher.MeshData GetRawMeshData()
    {
        if (State != ChunkState.Ready)
            throw new InvalidOperationException("Chunk is not ready.");

        return new ChunkMesher.MeshData(_opaqueMask, _transparentMasks);
    }

    public virtual Mesh GetMesh()
    {
        var meshData = GetRawMeshData();
        ChunkMesher.MeshChunk(this, meshData);
        return ChunkMesher.GenerateMesh(meshData);
    }

    public virtual int GetBytes()
    {
        lock (_lockObject)
            return _paletteStorage.GetStorageSize() * sizeof(ulong) +
                   _opaqueMask.Length * sizeof(ulong)
                     + (_transparentMasks?.Length ?? 0) * sizeof(ulong);
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

    public virtual void SetBlock(int x, int y, int z, Block block)
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

    public void SetBlock(Vector3I pos, Block block)
    {
        SetBlock(pos.X, pos.Y, pos.Z, block);
    }

    public virtual void SetMesherMask(int x, int y, int z, Block block)
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

    #endregion
}