using System;
using System.Collections.Generic;
using System.Threading;
using Godot;
using ITOC.Multithreading;

namespace ITOC.ChunkGeneration;

public class ChunkGenerationTask : GameTask
{
    public readonly WorldGenerator WorldGenerator;
    public Vector3I ChunkIndex { get; }
    public ChunkColumn ChunkColumn { get; }
    public Action<Chunk> Callback { get; }
    private readonly Chunk _chunk;

    public ChunkGenerationTask(
        WorldGenerator worldGenerator,
        Vector3I index,
        ChunkColumn chunkColumn,
        Action<Chunk> callback = null,
        string name = null,
        TaskPriority priority = TaskPriority.Normal)
        : base(name, priority)
    {
        WorldGenerator = worldGenerator;
        ChunkIndex = index;
        ChunkColumn = chunkColumn;
        Callback = callback;
        _chunk = new Chunk(ChunkIndex);
    }

    protected override void ExecuteCore(CancellationToken cancellationToken)
    {
        SetBlocksByHeightMap();
        Callback?.Invoke(_chunk);
    }

    private void SetBlocksByHeightMap()
    {
        // var debugBlock = BlockManager.Instance.GetBlock("dirt");
        var waterBlock = BlockManager.Instance.GetBlock("water");
        // var blockUpdates = new List<(Vector3I Position, Block Block)>();

        for (var x = 0; x < ChunkMesher.CS; x++)
            for (var z = 0; z < ChunkMesher.CS; z++)
            {
                var height = Mathf.FloorToInt(ChunkColumn.HeightMap[x, z]);

                // Calculate slope steepness
                // var maxSlope = CalculateSlope(x, z);

                // var baseDirtDepth = Mathf.Clamp(4 - Mathf.FloorToInt(maxSlope), 1, 4);
                for (var y = 0; y < ChunkMesher.CS; y++)
                {
                    var actualY = ChunkIndex.Y * ChunkMesher.CS + y;
                    if (actualY <= height)
                    {
                        var blockType = DetermineBlockType(actualY, height, 0, 4);

                        // blockUpdates.Add((new Vector3I(x, y, z), blockType));
                        _chunk.SetBlock(x, y, z, blockType);
                    }
                    else if (actualY <= 0)
                    {
                        // blockUpdates.Add((new Vector3I(x, y, z), waterBlock));
                        _chunk.SetBlock(x, y, z, waterBlock);
                    }
                }
            }

        // Set all blocks at once using the new SetRange method
        // _chunk.SetRange(blockUpdates);
    }

    private static string DetermineBlockType(int actualY, int height, double maxSlope, int dirtDepth)
    {
        // Depth-based layers
        if (actualY > height - dirtDepth)
        {
            // Elevation-based blocks
            if (actualY <= 3)
                return "sand"; // maxSlope <= 1 ? "sand" : "gravel";

            // Surface layers
            if (actualY == height)
            {
                // if (maxSlope > 1.5) return "stone";

                // if (_rng.Randf() > 1 - (actualY - 250) / 50.0f)
                //     return maxSlope <= 2 ? "snow" : "stone";

                // if (_rng.Randf() < (actualY - 170) / 50.0f)
                //     return maxSlope <= 1 ? "grass_block" : "stone";

                return "grass_block";
            }

            return "dirt";
            // return maxSlope > 2.5 ? "stone" : "dirt";
        }

        return "stone";
    }
}