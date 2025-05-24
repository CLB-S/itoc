using System;
using System.Threading;
using System.Collections.Generic;
using Godot;
using ITOC.Libs.Palette;

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
    public int Lod { get; protected set; } = 0;
    public Vector3I Index { get; protected set; }
    public ChunkState State { get; set; }
    public Vector3 Position => Index * ChunkMesher.CS * (1 << Lod);
    public Vector3 CenterPosition => Position + Vector3I.One * (ChunkMesher.CS * (1 << Lod) / 2);
    public Vector3 Size => Vector3I.One * (ChunkMesher.CS * (1 << Lod));

    public event EventHandler<OnBlockUpdatedEventArgs> OnBlockUpdated;
    public event EventHandler OnMeshUpdated;

    /// <summary>
    ///     Mask for opaque blocks.
    /// </summary>
    protected readonly ulong[] _opaqueMask = new ulong[ChunkMesher.CS_P2];

    protected ulong[] _transparentMasks;
    protected readonly PaletteStorage<Block> _paletteStorage; // Storage for all blocks 
    protected readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

    public Chunk(int x, int y, int z, Block[] blocks = null)
    {
        if (blocks != null)
        {
            if (blocks.Length != ChunkMesher.CS_3)
                throw new ArgumentException($"Blocks array must be of size {ChunkMesher.CS_3}");

            var palette = new Palette<Block>(BlockManager.Instance.GetBlock("itoc:air"));
            _paletteStorage = new PaletteStorage<Block>(palette, blocks);

            for (var i = 0; i < blocks.Length; i++)
            {
                var block = blocks[i];
                (var ix, var iy, var iz) = ChunkMesher.GetBlockIndex(i);

                if (block != null)
                    SetMesherMaskInternal(ix + 1, iy + 1, iz + 1, block);
            }
        }
        else
        {
            var palette = new Palette<Block>(BlockManager.Instance.GetBlock("itoc:air"));
            _paletteStorage = new PaletteStorage<Block>(palette);
        }

        Index = new Vector3I(x, y, z);

        State = ChunkState.Created;
    }

    public Chunk(Vector3I index, Block[] blocks = null) : this(index.X, index.Y, index.Z, blocks)
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
        _lock.EnterReadLock();
        try
        {
            return _paletteStorage.Get(index);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Dictionary<Vector3I, Block> GetBlocks(Vector3I start, Vector3I end)
    {
        // Ensure start coordinates are less than or equal to end coordinates
        var min = new Vector3I(
            Mathf.Min(start.X, end.X),
            Mathf.Min(start.Y, end.Y),
            Mathf.Min(start.Z, end.Z)
        );

        var max = new Vector3I(
            Mathf.Max(start.X, end.X),
            Mathf.Max(start.Y, end.Y),
            Mathf.Max(start.Z, end.Z)
        );

        // Clamp to chunk bounds
        min = ClampPositionToChunk(min);
        max = ClampPositionToChunk(max);

        var result = new Dictionary<Vector3I, Block>();

        _lock.EnterReadLock();
        try
        {
            for (int x = min.X; x <= max.X; x++)
                for (int y = min.Y; y <= max.Y; y++)
                    for (int z = min.Z; z <= max.Z; z++)
                    {
                        var position = new Vector3I(x, y, z);
                        var index = ChunkMesher.GetBlockIndex(position);
                        result[position] = _paletteStorage.Get(index);
                    }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        return result;
    }

    public Dictionary<Vector3I, Block> GetBlocks(IEnumerable<Vector3I> positions)
    {
        var result = new Dictionary<Vector3I, Block>();

        _lock.EnterReadLock();
        try
        {
            foreach (var position in positions)
            {
                if (!IsPositionInChunk(position))
                    continue;

                var index = ChunkMesher.GetBlockIndex(position);
                result[position] = _paletteStorage.Get(index);
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        return result;
    }

    public Block[] GetBlocks()
    {
        var blocks = new Block[ChunkMesher.CS_3];

        _lock.EnterReadLock();
        try
        {
            for (var i = 0; i < blocks.Length; i++)
                blocks[i] = _paletteStorage.Get(i);
        }
        finally
        {
            _lock.ExitReadLock();
        }

        return blocks;
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

    public virtual Mesh GetMesh(Material materialOverride = null)
    {
        var meshData = GetRawMeshData();
        ChunkMesher.MeshChunk(this, meshData);
        return ChunkMesher.GenerateMesh(meshData, materialOverride);
    }

    public virtual Shape3D GetCollisionShape()
    {
        var meshData = GetRawMeshData();
        ChunkMesher.MeshChunk(this, meshData, true);
        return ChunkMesher.GenerateMesh(meshData).CreateTrimeshShape();
    }

    public virtual int GetBytes()
    {
        return _paletteStorage.StorageSize * sizeof(ulong) +
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
        if (!IsPositionInChunk(new Vector3I(x, y, z)))
            return;

        var index = ChunkMesher.GetBlockIndex(x, y, z);

        Block oldBlock;

        _lock.EnterUpgradeableReadLock();
        try
        {
            oldBlock = _paletteStorage.Get(index);
            if (oldBlock == block)
                return;

            _lock.EnterWriteLock();
            try
            {
                _paletteStorage.Set(index, block);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }

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

    public virtual void SetBlocks(IEnumerable<(Vector3I Position, Block Block)> blocks, bool triggerEvents = true)
    {
        var entriesForPalette = new List<(int Index, Block Block)>();
        var positionsToUpdate = new List<(int X, int Y, int Z, Block Block)>();
        var blockUpdates = new List<OnBlockUpdatedEventArgs>();

        _lock.EnterUpgradeableReadLock();
        try
        {
            foreach (var (pos, block) in blocks)
            {
                if (!IsPositionInChunk(pos))
                    continue;

                var index = ChunkMesher.GetBlockIndex(pos.X, pos.Y, pos.Z);
                var oldBlock = _paletteStorage.Get(index);
                if (oldBlock == block)
                    continue;

                entriesForPalette.Add((index, block));
                positionsToUpdate.Add((pos.X + 1, pos.Y + 1, pos.Z + 1, block));

                if (State == ChunkState.Ready)
                    blockUpdates.Add(new OnBlockUpdatedEventArgs(pos, oldBlock, block));
            }

            if (entriesForPalette.Count > 0)
            {
                _lock.EnterWriteLock();
                try
                {
                    _paletteStorage.SetRange(entriesForPalette);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            else
            {
                return; // No changes to make
            }
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }

        // Update mesher masks
        _lock.EnterWriteLock();
        try
        {
            foreach (var (x, y, z, block) in positionsToUpdate)
                SetMesherMaskInternal(x, y, z, block);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        // Trigger events if the chunk is ready
        if (triggerEvents && State == ChunkState.Ready)
        {
            foreach (var update in blockUpdates)
                OnBlockUpdated?.Invoke(this, update);

            if (blockUpdates.Count > 0)
                OnMeshUpdated?.Invoke(this, EventArgs.Empty);
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