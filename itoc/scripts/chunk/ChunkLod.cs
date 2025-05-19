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

    public int ChildCount => _childChunks.Length;

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

    protected override void SetMesherMaskInternal(int x, int y, int z, Block block)
    {
        // This method assumes the write lock is already held
        if (block == null)
            ChunkMesher.AddNonOpaqueVoxel(_opaqueMask, x, y, z);
        else
            ChunkMesher.AddOpaqueVoxel(_opaqueMask, x, y, z);
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
    public void SetOrUpdateChildChunk(int x, int y, int z, Chunk chunk)
    {
        // var startTime = DateTime.Now;
        if (_childChunks[x, y, z] == null)
        {
            _childChunks[x, y, z] = chunk;

            chunk.OnBlockUpdated += (s, e) =>
            {
                var lx = Mathf.FloorToInt(e.UpdatePosition.X / 2.0) + (x * ChunkMesher.CS / 2);
                var ly = Mathf.FloorToInt(e.UpdatePosition.Y / 2.0) + (y * ChunkMesher.CS / 2);
                var lz = Mathf.FloorToInt(e.UpdatePosition.Z / 2.0) + (z * ChunkMesher.CS / 2);

                var block = GetBlockFromHigherLod(lx, ly, lz, chunk);
                SetBlock(lx, ly, lz, block);

                // GD.Print($"Block updated at {lx}, {ly}, {lz} for ChunkLod {Index} with LOD {Lod}");
            };
        }

        SetBlocksFromChildChunk(x, y, z);
    }

    public void SetOrUpdateChildChunk(Vector3I pos, Chunk chunk)
    {
        SetOrUpdateChildChunk(pos.X, pos.Y, pos.Z, chunk);
    }

    private void SetBlocksFromChildChunk(int childX, int childY, int childZ)
    {
        var chunk = _childChunks[childX, childY, childZ];
        if (chunk == null) return;

        // Calculate the base position in this LOD chunk corresponding to the child chunk
        int baseX = childX * (ChunkMesher.CS / 2);
        int baseY = childY * (ChunkMesher.CS / 2);
        int baseZ = childZ * (ChunkMesher.CS / 2);

        // For each 2x2x2 group of blocks in the child chunk, determine the dominant block type
        var blocksToUpdate = new List<(Vector3I, Block)>();

        // Check if the child chunk is a ChunkLod
        var blocks = chunk.GetBlocks();
        if (chunk is ChunkLod childLod)
        {
            // Only update blocks where the child ChunkLod has loaded children
            for (int cx = 0; cx < 2; cx++)
                for (int cy = 0; cy < 2; cy++)
                    for (int cz = 0; cz < 2; cz++)
                    {
                        var childChunk = childLod._childChunks[cx, cy, cz];
                        if (childChunk == null) continue;

                        // Calculate the start position for this sub-chunk in our coordinate system
                        int startX = baseX + cx * (ChunkMesher.CS / 4);
                        int startY = baseY + cy * (ChunkMesher.CS / 4);
                        int startZ = baseZ + cz * (ChunkMesher.CS / 4);

                        // Update the corresponding blocks in this LOD
                        for (int x = 0; x <= ChunkMesher.CS / 4; x++)
                            for (int y = 0; y <= ChunkMesher.CS / 4; y++)
                                for (int z = 0; z <= ChunkMesher.CS / 4; z++)
                                {
                                    // var block = GetBlockFromHigherLod(startX + x, startY + y, startZ + z, childChunk);
                                    var block = GetBlockFromHigherLod(startX + x, startY + y, startZ + z, blocks: blocks);
                                    blocksToUpdate.Add((new Vector3I(startX + x, startY + y, startZ + z), block));
                                }
                    }
        }
        else
        {
            // Original logic for regular chunks
            for (int x = 0; x < ChunkMesher.CS / 2; x++)
                for (int y = 0; y < ChunkMesher.CS / 2; y++)
                    for (int z = 0; z < ChunkMesher.CS / 2; z++)
                    {
                        var block = GetBlockFromHigherLod(baseX + x, baseY + y, baseZ + z, blocks: blocks);
                        // var block = GetBlockFromHigherLod(baseX + x, baseY + y, baseZ + z, chunk);
                        blocksToUpdate.Add((new Vector3I(baseX + x, baseY + y, baseZ + z), block));
                    }
        }

        SetBlocks(blocksToUpdate, false);

        State = ChunkState.Ready;
    }

    public Block GetBlockFromHigherLod(int x, int y, int z, Chunk childChunk = null, Block[] blocks = null)
    {
        // Normalize the coordinates relative to the child chunk
        int localX = x % (ChunkMesher.CS / 2);
        int localY = y % (ChunkMesher.CS / 2);
        int localZ = z % (ChunkMesher.CS / 2);

        var useBlocks = blocks != null && blocks.Length == ChunkMesher.CS_3;

        if (!useBlocks && childChunk == null)
        {
            // If no child chunk is provided, use the one based on the coordinates
            int childX = x / (ChunkMesher.CS / 2);
            int childY = y / (ChunkMesher.CS / 2);
            int childZ = z / (ChunkMesher.CS / 2);

            childChunk = _childChunks[childX, childY, childZ];

            // Skip if the child chunk isn't loaded
            if (childChunk == null)
                return null;
        }

        // Count occurrences of each block type in the 2x2x2 region
        var blockCounts = new Dictionary<Block, double>();
        int airCount = 0;

        for (int dx = 0; dx < 2; dx++)
            for (int dy = 0; dy < 2; dy++)
                for (int dz = 0; dz < 2; dz++)
                {
                    Block block;
                    if (useBlocks)
                        block = blocks[ChunkMesher.GetBlockIndex(localX * 2 + dx, localY * 2 + dy, localZ * 2 + dz)];
                    else
                        block = childChunk.GetBlock(localX * 2 + dx, localY * 2 + dy, localZ * 2 + dz);

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
            return null;

        // Find the dominant block type
        Block dominantBlock = null;
        var maxCount = 0.0;

        foreach (var pair in blockCounts)
            if (pair.Value > maxCount)
            {
                maxCount = pair.Value;
                dominantBlock = pair.Key;
            }

        return dominantBlock;
    }

    public void RemoveChildChunk(int x, int y, int z)
    {
        // Just set the child chunk to null. Keep the LOD blocks.
        _childChunks[x, y, z] = null;
    }

    public void RemoveChildChunk(Vector3I pos)
    {
        RemoveChildChunk(pos.X, pos.Y, pos.Z);
    }

    #endregion
}
