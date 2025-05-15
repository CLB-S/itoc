using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ITOC;

public class ChunkLod : Chunk
{
    public bool Visible = false;
    public bool AllChildrenLoaded =>
        _childChunks[0, 0, 0] != null &&
        _childChunks[0, 0, 1] != null &&
        _childChunks[0, 1, 0] != null &&
        _childChunks[0, 1, 1] != null &&
        _childChunks[1, 0, 0] != null &&
        _childChunks[1, 0, 1] != null &&
        _childChunks[1, 1, 0] != null &&
        _childChunks[1, 1, 1] != null;

    // Reference to the child chunks that make up this chunk (can be either Chunk or ChunkLod objects)
    private Chunk[,,] _childChunks;

    public ChunkLod(int x, int y, int z, int lodLevel) : base(x, y, z)
    {
        Lod = lodLevel;
        _childChunks = new Chunk[2, 2, 2];

        // GD.Print($"ChunkLod created at {x}, {y}, {z} with LOD {lodLevel}");
    }

    public ChunkLod(Vector3I pos, int lodLevel) : this(pos.X, pos.Y, pos.Z, lodLevel)
    {
    }

    #region Get

    public override ChunkMesher.MeshData GetRawMeshData()
    {
        if (State != ChunkState.Ready)
            throw new InvalidOperationException("Chunk is not ready.");

        return new ChunkMesher.MeshData(_opaqueMask) { Lod = Lod };
    }

    #endregion

    #region Set

    public override void SetMesherMask(int x, int y, int z, Block block)
    {
        lock (_lockObject)
        {
            if (block == null)
                ChunkMesher.AddNonOpaqueVoxel(_opaqueMask, x, y, z);
            else
                ChunkMesher.AddOpaqueVoxel(_opaqueMask, x, y, z);

            if (State == ChunkState.Ready)
                InvokeMeshUpdatedEvent();
        }
    }

    #endregion

    #region Utils

    public IEnumerable<Chunk> GetChildChunks()
    {
        for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
                for (int z = 0; z < 2; z++)
                    if (_childChunks[x, y, z] != null)
                        yield return _childChunks[x, y, z];
    }

    #endregion

    #region LOD Management
    public void SetChildChunk(int x, int y, int z, Chunk chunk)
    {
        _childChunks[x, y, z] = chunk;
        UpdateBlocksFromChildChunk(x, y, z);

        chunk.OnBlockUpdated += (s, e) =>
        {
            var lx = Mathf.FloorToInt(e.UpdatePosition.X / 2.0) + (x * ChunkMesher.CS / 2);
            var ly = Mathf.FloorToInt(e.UpdatePosition.Y / 2.0) + (y * ChunkMesher.CS / 2);
            var lz = Mathf.FloorToInt(e.UpdatePosition.Z / 2.0) + (z * ChunkMesher.CS / 2);

            UpdateBlockFromHigherLod(lx, ly, lz, chunk);
        };

        // GD.Print($"Child chunk set at {x}, {y}, {z} for ChunkLod {Index} with LOD {Lod}. Total children: {GetChildChunks().Count()}");
    }

    public void SetChildChunk(Vector3I pos, Chunk chunk)
    {
        SetChildChunk(pos.X, pos.Y, pos.Z, chunk);
    }

    private void UpdateBlocksFromChildChunk(int childX, int childY, int childZ)
    {
        var chunk = _childChunks[childX, childY, childZ];
        if (chunk == null) return;

        // Calculate the base position in this LOD chunk corresponding to the child chunk
        int baseX = childX * (ChunkMesher.CS / 2);
        int baseY = childY * (ChunkMesher.CS / 2);
        int baseZ = childZ * (ChunkMesher.CS / 2);

        // For each 2x2x2 group of blocks in the child chunk, determine the dominant block type
        for (int x = 0; x < ChunkMesher.CS / 2; x++)
            for (int y = 0; y < ChunkMesher.CS / 2; y++)
                for (int z = 0; z < ChunkMesher.CS / 2; z++)
                    UpdateBlockFromHigherLod(baseX + x, baseY + y, baseZ + z, chunk);

        State = ChunkState.Ready;
    }

    public void UpdateBlockFromHigherLod(int x, int y, int z, Chunk childChunk = null)
    {
        // Normalize the coordinates relative to the child chunk
        int localX = x % (ChunkMesher.CS / 2);
        int localY = y % (ChunkMesher.CS / 2);
        int localZ = z % (ChunkMesher.CS / 2);

        if (childChunk == null)
        {
            // If no child chunk is provided, use the one based on the coordinates
            int childX = x / (ChunkMesher.CS / 2);
            int childY = y / (ChunkMesher.CS / 2);
            int childZ = z / (ChunkMesher.CS / 2);

            childChunk = _childChunks[childX, childY, childZ];

            // Skip if the child chunk isn't loaded
            if (childChunk == null)
                return;
        }

        // Count occurrences of each block type in the 2x2x2 region
        var blockCounts = new Dictionary<Block, double>();
        int airCount = 0;

        for (int dx = 0; dx < 2; dx++)
            for (int dy = 0; dy < 2; dy++)
                for (int dz = 0; dz < 2; dz++)
                {
                    Block block = childChunk.GetBlock(localX * 2 + dx, localY * 2 + dy, localZ * 2 + dz);

                    if (block == null)
                        airCount++;
                    else
                    {
                        if (!blockCounts.ContainsKey(block))
                            blockCounts[block] = 0;
                        blockCounts[block] += 1 + dy * 0.25;
                    }
                }

        // If all blocks are air, set this block to air
        if (airCount == 8)
        {
            SetBlock(x, y, z, (Block)null);
            return;
        }

        // Find the dominant block type
        Block dominantBlock = null;
        var maxCount = 0.0;

        foreach (var pair in blockCounts)
            if (pair.Value > maxCount)
            {
                maxCount = pair.Value;
                dominantBlock = pair.Key;
            }

        SetBlock(x, y, z, dominantBlock);
    }

    #endregion
}
