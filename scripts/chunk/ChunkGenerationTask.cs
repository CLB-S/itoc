using System;
using System.Threading;
using Godot;
using ITOC.Multithreading;

namespace ITOC.Chunks;

public class ChunkGenerationTask : GameTask
{
    public readonly WorldGenerator.WorldGenerator WorldGenerator;
    public Vector3I ChunkPosition { get; }
    public ChunkColumn ChunkColumn { get; }
    public Action<ChunkData> Callback { get; }
    private readonly ChunkData _chunkData;

    public ChunkGenerationTask(
        WorldGenerator.WorldGenerator worldGenerator,
        Vector3I position,
        ChunkColumn chunkColumn,
        Action<ChunkData> callback,
        string name = null,
        TaskPriority priority = TaskPriority.Normal)
        : base(name, priority)
    {
        WorldGenerator = worldGenerator;
        ChunkPosition = position;
        ChunkColumn = chunkColumn;
        Callback = callback;
        _chunkData = new ChunkData(ChunkPosition);
    }

    protected override void ExecuteCore(CancellationToken cancellationToken)
    {
        SetBlocksByHeightMap();
        Callback?.Invoke(_chunkData);
    }

    private void SetBlocksByHeightMap()
    {
        for (var x = 0; x < ChunkMesher.CS; x++)
            for (var z = 0; z < ChunkMesher.CS; z++)
            {
                var height = Mathf.FloorToInt(ChunkColumn.HeightMap[x, z]);

                // Calculate slope steepness
                // var maxSlope = CalculateSlope(x, z);

                // var baseDirtDepth = Mathf.Clamp(4 - Mathf.FloorToInt(maxSlope), 1, 4);
                for (var y = 0; y < ChunkMesher.CS; y++)
                {
                    var actualY = ChunkPosition.Y * ChunkMesher.CS + y;
                    if (actualY <= height)
                    {
                        var blockType = DetermineBlockType(actualY, height, 0, 4);
                        _chunkData.SetBlock(x, y, z, blockType);
                    }
                    else if (actualY <= 0)
                    {
                        _chunkData.SetBlock(x, y, z, "water");
                    }
                }
            }
    }

    private string DetermineBlockType(int actualY, int height, double maxSlope, int dirtDepth)
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