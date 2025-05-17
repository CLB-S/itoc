using System;
using System.Threading;
using System.Collections.Generic;
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

public class Chunk : IDisposable
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
    protected readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

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
        return _paletteStorage.Get(index);
    }

    public virtual ChunkMesher.MeshData GetRawMeshData()
    {
        if (State != ChunkState.Ready)
            throw new InvalidOperationException("Chunk is not ready.");

        _lock.EnterReadLock();
        try
        {
            // Create a copy of the mesh data to avoid thread safety issues
            ulong[] opaqueMaskCopy = new ulong[_opaqueMask.Length];
            Array.Copy(_opaqueMask, opaqueMaskCopy, _opaqueMask.Length);

            ulong[] transparentMasksCopy = null;
            if (_transparentMasks != null)
            {
                transparentMasksCopy = new ulong[_transparentMasks.Length];
                Array.Copy(_transparentMasks, transparentMasksCopy, _transparentMasks.Length);
            }

            return new ChunkMesher.MeshData(opaqueMaskCopy, transparentMasksCopy);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public virtual Mesh GetMesh()
    {
        var meshData = GetRawMeshData();
        ChunkMesher.MeshChunk(this, meshData);
        return ChunkMesher.GenerateMesh(meshData);
    }

    public virtual int GetBytes()
    {
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

    public void SetBlock(Vector3I pos, Block block)
    {
        SetBlock(pos.X, pos.Y, pos.Z, block);
    }

    public void SetMesherMask(int x, int y, int z, Block block)
    {
        _lock.EnterWriteLock();
        try
        {
            SetMesherMaskInternal(x, y, z, block);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        if (State == ChunkState.Ready)
            OnMeshUpdated?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void SetMesherMaskInternal(int x, int y, int z, Block block)
    {
        // This method assumes the write lock is already held
        if (block == null || !block.IsOpaque)
            ChunkMesher.AddNonOpaqueVoxel(_opaqueMask, x, y, z);
        else
            ChunkMesher.AddOpaqueVoxel(_opaqueMask, x, y, z);

        if (block != null && !block.IsOpaque)
        {
            _transparentMasks ??= new ulong[ChunkMesher.CS_P2];
            ChunkMesher.AddOpaqueVoxel(_transparentMasks, x, y, z);
        }
    }

    #endregion

    #region Helpers
    private bool IsPositionInChunk(Vector3I pos)
    {
        return pos.X >= 0 && pos.X < ChunkMesher.CS &&
               pos.Y >= 0 && pos.Y < ChunkMesher.CS &&
               pos.Z >= 0 && pos.Z < ChunkMesher.CS;
    }

    private Vector3I ClampPositionToChunk(Vector3I pos)
    {
        return new Vector3I(
            Mathf.Clamp(pos.X, 0, ChunkMesher.CS - 1),
            Mathf.Clamp(pos.Y, 0, ChunkMesher.CS - 1),
            Mathf.Clamp(pos.Z, 0, ChunkMesher.CS - 1)
        );
    }
    #endregion

    #region IDisposable
    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _lock.Dispose();
        }

        _disposed = true;
    }

    ~Chunk()
    {
        Dispose(false);
    }
    #endregion
}