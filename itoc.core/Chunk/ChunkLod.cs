using Godot;
using ITOC.Core.ChunkMeshing;

namespace ITOC.Core;

public class ChunkLod : Chunk
{
    public bool Visible = false;
    public bool AllChildrenLoaded =>
        _childChunks[0, 0, 0] != null
        && _childChunks[0, 0, 1] != null
        && _childChunks[0, 1, 0] != null
        && _childChunks[0, 1, 1] != null
        && _childChunks[1, 0, 0] != null
        && _childChunks[1, 0, 1] != null
        && _childChunks[1, 1, 0] != null
        && _childChunks[1, 1, 1] != null;

    public int ChildCount => _childChunks.Cast<Chunk>().Count(c => c != null);

    // Reference to the child chunks that make up this chunk (can be either Chunk or ChunkLod objects)
    private readonly Chunk[,,] _childChunks;

    public ChunkLod(int x, int y, int z, int lodLevel)
        : base(x, y, z)
    {
        Lod = lodLevel;
        _childChunks = new Chunk[2, 2, 2];

        // GD.Print($"ChunkLod created at {x}, {y}, {z} with LOD {lodLevel}");
    }

    public ChunkLod(Vector3I pos, int lodLevel)
        : this(pos.X, pos.Y, pos.Z, lodLevel) { }

    #region Get

    public override MeshBuffer GetMeshBuffer()
    {
        if (State != ChunkState.Ready)
            throw new InvalidOperationException("Chunk is not ready.");

        _lock.EnterReadLock();
        try
        {
            // Create a copy of the mesh data to avoid thread safety issues
            var opaqueMaskCopy = new ulong[_opaqueMask.Length];
            Array.Copy(_opaqueMask, opaqueMaskCopy, _opaqueMask.Length);
            return new MeshBuffer(opaqueMaskCopy);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    #endregion

    #region Set

    protected override void SetMesherMaskInternal(int x, int y, int z, Block block)
    {
        // This method assumes the write lock is already held
        if (block.IsOpaque)
            ChunkMesher.AddOpaqueVoxel(_opaqueMask, x, y, z);
        else
            ChunkMesher.AddNonOpaqueVoxel(_opaqueMask, x, y, z);
    }

    #endregion

    #region Utils

    public IEnumerable<Chunk> GetChildChunks()
    {
        for (var x = 0; x < 2; x++)
        for (var y = 0; y < 2; y++)
        for (var z = 0; z < 2; z++)
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
                var lx = Mathf.FloorToInt(e.UpdatePosition.X / 2.0) + (x * SIZE / 2);
                var ly = Mathf.FloorToInt(e.UpdatePosition.Y / 2.0) + (y * SIZE / 2);
                var lz = Mathf.FloorToInt(e.UpdatePosition.Z / 2.0) + (z * SIZE / 2);

                var block = GetBlockFromHigherLod(lx, ly, lz, chunk);
                SetBlock(lx, ly, lz, block);

                // GD.Print($"Block updated at {lx}, {ly}, {lz} for ChunkLod {Index} with LOD {Lod}");
            };
        }

        SetBlocksFromChildChunk(x, y, z);
    }

    public void SetOrUpdateChildChunk(Vector3I pos, Chunk chunk) =>
        SetOrUpdateChildChunk(pos.X, pos.Y, pos.Z, chunk);

    private void SetBlocksFromChildChunk(int childX, int childY, int childZ)
    {
        var chunk = _childChunks[childX, childY, childZ];
        if (chunk == null)
            return;

        // Calculate the base position in this LOD chunk corresponding to the child chunk
        var baseX = childX * (SIZE / 2);
        var baseY = childY * (SIZE / 2);
        var baseZ = childZ * (SIZE / 2);

        // For each 2x2x2 group of blocks in the child chunk, determine the dominant block type
        var blocksToUpdate = new List<(Vector3I, Block)>();

        // Check if the child chunk is a ChunkLod
        var blocks = chunk.GetBlocks();
        if (chunk is ChunkLod childLod)
        {
            // Only update blocks where the child ChunkLod has loaded children
            for (var cx = 0; cx < 2; cx++)
            for (var cy = 0; cy < 2; cy++)
            for (var cz = 0; cz < 2; cz++)
            {
                var childChunk = childLod._childChunks[cx, cy, cz];
                if (childChunk == null)
                    continue;

                // Calculate the start position for this sub-chunk in our coordinate system
                var startX = baseX + cx * (SIZE / 4);
                var startY = baseY + cy * (SIZE / 4);
                var startZ = baseZ + cz * (SIZE / 4);

                // Update the corresponding blocks in this LOD
                for (var x = 0; x <= SIZE / 4; x++)
                for (var y = 0; y <= SIZE / 4; y++)
                for (var z = 0; z <= SIZE / 4; z++)
                {
                    // var block = GetBlockFromHigherLod(startX + x, startY + y, startZ + z, childChunk);
                    var block = GetBlockFromHigherLod(
                        startX + x,
                        startY + y,
                        startZ + z,
                        blocks: blocks
                    );
                    blocksToUpdate.Add((new Vector3I(startX + x, startY + y, startZ + z), block));
                }
            }
        }
        else
        {
            // Original logic for regular chunks
            for (var x = 0; x < SIZE / 2; x++)
            for (var y = 0; y < SIZE / 2; y++)
            for (var z = 0; z < SIZE / 2; z++)
            {
                var block = GetBlockFromHigherLod(baseX + x, baseY + y, baseZ + z, blocks: blocks);
                // var block = GetBlockFromHigherLod(baseX + x, baseY + y, baseZ + z, chunk);
                blocksToUpdate.Add((new Vector3I(baseX + x, baseY + y, baseZ + z), block));
            }
        }

        SetBlocks(blocksToUpdate, false);

        State = ChunkState.Ready;
    }

    public Block GetBlockFromHigherLod(
        int x,
        int y,
        int z,
        Chunk childChunk = null,
        Block[] blocks = null
    )
    {
        // Normalize the coordinates relative to the child chunk
        var localX = x % (SIZE / 2);
        var localY = y % (SIZE / 2);
        var localZ = z % (SIZE / 2);

        var useBlocks = blocks != null && blocks.Length == SIZE_3;

        if (!useBlocks && childChunk == null)
        {
            // If no child chunk is provided, use the one based on the coordinates
            var childX = x / (SIZE / 2);
            var childY = y / (SIZE / 2);
            var childZ = z / (SIZE / 2);

            childChunk = _childChunks[childX, childY, childZ];

            // Skip if the child chunk isn't loaded
            if (childChunk == null)
                return null;
        }

        // Count occurrences of each block type in the 2x2x2 region
        var blockCounts = new Dictionary<Block, double>();
        var airCount = 0;

        for (var dx = 0; dx < 2; dx++)
        for (var dy = 0; dy < 2; dy++)
        for (var dz = 0; dz < 2; dz++)
        {
            Block block;
            if (useBlocks)
                block = blocks[
                    ChunkMesher.GetBlockIndex(localX * 2 + dx, localY * 2 + dy, localZ * 2 + dz)
                ];
            else
                block = childChunk.GetBlock(localX * 2 + dx, localY * 2 + dy, localZ * 2 + dz);

            if (block == Block.Air)
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
            return Block.Air;

        // Find the dominant block type
        var dominantBlock = Block.Air;
        var maxCount = 0.0;

        foreach (var pair in blockCounts)
            if (pair.Value > maxCount)
            {
                maxCount = pair.Value;
                dominantBlock = pair.Key;
            }

        return dominantBlock;
    }

    public void RemoveChildChunk(int x, int y, int z) =>
        // Just set the child chunk to null. Keep the LOD blocks.
        _childChunks[x, y, z] = null;

    public void RemoveChildChunk(Vector3I pos) => RemoveChildChunk(pos.X, pos.Y, pos.Z);

    #endregion
}
