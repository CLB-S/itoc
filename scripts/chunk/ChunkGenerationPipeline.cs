using System;
using System.Collections.Generic;
using Godot;

namespace ChunkGenerator;

public enum ChunkGenerationState
{
    NotStarted,
    Initializing,
    HeightMap,
    Custom,
    Completed,
    Failed
}

public class ChunkGenerationPipeline
{
    public class GenerationStep
    {
        public ChunkGenerationState State { get; }
        public Action Action { get; }
        public bool Optional { get; }

        public GenerationStep(ChunkGenerationState state, Action action, bool optional = false)
        {
            State = state;
            Action = action;
            Optional = optional;
        }
    }


    public ChunkGenerationState State { get; private set; } = ChunkGenerationState.NotStarted;

    private readonly ChunkData _chunkData;
    private readonly ChunkGenerationRequest _request;
    private readonly RandomNumberGenerator _rng;
    private readonly LinkedList<GenerationStep> _generationPipeline = new();

    public ChunkGenerationPipeline(ChunkGenerationRequest request)
    {
        InitializePipeline();
        _request = request;
        _chunkData = new ChunkData(_request.ChunkPosition);
        _rng = new RandomNumberGenerator
        {
            Seed = _request.WorldGenerator.Settings.Seed +
                        (uint)(_request.ChunkPosition.X + _request.ChunkPosition.Y + _request.ChunkPosition.Z)
        };
    }

    private void InitializePipeline()
    {
        _generationPipeline.AddLast(new GenerationStep(ChunkGenerationState.HeightMap, SetBlocksByHeightMap));
    }

    private void SetBlocksByHeightMap()
    {
        for (var x = 0; x < ChunkMesher.CS; x++)
            for (var z = 0; z < ChunkMesher.CS; z++)
            {
                var height = Mathf.FloorToInt(_request.ChunkColumn.HeightMap[x, z]);

                // Calculate slope steepness
                // var maxSlope = CalculateSlope(x, z);

                // var baseDirtDepth = Mathf.Clamp(4 - Mathf.FloorToInt(maxSlope), 1, 4);
                for (var y = 0; y < ChunkMesher.CS; y++)
                {
                    var actualY = _request.ChunkPosition.Y * ChunkMesher.CS + y;
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

    private double CalculateSlope(int x, int z)
    {
        double maxSlope = 0;

        int[][] neighborOffsets =
        [
            [-1, 1],
            [0, 1],
            [1, 0],
            [1, 1]
        ];

        foreach (var offset in neighborOffsets)
        {
            var dx = offset[0];
            var dz = offset[1];

            var neighborAX = x + dx;
            var neighborAZ = z + dz;
            if (neighborAX < 0 || neighborAX >= ChunkMesher.CS_P || neighborAZ < 0 || neighborAZ >= ChunkMesher.CS_P)
                continue;

            var neighborBX = x - dx;
            var neighborBZ = z - dz;
            if (neighborBX < 0 || neighborBX >= ChunkMesher.CS_P || neighborBZ < 0 || neighborBZ >= ChunkMesher.CS_P)
                continue;

            var neighborHeightA = _request.ChunkColumn.HeightMap[neighborAX, neighborAZ];
            var neighborHeightB = _request.ChunkColumn.HeightMap[neighborBX, neighborBZ];

            var slope = Mathf.Abs(neighborHeightA - neighborHeightB) / 2.0f;
            if (slope > maxSlope)
                maxSlope = slope;
        }


        return maxSlope;
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

    public ChunkGenerationResult Execute()
    {
        try
        {
            State = ChunkGenerationState.Initializing;

            var currentNode = _generationPipeline.First;
            while (currentNode != null)
            {
                var step = currentNode.Value;
                currentNode = currentNode.Next;

                State = step.State;
                step.Action();
            }

            return CompleteGeneration();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    private ChunkGenerationResult CompleteGeneration()
    {
        State = ChunkGenerationState.Completed;
        return new ChunkGenerationResult(_chunkData, _request.ChunkColumn);
    }

    private ChunkGenerationResult HandleError(Exception ex)
    {
        State = ChunkGenerationState.Failed;
        GD.PrintErr($"Chunk generation failed: {ex}");
        return null;
    }
}